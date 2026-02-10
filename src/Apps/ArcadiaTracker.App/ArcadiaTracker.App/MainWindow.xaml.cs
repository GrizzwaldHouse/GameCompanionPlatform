using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ArcadiaTracker.App.ViewModels;
using ArcadiaTracker.App.Views;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

namespace ArcadiaTracker.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DashboardView _dashboardView;
    private readonly ProgressionView _progressionView;
    private readonly NativeMapView _nativeMapView;
    private readonly RoadmapView _roadmapView;
    private readonly SettingsView _settingsView;
    private readonly ProductionView _productionView;
    private readonly ResearchView _researchView;
    private readonly SessionsView _sessionsView;
    private readonly PlayStatsView _playStatsView;
    private readonly ExportView _exportView;
    private readonly AchievementsView _achievementsView;
    private readonly NotificationsView _notificationsView;

    // Phase 3 services
    private readonly CataclysmTimerService _cataclysmService;
    private readonly ProductionDataService _productionService;
    private readonly SessionTrackingService _sessionService;
    private readonly ResearchTreeService _researchService;
    private readonly SaveHealthService _saveHealthService;

    // Phase 4 services
    private readonly PlayStatisticsService _playStatsService;
    private readonly ExportService _exportService;
    private readonly SteamAchievementService _achievementService;
    private readonly NotificationService _notificationService;

    // Track previous progress for notification comparison
    private PlayerProgress? _previousProgress;

    public MainWindow()
    {
        InitializeComponent();

        // Get ViewModel from DI
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;

        // Get Phase 3 services from DI
        _cataclysmService = App.Services.GetRequiredService<CataclysmTimerService>();
        _productionService = App.Services.GetRequiredService<ProductionDataService>();
        _sessionService = App.Services.GetRequiredService<SessionTrackingService>();
        _researchService = App.Services.GetRequiredService<ResearchTreeService>();
        _saveHealthService = App.Services.GetRequiredService<SaveHealthService>();

        // Get Phase 4 services from DI
        _playStatsService = App.Services.GetRequiredService<PlayStatisticsService>();
        _exportService = App.Services.GetRequiredService<ExportService>();
        _achievementService = App.Services.GetRequiredService<SteamAchievementService>();
        _notificationService = App.Services.GetRequiredService<NotificationService>();

        // Create views
        _dashboardView = new DashboardView();
        _progressionView = new ProgressionView();
        _nativeMapView = new NativeMapView();
        _roadmapView = new RoadmapView();
        _settingsView = new SettingsView();
        _productionView = new ProductionView();
        _researchView = new ResearchView();
        _sessionsView = new SessionsView();
        _playStatsView = new PlayStatsView();
        _exportView = new ExportView();
        _achievementsView = new AchievementsView();
        _notificationsView = new NotificationsView();

        // Wire up settings backup/restore events
        _settingsView.BackupRequested += async (s, savePath) =>
        {
            var result = await _saveHealthService.CreateBackupAsync(savePath);
            _settingsView.ShowHealthStatus(result.IsSuccess
                ? $"Backup created: {result.Value!.BackupId}"
                : $"Backup failed: {result.Error}");
            // Refresh health status after backup
            await UpdateSaveHealthAsync(savePath);
        };

        _settingsView.RestoreRequested += async (s, savePath) =>
        {
            var backupsResult = await _saveHealthService.GetBackupsAsync(
                System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(savePath)) ?? "");
            if (backupsResult.IsSuccess && backupsResult.Value!.Count > 0)
            {
                var latest = backupsResult.Value[0];
                var restoreResult = await _saveHealthService.RestoreBackupAsync(latest.BackupId);
                _settingsView.ShowHealthStatus(restoreResult.IsSuccess
                    ? "Restore complete. Refresh to reload data."
                    : $"Restore failed: {restoreResult.Error}");
            }
            else
            {
                _settingsView.ShowHealthStatus("No backups available to restore.");
            }
        };

        // Wire up export events
        _exportView.ExportRequested += async (s, request) =>
        {
            if (_viewModel.CurrentSave == null) return;

            // Show SaveFileDialog
            var ext = request.Format == ExportFormat.Csv ? "csv" : "xlsx";
            var filter = request.Format == ExportFormat.Csv ? "CSV Files (*.csv)|*.csv" : "Excel Files (*.xlsx)|*.xlsx";
            var dialog = new SaveFileDialog
            {
                FileName = $"ArcadiaTracker_{request.DataType}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = $".{ext}",
                Filter = filter,
            };

            if (dialog.ShowDialog() == true)
            {
                var exportRequest = new ExportRequest
                {
                    Format = request.Format,
                    DataType = request.DataType,
                    OutputPath = dialog.FileName,
                    SessionName = _viewModel.SelectedSession,
                };
                var result = await _exportService.ExportAsync(exportRequest, _viewModel.CurrentSave);
                if (result.IsSuccess)
                {
                    _exportView.UpdateHistory(new System.Collections.Generic.List<ExportResult> { result.Value! });
                    // Generate notification
                    _notificationService.AddNotification(new AppNotification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = NotificationType.ExportComplete,
                        Severity = NotificationSeverity.Success,
                        Title = "Export Complete",
                        Message = $"Exported {result.Value!.RowCount} rows to {result.Value.DisplaySize}",
                        Timestamp = DateTime.Now,
                    });
                }
            }
        };

        // Wire up notification events
        _notificationsView.MarkAllReadClicked += (s, _) =>
        {
            _notificationService.MarkAllAsRead();
            _notificationsView.UpdateNotifications(_notificationService.GetNotifications());
        };
        _notificationsView.ClearClicked += (s, _) =>
        {
            _notificationService.ClearNotifications();
            _notificationsView.UpdateNotifications(_notificationService.GetNotifications());
        };

        // Wire up progress updates to views
        _viewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentProgress) && _viewModel.CurrentProgress != null)
            {
                _dashboardView.UpdateProgress(_viewModel.CurrentProgress);
                _progressionView.UpdateProgress(_viewModel.CurrentProgress);
                _roadmapView.UpdateProgress(_viewModel.CurrentProgress);

                // Record session snapshot
                var session = _viewModel.SelectedSession;
                if (!string.IsNullOrEmpty(session))
                {
                    await _sessionService.RecordSnapshotAsync(session, _viewModel.CurrentProgress);
                    var historyResult = await _sessionService.GetHistoryAsync(session);
                    if (historyResult.IsSuccess)
                        _sessionsView.UpdateHistory(historyResult.Value!);
                }

                // Phase 4: Evaluate notifications
                CataclysmState? cataclysm = null;
                if (_viewModel.CurrentSave != null)
                    cataclysm = _cataclysmService.AnalyzeWave(_viewModel.CurrentSave.EnviroWave, _viewModel.CurrentSave.PlayTime);

                var newNotifications = _notificationService.EvaluateChanges(
                    _previousProgress, _viewModel.CurrentProgress, cataclysm);
                foreach (var notif in newNotifications)
                    _notificationService.AddNotification(notif);
                _previousProgress = _viewModel.CurrentProgress;

                // Update notification view
                _notificationsView.UpdateNotifications(_notificationService.GetNotifications());
            }

            if (e.PropertyName == nameof(MainViewModel.CurrentSave) && _viewModel.CurrentSave != null)
            {
                var save = _viewModel.CurrentSave;
                _nativeMapView.LoadFromSave(save);

                // Cataclysm timer
                var cataclysmState = _cataclysmService.AnalyzeWave(save.EnviroWave, save.PlayTime);
                _dashboardView.UpdateCataclysm(cataclysmState);

                // Production summary
                var productionResult = _productionService.BuildProductionSummary(save);
                if (productionResult.IsSuccess)
                    _productionView.UpdateProduction(productionResult.Value!);

                // Research tree
                var treeResult = await _researchService.BuildTreeAsync(save.Crafting);
                if (treeResult.IsSuccess)
                    _researchView.UpdateTree(treeResult.Value!);

                // Save health
                var newestSlot = _viewModel.AvailableSessions
                    .FirstOrDefault(s => s.SessionName == _viewModel.SelectedSession)?
                    .Slots.FirstOrDefault();
                if (newestSlot != null)
                {
                    _settingsView.SetSavePath(newestSlot.SaveFilePath);
                    await UpdateSaveHealthAsync(newestSlot.SaveFilePath);
                }

                // Phase 4: Play statistics
                var statsResult = await _playStatsService.ComputeStatisticsWithHistoryAsync(
                    save, _viewModel.SelectedSession);
                if (statsResult.IsSuccess)
                    _playStatsView.UpdateStatistics(statsResult.Value!);

                // Phase 4: Achievement cross-reference
                var achieveResult = await _achievementService.GetAchievementSummaryAsync(save);
                if (achieveResult.IsSuccess)
                    _achievementsView.UpdateAchievements(achieveResult.Value!);
            }
        };

        // Show dashboard by default
        ContentArea.Content = _dashboardView;

        // Initialize
        Loaded += async (s, e) =>
        {
            // Load notification history
            await _notificationService.LoadHistoryAsync();
            _notificationsView.UpdateNotifications(_notificationService.GetNotifications());

            await _viewModel.InitializeCommand.ExecuteAsync(null);
        };
    }

    private async Task UpdateSaveHealthAsync(string savePath)
    {
        var healthResult = await _saveHealthService.AnalyzeHealthAsync(savePath);
        if (healthResult.IsSuccess)
            _settingsView.UpdateHealth(healthResult.Value!);
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            ContentArea.Content = tag switch
            {
                "Dashboard" => _dashboardView,
                "Progression" => _progressionView,
                "Map" => _nativeMapView,
                "Roadmap" => _roadmapView,
                "Production" => _productionView,
                "Research" => _researchView,
                "Sessions" => _sessionsView,
                "PlayStats" => _playStatsView,
                "Achievements" => _achievementsView,
                "Export" => _exportView,
                "Notifications" => _notificationsView,
                "Settings" => _settingsView,
                _ => _dashboardView
            };
        }
    }
}
