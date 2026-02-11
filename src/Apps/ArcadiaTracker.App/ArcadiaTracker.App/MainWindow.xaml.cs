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
using GameCompanion.Module.SaveModifier.Interfaces;
using GameCompanion.Module.SaveModifier.Services;
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

    // Phase 5 views
    private readonly BlueprintView _blueprintView;
    private readonly BottleneckView _bottleneckView;
    private readonly BuildPlannerView _buildPlannerView;
    private readonly CataclysmPlannerView _cataclysmPlannerView;
    private readonly ChallengeTrackerView _challengeTrackerView;
    private readonly DepletionForecastView _depletionForecastView;
    private readonly ExpansionAdvisorView _expansionAdvisorView;
    private readonly LogisticsHeatmapView _logisticsHeatmapView;
    private readonly PowerGridView _powerGridView;
    private readonly RatioCalculatorView _ratioCalculatorView;
    private readonly RecordsView _recordsView;
    private readonly ResearchPathView _researchPathView;
    private readonly SessionDiffView _sessionDiffView;
    private readonly SpeedrunView _speedrunView;
    private readonly TimeLapseView _timeLapseView;
    private readonly WizardView _wizardView;

    // Premium views (created only when entitled)
    private SaveEditorView? _saveEditorView;
    private SaveInspectorView? _saveInspectorView;
    private BackupManagerView? _backupManagerView;
    private ActivationView? _activationView;
    private AdminPanelView? _adminPanelView;

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

    // Phase 5 services
    private readonly BlueprintService _blueprintService;
    private readonly BottleneckAnalyzerService _bottleneckService;
    private readonly BuildPlannerService _buildPlannerService;
    private readonly CataclysmPlannerService _cataclysmPlannerService;
    private readonly ChallengeTrackerService _challengeService;
    private readonly DepletionForecastService _depletionService;
    private readonly ExpansionAdvisorService _expansionService;
    private readonly LogisticsHeatmapService _logisticsService;
    private readonly PowerGridAnalyzerService _powerGridService;
    private readonly RatioCalculatorService _ratioService;
    private readonly RecordsService _recordsService;
    private readonly ResearchPathService _researchPathService;
    private readonly SessionDiffService _sessionDiffService;
    private readonly SnapshotService _snapshotService;
    private readonly SpeedrunService _speedrunService;
    private readonly WizardService _wizardService;

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

        // Get Phase 5 services from DI
        _blueprintService = App.Services.GetRequiredService<BlueprintService>();
        _bottleneckService = App.Services.GetRequiredService<BottleneckAnalyzerService>();
        _buildPlannerService = App.Services.GetRequiredService<BuildPlannerService>();
        _cataclysmPlannerService = App.Services.GetRequiredService<CataclysmPlannerService>();
        _challengeService = App.Services.GetRequiredService<ChallengeTrackerService>();
        _depletionService = App.Services.GetRequiredService<DepletionForecastService>();
        _expansionService = App.Services.GetRequiredService<ExpansionAdvisorService>();
        _logisticsService = App.Services.GetRequiredService<LogisticsHeatmapService>();
        _powerGridService = App.Services.GetRequiredService<PowerGridAnalyzerService>();
        _ratioService = App.Services.GetRequiredService<RatioCalculatorService>();
        _recordsService = App.Services.GetRequiredService<RecordsService>();
        _researchPathService = App.Services.GetRequiredService<ResearchPathService>();
        _sessionDiffService = App.Services.GetRequiredService<SessionDiffService>();
        _snapshotService = App.Services.GetRequiredService<SnapshotService>();
        _speedrunService = App.Services.GetRequiredService<SpeedrunService>();
        _wizardService = App.Services.GetRequiredService<WizardService>();

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

        // Phase 5 views
        _blueprintView = new BlueprintView();
        _bottleneckView = new BottleneckView();
        _buildPlannerView = new BuildPlannerView();
        _buildPlannerView.DataContext = App.Services.GetRequiredService<BuildPlannerViewModel>();
        _cataclysmPlannerView = new CataclysmPlannerView();
        _challengeTrackerView = new ChallengeTrackerView();
        _depletionForecastView = new DepletionForecastView();
        _expansionAdvisorView = new ExpansionAdvisorView();
        _logisticsHeatmapView = new LogisticsHeatmapView();
        _powerGridView = new PowerGridView();
        _ratioCalculatorView = new RatioCalculatorView();
        _recordsView = new RecordsView();
        _researchPathView = new ResearchPathView();
        _sessionDiffView = new SessionDiffView();
        _speedrunView = new SpeedrunView(App.Services.GetRequiredService<SpeedrunViewModel>());
        _timeLapseView = new TimeLapseView();
        _wizardView = new WizardView();
        _wizardView.SetViewModel(App.Services.GetRequiredService<WizardViewModel>());

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
                    _saveEditorView?.SetSavePath(newestSlot.SaveFilePath);
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

                // Phase 5: New view data updates
                var bottleneckResult = _bottleneckService.AnalyzeBottlenecks(save);
                if (bottleneckResult.IsSuccess)
                    _bottleneckView.UpdateAnalysis(bottleneckResult.Value!);

                var powerGridResult = _powerGridService.AnalyzePowerGrid(save);
                if (powerGridResult.IsSuccess)
                    _powerGridView.UpdateAnalysis(powerGridResult.Value!);

                var cataclysmPlanResult = _cataclysmPlannerService.GeneratePlan(save);
                if (cataclysmPlanResult.IsSuccess)
                    _cataclysmPlannerView.UpdatePlan(cataclysmPlanResult.Value!);

                var challengeResult = _challengeService.GetChallengeTracker(save);
                if (challengeResult.IsSuccess)
                    _challengeTrackerView.UpdateChallenges(challengeResult.Value!);

                var depletionResult = _depletionService.GenerateForecast(save);
                if (depletionResult.IsSuccess)
                    _depletionForecastView.UpdateForecast(depletionResult.Value!);

                var expansionResult = _expansionService.AnalyzeExpansion(save);
                if (expansionResult.IsSuccess)
                    _expansionAdvisorView.UpdatePlan(expansionResult.Value!);

                var heatmapResult = _logisticsService.GenerateHeatmap(save);
                if (heatmapResult.IsSuccess)
                    _logisticsHeatmapView.UpdateHeatmap(heatmapResult.Value!);

                _ratioCalculatorView.SetCurrentSave(save);

                var blueprintResult = await _blueprintService.GetLibraryAsync();
                if (blueprintResult.IsSuccess)
                    _blueprintView.UpdateLibrary(blueprintResult.Value!);

                var researchPathResult = await _researchPathService.GeneratePathAsync(save.Crafting);
                if (researchPathResult.IsSuccess)
                    _researchPathView.UpdatePath(researchPathResult.Value!);

                var recordsResult = await _recordsService.LoadRecordsAsync();
                if (recordsResult.IsSuccess)
                    _recordsView.UpdateRecords(recordsResult.Value!);

                var snapshotSessions = _snapshotService.GetSessionsWithSnapshots();
                if (snapshotSessions.IsSuccess)
                    _timeLapseView.UpdateSessions(snapshotSessions.Value!);
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
        var entitlementService = App.Services.GetRequiredService<IEntitlementService>();
        var activationService = App.Services.GetService<IActivationCodeService>();

        // Check Save Editor capability (core paid feature)
        if (await _pluginLoader.HasCapabilityAsync(CapabilityActions.SaveModify, GameScope))
        {
            if (_saveEditorView == null)
            {
                _saveEditorView = new SaveEditorView();
                var orchestrator = App.Services.GetRequiredService<SaveModificationOrchestrator>();
                var consentService = App.Services.GetRequiredService<IConsentService>();
                var adapter = App.Services.GetRequiredService<ISaveModifierAdapter>();
                _saveEditorView.Initialize(orchestrator, consentService, adapter);
            }
            NavSaveEditor.Visibility = Visibility.Visible;
            hasPremium = true;
        }

        // Check Save Inspector capability
        if (await _pluginLoader.HasCapabilityAsync(CapabilityActions.SaveInspect, GameScope))
        {
            _saveInspectorView ??= new SaveInspectorView();
            NavSaveInspector.Visibility = Visibility.Visible;
            hasPremium = true;
        }

        // Check Backup Manager capability
        if (await _pluginLoader.HasCapabilityAsync(CapabilityActions.BackupManage, GameScope))
        {
            if (_backupManagerView == null)
            {
                _backupManagerView = new BackupManagerView();
                _backupManagerView.Initialize(_saveHealthService);
            }
            NavBackupManager.Visibility = Visibility.Visible;
            hasPremium = true;
        }

        // Always show activation view (entry point for getting premium features)
        if (_activationView == null)
        {
            _activationView = new ActivationView();
            if (activationService != null)
            {
                _activationView.Initialize(activationService, entitlementService, GameScope);
                _activationView.FeaturesActivated += async (s, e) =>
                {
                    // Re-evaluate premium nav items after activation
                    await InitializePremiumFeatures();
                };
            }
        }
        NavActivation.Visibility = Visibility.Visible;

        // Admin panel: available when admin capabilities are active (any build)
        var adminProvider = App.Services.GetRequiredService<AdminCapabilityProvider>();
        if (await adminProvider.HasAdminOverrideAsync(GameScope))
        {
            if (_adminPanelView == null)
            {
                _adminPanelView = new AdminPanelView();
                var auditLogger = App.Services.GetRequiredService<LocalAuditLogger>();
                var adminTokenService = App.Services.GetRequiredService<IAdminTokenService>();
                var tamperDetector = App.Services.GetRequiredService<TamperDetector>();
                var capabilityStore = App.Services.GetRequiredService<ICapabilityStore>();
                _adminPanelView.Initialize(
                    activationService!, entitlementService, auditLogger, GameScope,
                    adminProvider, adminTokenService, tamperDetector, capabilityStore);
                _adminPanelView.FeaturesActivated += async (s, e) =>
                {
                    await InitializePremiumFeatures();
                };
            }
            NavAdminPanel.Visibility = Visibility.Visible;
        }

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
                // Overview
                "Dashboard" => (object)_dashboardView,
                "Wizard" => _wizardView,
                // Analytics
                "Progression" => _progressionView,
                "Records" => _recordsView,
                "SessionDiff" => _sessionDiffView,
                "TimeLapse" => _timeLapseView,
                "Speedrun" => _speedrunView,
                // Base Management
                "Production" => _productionView,
                "PowerGrid" => _powerGridView,
                "RatioCalculator" => _ratioCalculatorView,
                "Bottleneck" => _bottleneckView,
                "LogisticsHeatmap" => _logisticsHeatmapView,
                // Strategy
                "Research" => _researchView,
                "ResearchPath" => _researchPathView,
                "CataclysmPlanner" => _cataclysmPlannerView,
                "BuildPlanner" => _buildPlannerView,
                "DepletionForecast" => _depletionForecastView,
                // Exploration
                "Map" => _nativeMapView,
                "Blueprints" => _blueprintView,
                "ExpansionAdvisor" => _expansionAdvisorView,
                "ChallengeTracker" => _challengeTrackerView,
                // Meta
                "Roadmap" => _roadmapView,
                "Sessions" => _sessionsView,
                "PlayStats" => _playStatsView,
                "Achievements" => _achievementsView,
                "Export" => _exportView,
                "Notifications" => _notificationsView,
                "Settings" => _settingsView,
                // Premium (capability-gated)
                "SaveEditor" => (object?)_saveEditorView ?? _dashboardView,
                "SaveInspector" => (object?)_saveInspectorView ?? _dashboardView,
                "BackupManager" => (object?)_backupManagerView ?? _dashboardView,
                "Activation" => (object?)_activationView ?? _dashboardView,
                "AdminPanel" => (object?)_adminPanelView ?? _dashboardView,
                _ => _dashboardView
            };

            ContentArea.Content = newContent;

            // Apply view transition animation
            if (newContent is FrameworkElement element)
            {
                element.RenderTransform = new System.Windows.Media.TranslateTransform();
                var storyboard = (Storyboard?)TryFindResource("ViewFadeInStoryboard");
                storyboard?.Begin(element);
            }
        }
    }
}
