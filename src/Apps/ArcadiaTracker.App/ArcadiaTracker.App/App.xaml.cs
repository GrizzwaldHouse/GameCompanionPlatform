using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GameCompanion.Module.StarRupture.Services;
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
        Services = services.BuildServiceProvider();
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
    }
}
