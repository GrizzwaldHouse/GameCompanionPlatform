using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ArcadiaTracker.App.ViewModels;
using ArcadiaTracker.App.Views;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Services;
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

    // Premium views (created only when entitled)
    private SaveInspectorView? _saveInspectorView;
    private BackupManagerView? _backupManagerView;
    private ActivationView? _activationView;

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

    // Entitlement services
    private readonly CapabilityGatedPluginLoader _pluginLoader;

    // Track previous progress for notification comparison
    private PlayerProgress? _previousProgress;

    private const string GameScope = "star_rupture";

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

        // Entitlement services
        _pluginLoader = App.Services.GetRequiredService<CapabilityGatedPluginLoader>();

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

                    // Update premium views if they exist
                    _saveInspectorView?.UpdateFromSave(save, newestSlot.SaveFilePath);
                    _backupManagerView?.SetSavePath(newestSlot.SaveFilePath);
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

            // Check entitlements and show premium nav items
            await InitializePremiumFeatures();

            await _viewModel.InitializeCommand.ExecuteAsync(null);
        };
    }

    /// <summary>
    /// Checks entitlements and conditionally shows premium nav items.
    /// Premium views are only created if the user has valid capabilities,
    /// ensuring non-discoverability for non-paying users.
    /// </summary>
    private async Task InitializePremiumFeatures()
    {
        var hasPremium = false;

        // Check Save Inspector capability
        if (await _pluginLoader.HasCapabilityAsync(CapabilityActions.SaveInspect, GameScope))
        {
            _saveInspectorView = new SaveInspectorView();
            NavSaveInspector.Visibility = Visibility.Visible;
            hasPremium = true;
        }

        // Check Backup Manager capability
        if (await _pluginLoader.HasCapabilityAsync(CapabilityActions.BackupManage, GameScope))
        {
            _backupManagerView = new BackupManagerView();
            _backupManagerView.Initialize(_saveHealthService);
            NavBackupManager.Visibility = Visibility.Visible;
            hasPremium = true;
        }

        // Always show activation view (it's the entry point for getting premium features)
        // but only when running in a mode where activation is possible
        _activationView = new ActivationView();
        var activationService = App.Services.GetService<IActivationCodeService>();
        var entitlementService = App.Services.GetRequiredService<IEntitlementService>();
        if (activationService != null)
        {
            _activationView.Initialize(activationService, entitlementService, GameScope);
            _activationView.FeaturesActivated += async (s, e) =>
            {
                // Re-evaluate premium nav items after activation
                await InitializePremiumFeatures();
            };
        }
        NavActivation.Visibility = Visibility.Visible;

        // Show premium separator if any premium features are unlocked
        PremiumSeparator.Visibility = hasPremium
            ? Visibility.Visible
            : Visibility.Collapsed;
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
            var newContent = tag switch
            {
                "Dashboard" => (object)_dashboardView,
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
                // Premium views
                "SaveInspector" => (object?)_saveInspectorView ?? _dashboardView,
                "BackupManager" => (object?)_backupManagerView ?? _dashboardView,
                "Activation" => (object?)_activationView ?? _dashboardView,
                _ => _dashboardView
            };

            ContentArea.Content = newContent;

            // Apply view transition animation
            if (newContent is UIElement element)
            {
                element.RenderTransform = new System.Windows.Media.TranslateTransform();
                var storyboard = (Storyboard?)TryFindResource("ViewFadeInStoryboard");
                storyboard?.Begin(element);
            }
        }
    }
}
