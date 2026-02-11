using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GameCompanion.Module.StarRupture.Services;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Services;
using GameCompanion.Module.SaveModifier.Interfaces;
using GameCompanion.Module.SaveModifier.Services;
using GameCompanion.Module.SaveModifier.StarRupture.Services;
using Serilog;

namespace ArcadiaTracker.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static string CrashReportDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "crash_reports");

    private static string EntitlementsDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "entitlements");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Configure Serilog crash reporting
        Directory.CreateDirectory(CrashReportDir);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.File(
                Path.Combine(CrashReportDir, "arcadia-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();

        // Global exception handlers
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled UI exception");
            Log.CloseAndFlush();
            args.Handled = true;
            MessageBox.Show(
                $"An unexpected error occurred. A crash report has been saved to:\n{CrashReportDir}\n\nError: {args.Exception.Message}",
                "Arcadia Tracker - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Unhandled domain exception");
                Log.CloseAndFlush();
            }
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        base.OnStartup(e);

        Log.Information("Arcadia Tracker starting up");

        var services = new ServiceCollection();
        ConfigureServices(services);
        ConfigureEntitlementServices(services);
        Services = services.BuildServiceProvider();

        // Attempt admin capability injection (no-op in production)
        _ = Task.Run(async () =>
        {
            var adminProvider = Services.GetRequiredService<AdminCapabilityProvider>();
            await adminProvider.TryInjectAdminCapabilitiesAsync();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Arcadia Tracker shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // StarRupture Module Services
        services.AddSingleton<SaveDiscoveryService>();
        services.AddSingleton<SaveParserService>();
        services.AddSingleton<ProgressionAnalyzerService>();
        services.AddSingleton<MapDataService>();
        services.AddSingleton<WikiDataService>();

        // Phase 3 Services
        services.AddSingleton<CataclysmTimerService>();
        services.AddSingleton<ProductionDataService>();
        services.AddSingleton<SessionTrackingService>();
        services.AddSingleton<ResearchTreeService>();
        services.AddSingleton<SaveHealthService>();

        // Phase 4 Services
        services.AddSingleton<PlayStatisticsService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<SteamAchievementService>();
        services.AddSingleton<WikiCacheService>();
        services.AddSingleton<SaveSharingService>();
        services.AddSingleton<NotificationService>();

        // Phase 5 Services
        services.AddSingleton<BlueprintService>();
        services.AddSingleton<BottleneckAnalyzerService>();
        services.AddSingleton<BuildPlannerService>();
        services.AddSingleton<CataclysmPlannerService>();
        services.AddSingleton<ChallengeTrackerService>();
        services.AddSingleton<DepletionForecastService>();
        services.AddSingleton<ExpansionAdvisorService>();
        services.AddSingleton<LogisticsHeatmapService>();
        services.AddSingleton<PowerGridAnalyzerService>();
        services.AddSingleton<RatioCalculatorService>();
        services.AddSingleton<RecordsService>();
        services.AddSingleton<ResearchPathService>();
        services.AddSingleton<SessionDiffService>();
        services.AddSingleton<SnapshotService>();
        services.AddSingleton<SpeedrunService>();
        services.AddSingleton<WizardService>();

        // ViewModels
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.DashboardViewModel>();
        services.AddTransient<ViewModels.ProgressionViewModel>();
        services.AddTransient<ViewModels.MapViewModel>();
        services.AddTransient<ViewModels.NativeMapViewModel>();
        services.AddTransient<ViewModels.RoadmapViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.ProductionViewModel>();
        services.AddTransient<ViewModels.SessionsViewModel>();
        services.AddTransient<ViewModels.ResearchViewModel>();
        services.AddTransient<ViewModels.PlayStatsViewModel>();
        services.AddTransient<ViewModels.ExportViewModel>();
        services.AddTransient<ViewModels.AchievementsViewModel>();
        services.AddTransient<ViewModels.NotificationsViewModel>();

        // Phase 5 ViewModels
        services.AddTransient<ViewModels.BlueprintViewModel>();
        services.AddTransient<ViewModels.BottleneckViewModel>();
        services.AddTransient<ViewModels.BuildPlannerViewModel>();
        services.AddTransient<ViewModels.CataclysmPlannerViewModel>();
        services.AddTransient<ViewModels.ChallengeTrackerViewModel>();
        services.AddTransient<ViewModels.DepletionForecastViewModel>();
        services.AddTransient<ViewModels.ExpansionAdvisorViewModel>();
        services.AddTransient<ViewModels.LogisticsHeatmapViewModel>();
        services.AddTransient<ViewModels.PowerGridViewModel>();
        services.AddTransient<ViewModels.RatioCalculatorViewModel>();
        services.AddTransient<ViewModels.RecordsViewModel>();
        services.AddTransient<ViewModels.ResearchPathViewModel>();
        services.AddTransient<ViewModels.SessionDiffViewModel>();
        services.AddTransient<ViewModels.SpeedrunViewModel>();
        services.AddTransient<ViewModels.TimeLapseViewModel>();
        services.AddTransient<ViewModels.WizardViewModel>();
    }

    /// <summary>
    /// Configures entitlement and capability-gated services.
    /// The save modifier plugin is only registered if a valid capability exists;
    /// otherwise these services are inert and invisible to the rest of the app.
    /// </summary>
    private static void ConfigureEntitlementServices(IServiceCollection services)
    {
        Directory.CreateDirectory(EntitlementsDir);

        // Derive keys from machine-specific seed
        var machineSeed = SigningKeyProvider.GetMachineSeed();
        var signingKey = SigningKeyProvider.DeriveSigningKey(machineSeed);
        var encryptionKey = SigningKeyProvider.DeriveEncryptionKey(machineSeed);

        // Core entitlement infrastructure
        var validator = new CapabilityValidator(signingKey);
        var issuer = new CapabilityIssuer(validator);
        var store = new LocalCapabilityStore(
            Path.Combine(EntitlementsDir, "capabilities.dat"),
            encryptionKey);

        var entitlementService = new EntitlementService(validator, issuer, store);

        services.AddSingleton(validator);
        services.AddSingleton(issuer);
        services.AddSingleton<ICapabilityStore>(store);
        services.AddSingleton<IEntitlementService>(entitlementService);

        // Capability-gated plugin loader
        var pluginLoader = new CapabilityGatedPluginLoader(entitlementService);
        services.AddSingleton(pluginLoader);

        // Audit logger
        var auditLogger = new LocalAuditLogger(Path.Combine(EntitlementsDir, "audit.log"));
        services.AddSingleton(auditLogger);

        // Consent service
        var consentService = new LocalConsentService(Path.Combine(EntitlementsDir, "consent.json"));
        services.AddSingleton<IConsentService>(consentService);

        // Tamper detector
        var tamperDetector = new TamperDetector(
            Path.Combine(EntitlementsDir, "integrity.dat"),
            auditLogger);
        services.AddSingleton(tamperDetector);

        // Admin token service (release-safe admin authentication)
        var adminTokenService = new AdminTokenService(
            Path.Combine(EntitlementsDir, "admin.token"),
            encryptionKey,
            auditLogger,
            tamperDetector);
        services.AddSingleton<IAdminTokenService>(adminTokenService);

        // Admin capability provider
        // - In DEBUG: env vars OR admin token
        // - In RELEASE: admin token only (env vars ignored)
#if DEBUG
        var isProduction = false;
#else
        var isProduction = true;
#endif
        var adminProvider = new AdminCapabilityProvider(
            entitlementService, auditLogger, isProduction, adminTokenService);
        services.AddSingleton(adminProvider);

        // Activation code service (for code-based feature unlocking)
        var activationService = new ActivationCodeService(
            entitlementService,
            auditLogger,
            Path.Combine(EntitlementsDir, "redeemed.json"));
        services.AddSingleton<IActivationCodeService>(activationService);

        // Save modification orchestrator (capability-gated at runtime)
        services.AddSingleton(sp =>
        {
            var orchestrator = new SaveModificationOrchestrator(
                sp.GetRequiredService<IEntitlementService>(),
                sp.GetRequiredService<IConsentService>(),
                sp.GetRequiredService<GameCompanion.Engine.SaveSafety.Interfaces.IBackupService>(),
                sp.GetRequiredService<LocalAuditLogger>());

            // Register game-specific adapters
            orchestrator.RegisterAdapter(new StarRuptureSaveModifierAdapter());

            return orchestrator;
        });

        // Save modifier adapter (registered for direct access if needed)
        services.AddSingleton<ISaveModifierAdapter, StarRuptureSaveModifierAdapter>();
    }
}
