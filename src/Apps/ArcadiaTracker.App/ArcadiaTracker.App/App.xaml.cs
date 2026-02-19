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
using ArcadiaTracker.App.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

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

    public static string DiagnosticsLogDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "logs");

    private static string EntitlementsDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "entitlements");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Configure Serilog crash reporting + structured diagnostics logging
        Directory.CreateDirectory(CrashReportDir);
        Directory.CreateDirectory(DiagnosticsLogDir);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(CrashReportDir, "arcadia-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(DiagnosticsLogDir, "diagnostics-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Debug()
            .CreateLogger();

        base.OnStartup(e);

        Log.Information("Arcadia Tracker starting up");

        var services = new ServiceCollection();
        ConfigureServices(services);
        ConfigureEntitlementServices(services);
        Services = services.BuildServiceProvider();

        // Initialize License Service (await to avoid race with early capability checks)
        var licenseService = Services.GetRequiredService<GameCompanion.Engine.Licensing.Interfaces.ILicenseService>();
        try
        {
            licenseService.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "License service initialization failed — continuing without license");
        }

        // Wire global exception handlers through DiagnosticsService (observer pattern)
        var diagnostics = Services.GetRequiredService<DiagnosticsService>();

        // Guard against re-entrant exceptions in the handler itself
        var handlingException = false;
        DispatcherUnhandledException += (s, args) =>
        {
            if (handlingException)
            {
                // Already handling an exception — prevent infinite recursion.
                // Just mark handled and let the app shut down.
                args.Handled = true;
                return;
            }

            handlingException = true;
            try
            {
                diagnostics.ReportFatal("Dispatcher", args.Exception);
                Log.CloseAndFlush();
            }
            catch
            {
                // Last-resort: never let the handler itself throw
            }

            args.Handled = true;
            // No MessageBox.Show here — it triggers layout which can cause recursive exceptions.
            // Crash report has already been written to disk via Serilog.
            Application.Current.Shutdown(1);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                diagnostics.ReportFatal("AppDomain", ex);
                Log.CloseAndFlush();
            }
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            diagnostics.Report(new DiagnosticEvent
            {
                Source = "TaskScheduler",
                Operation = "UnobservedTask",
                Severity = DiagnosticSeverity.Error,
                Message = args.Exception?.Message ?? "Unobserved task exception",
                Exception = args.Exception
            });
            args.SetObserved();
        };

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
        // Logging — wire up ILogger<T> with Serilog
        services.AddLogging(builder => builder.AddSerilog(dispose: false));

        // Diagnostics — observer-pattern error hub
        services.AddSingleton<DiagnosticsService>();

        // Engine Services
        services.AddSingleton<GameCompanion.Engine.SaveSafety.Interfaces.IBackupService, GameCompanion.Engine.SaveSafety.Services.BackupService>();
        
        // Licensing Configuration
        var licensePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArcadiaTracker", "license.key");
        var licenseConfig = new GameCompanion.Engine.Licensing.Models.LicenseConfiguration
        {
            LicenseFilePath = licensePath,
            PublicKeyXml = "<RSAKeyValue><Modulus>8joefANqVvw8UwmksFKIXp5+VsgSwDsb5d8bGvQIVL8Ma2GTkOmnfGJzo6cVaBMs79XNa0/qQuQ3PqXtH8iTZ+E6l15W0jhYH8otyyUXewfryxVSQdfec9ThoOPt7OhkOCNYN0S0gwEKpzMW8QQ3KYD9WNaIRHU3gnk7qLuCscpGBwfDQnjOpNAfg0bIJCwOLYgp9AZfp9fvN2e+u2hFru9qysmL/JUpkioISeZ9yHlsDo3xYqY5ejUfTLJc795j93O0errYVTcpf1dFxp1/HCW/j25g9/plDPOEmiEN/cHzel4LyOLQJQ8z3dtE6FbjW56SqHBP4JHHehcBH1oabQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>"
        };
        services.AddSingleton(licenseConfig);
        services.AddSingleton<GameCompanion.Engine.Licensing.Interfaces.ILicenseService, GameCompanion.Engine.Licensing.Services.LicenseService>();

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
        services.AddSingleton<INavigationService, WpfNavigationService>();

        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.DashboardViewModel>();
        services.AddTransient<ViewModels.ProgressionViewModel>();
        services.AddTransient<ViewModels.MapViewModel>();
        services.AddSingleton<ViewModels.NativeMapViewModel>();
        services.AddTransient<ViewModels.RoadmapViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.ProductionViewModel>();
        services.AddSingleton<ViewModels.SessionsViewModel>();
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
        services.AddTransient<ViewModels.ActivationViewModel>();
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
        services.AddSingleton(sp => new CapabilityValidator(signingKey));
        services.AddSingleton(sp => new CapabilityIssuer(sp.GetRequiredService<CapabilityValidator>()));
        
        services.AddSingleton<ICapabilityStore>(sp => new LocalCapabilityStore(
            Path.Combine(EntitlementsDir, "capabilities.dat"),
            encryptionKey));

        // Register EntitlementService to be resolving via DI (injects Logger, Validator, Issuer, Store)
        services.AddSingleton<EntitlementService>();
        services.AddSingleton<IEntitlementService>(sp => sp.GetRequiredService<EntitlementService>());

        // Capability-gated plugin loader (injects EntitlementService)
        services.AddSingleton<CapabilityGatedPluginLoader>();

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

        // Admin token service
        services.AddSingleton<IAdminTokenService>(sp => 
        {
            var audit = sp.GetRequiredService<LocalAuditLogger>();
            var tamper = sp.GetRequiredService<TamperDetector>();
            
            return new AdminTokenService(
                Path.Combine(EntitlementsDir, "admin.token"),
                encryptionKey,
                audit,
                tamper);
        });
        // Also register concrete type if needed, or just let others resolve interface.
        services.AddSingleton(sp => (AdminTokenService)sp.GetRequiredService<IAdminTokenService>());

        // Admin capability provider
        // - In DEBUG: env vars OR admin token
        // - In RELEASE: admin token only (env vars ignored)
        // Admin capability provider
        services.AddSingleton(sp => 
        {
            var audit = sp.GetRequiredService<LocalAuditLogger>();
            var ent = sp.GetRequiredService<EntitlementService>();
            var tokenService = sp.GetRequiredService<AdminTokenService>();
            
#if DEBUG
            var isProduction = false;
#else
            var isProduction = true;
#endif
            return new AdminCapabilityProvider(ent, audit, isProduction, tokenService);
        });

        // Activation code service (for code-based feature unlocking)
        services.AddSingleton<IActivationCodeService>(sp => 
            new ActivationCodeService(
                sp.GetRequiredService<EntitlementService>(),
                sp.GetRequiredService<LocalAuditLogger>(),
                Path.Combine(EntitlementsDir, "redeemed.json")));

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
