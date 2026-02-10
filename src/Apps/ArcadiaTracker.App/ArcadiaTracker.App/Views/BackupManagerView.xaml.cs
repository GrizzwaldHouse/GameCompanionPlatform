using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Services;

namespace ArcadiaTracker.App.Views;

public partial class BackupManagerView : UserControl
{
    private SaveHealthService? _saveHealthService;
    private string? _currentSavePath;
    private FileSystemWatcher? _watcher;
    private bool _autoBackupEnabled;

    public BackupManagerView()
    {
        InitializeComponent();
    }

    public void Initialize(SaveHealthService saveHealthService)
    {
        _saveHealthService = saveHealthService;
    }

    public void SetSavePath(string savePath)
    {
        _currentSavePath = savePath;
        _ = RefreshBackupList();
        SetupFileWatcher(savePath);
    }

    private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (_saveHealthService == null || string.IsNullOrEmpty(_currentSavePath)) return;

        CreateBackupButton.IsEnabled = false;
        try
        {
            var result = await _saveHealthService.CreateBackupAsync(_currentSavePath);
            if (result.IsSuccess)
            {
                ShowStatus($"Backup created: {result.Value!.BackupId}", isError: false);
                await RefreshBackupList();
            }
            else
            {
                ShowStatus($"Backup failed: {result.Error}", isError: true);
            }
        }
        finally
        {
            CreateBackupButton.IsEnabled = true;
        }
    }

    private async void RefreshListButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshBackupList();
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (_saveHealthService == null) return;
        if (sender is Button btn && btn.Tag is string backupId)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to restore this backup? Your current save will be overwritten.",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _saveHealthService.RestoreBackupAsync(backupId);
            ShowStatus(result.IsSuccess
                ? "Restore complete. Refresh the app to reload data."
                : $"Restore failed: {result.Error}",
                isError: result.IsFailure);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_saveHealthService == null) return;
        if (sender is Button btn && btn.Tag is string backupId)
        {
            var confirm = MessageBox.Show(
                "Delete this backup permanently?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _saveHealthService.DeleteBackupAsync(backupId);
            if (result.IsSuccess)
                await RefreshBackupList();
            else
                ShowStatus($"Delete failed: {result.Error}", isError: true);
        }
    }

    private void AutoBackupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _autoBackupEnabled = AutoBackupCheckBox.IsChecked == true;
    }

    private async Task RefreshBackupList()
    {
        if (_saveHealthService == null || string.IsNullOrEmpty(_currentSavePath)) return;

        var sessionName = Path.GetFileName(Path.GetDirectoryName(_currentSavePath)) ?? "";
        var result = await _saveHealthService.GetBackupsAsync(sessionName);

        if (result.IsSuccess && result.Value != null)
        {
            var backups = result.Value;
            var displayItems = new ObservableCollection<BackupDisplayItem>();
            long totalSize = 0;

            foreach (var backup in backups)
            {
                displayItems.Add(new BackupDisplayItem
                {
                    BackupId = backup.BackupId,
                    DisplayName = $"Backup {backup.BackupId[..Math.Min(8, backup.BackupId.Length)]}",
                    TimestampDisplay = backup.CreatedAt.ToString("g"),
                    SizeDisplay = $"{backup.SizeBytes / 1024.0:F1} KB"
                });
                totalSize += backup.SizeBytes;
            }

            BackupList.ItemsSource = displayItems;
            TotalBackupsText.Text = backups.Count.ToString();
            TotalSizeText.Text = totalSize > 1048576
                ? $"{totalSize / 1048576.0:F1} MB"
                : $"{totalSize / 1024.0:F1} KB";
            LastBackupText.Text = backups.Count > 0
                ? backups[0].CreatedAt.ToString("g")
                : "Never";
            NoBackupsText.Visibility = backups.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void SetupFileWatcher(string savePath)
    {
        _watcher?.Dispose();

        var dir = Path.GetDirectoryName(savePath);
        var fileName = Path.GetFileName(savePath);
        if (dir == null || !Directory.Exists(dir)) return;

        _watcher = new FileSystemWatcher(dir, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Changed += async (s, e) =>
        {
            if (!_autoBackupEnabled || _saveHealthService == null) return;

            // Debounce: wait for write to complete
            await Task.Delay(2000);

            await Dispatcher.InvokeAsync(async () =>
            {
                var result = await _saveHealthService.CreateBackupAsync(savePath);
                if (result.IsSuccess)
                {
                    ShowStatus("Auto-backup created.", isError: false);
                    await RefreshBackupList();
                }
            });
        };

        _watcher.EnableRaisingEvents = true;
    }

    private void ShowStatus(string message, bool isError)
    {
        BackupStatusBorder.Visibility = Visibility.Visible;
        BackupStatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        BackupStatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        BackupStatusText.Text = message;
    }
}

public class BackupDisplayItem
{
    public string BackupId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string TimestampDisplay { get; set; } = "";
    public string SizeDisplay { get; set; } = "";
}
