using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Settings view for app configuration.
/// </summary>
public partial class SettingsView : UserControl
{
    private static readonly string CrashReportDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "crash_reports");

    public SettingsView()
    {
        InitializeComponent();

        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version != null)
        {
            VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private void WikiButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://starrupture.tools",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

    private void ReportIssueButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/GrizzwaldHouse/GameCompanionPlatform/issues",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

    private void CopyErrorReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(CrashReportDir))
            {
                ShowCrashStatus("No crash reports found.");
                return;
            }

            var latestLog = Directory.GetFiles(CrashReportDir, "*.txt")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (latestLog == null)
            {
                ShowCrashStatus("No crash reports found.");
                return;
            }

            var content = File.ReadAllText(latestLog);
            Clipboard.SetText(content);
            ShowCrashStatus($"Copied {Path.GetFileName(latestLog)} to clipboard.");
        }
        catch (Exception ex)
        {
            ShowCrashStatus($"Failed to copy: {ex.Message}");
        }
    }

    private void OpenCrashFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(CrashReportDir))
                Directory.CreateDirectory(CrashReportDir);

            Process.Start(new ProcessStartInfo
            {
                FileName = CrashReportDir,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening folder
        }
    }

    private void ShowCrashStatus(string message)
    {
        CrashReportStatus.Text = message;
        CrashReportStatus.Visibility = Visibility.Visible;
    }

    public void UpdateHealth(SaveHealthStatus health)
    {
        var (statusText, statusColor) = health.Level switch
        {
            SaveHealthLevel.Healthy => ("Healthy", "#00FF88"),
            SaveHealthLevel.Warning => ("Warning", "#FFB800"),
            SaveHealthLevel.Corrupted => ("Corrupted", "#FF4757"),
            _ => ("Unknown", "#888888")
        };

        HealthStatusText.Text = statusText;
        HealthStatusText.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(statusColor));

        HealthDetailsText.Text = $"{health.FileSizeDisplay} · Modified {health.LastModified:g}";
        BackupCountText.Text = $"{health.BackupCount} backup{(health.BackupCount != 1 ? "s" : "")}";

        if (health.Issues.Count > 0)
        {
            HealthIssuesList.ItemsSource = health.Issues;
            HealthIssuesList.Visibility = Visibility.Visible;
        }
        else
        {
            HealthIssuesList.Visibility = Visibility.Collapsed;
        }

        RestoreButton.IsEnabled = health.BackupCount > 0;
    }

    // Store current save path for backup/restore operations
    private string? _currentSavePath;
    public void SetSavePath(string path) => _currentSavePath = path;

    private void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSavePath == null) { ShowHealthStatus("No save loaded."); return; }
        ShowHealthStatus("Creating backup...");
        // The actual service call will be wired from MainWindow
        BackupRequested?.Invoke(this, _currentSavePath);
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSavePath == null) { ShowHealthStatus("No save loaded."); return; }
        RestoreRequested?.Invoke(this, _currentSavePath);
    }

    public event EventHandler<string>? BackupRequested;
    public event EventHandler<string>? RestoreRequested;

    public void ShowHealthStatus(string message)
    {
        HealthActionStatus.Text = message;
        HealthActionStatus.Visibility = Visibility.Visible;
    }
}
