namespace GameCompanion.Module.StarRupture;

using GameCompanion.Core.Enums;
using GameCompanion.Core.Interfaces;
using GameCompanion.Module.StarRupture.Progression;
using GameCompanion.Module.StarRupture.Services;
using GameCompanion.Module.StarRupture.Theme;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Game module for StarRupture survival game.
/// </summary>
public sealed class StarRuptureGameModule : IGameModule
{
    private readonly StarRuptureProgressionMap _progressionMap = new();
    private readonly StarRuptureThemeProvider _themeProvider = new();

    public string GameId => "starrupture";
    public string DisplayName => "StarRupture";
    public Version ModuleVersion => new(1, 0, 0);

    public IProgressionMap GetProgressionMap() => _progressionMap;

    public IReadOnlyList<ISaveFieldDefinition> GetEditableFields()
    {
        // TODO: Define editable save fields
        return Array.Empty<ISaveFieldDefinition>();
    }

    public IReadOnlyDictionary<string, RiskLevel> GetFieldRiskClassifications()
    {
        return new Dictionary<string, RiskLevel>
        {
            // LOW: Cosmetic / UI preferences
            ["settings.graphics"] = RiskLevel.Low,
            ["settings.audio"] = RiskLevel.Low,

            // MEDIUM: Player inventory and stats
            ["player.inventory"] = RiskLevel.Medium,
            ["player.dataPoints"] = RiskLevel.Medium,
            ["corporations.xp"] = RiskLevel.Medium,

            // HIGH: World state
            ["world.entities"] = RiskLevel.High,
            ["world.buildings"] = RiskLevel.High,
            ["enviroWave.progress"] = RiskLevel.High,

            // CRITICAL: Save integrity
            ["header.version"] = RiskLevel.Critical,
            ["header.timestamp"] = RiskLevel.Critical,
            ["header.checksum"] = RiskLevel.Critical
        };
    }

    public IThemeProvider GetThemeProvider() => _themeProvider;

    public IReadOnlyDictionary<string, string> GetUICopy()
    {
        return new Dictionary<string, string>
        {
            ["app.title"] = "StarRupture Companion",
            ["app.tagline"] = "Your survival guide to the alien world",

            ["dashboard.current_phase"] = "Current Phase",
            ["dashboard.next_action"] = "Recommended Next",
            ["dashboard.save_health"] = "Save Health",

            ["progression.early_game"] = "Early Game",
            ["progression.mid_game"] = "Mid Game",
            ["progression.end_game"] = "End Game",
            ["progression.mastery"] = "Mastery",

            ["blueprints.title"] = "Blueprints",
            ["blueprints.locked"] = "Locked",
            ["blueprints.unlocked"] = "Unlocked",

            ["corporations.title"] = "Corporations",
            ["corporations.data_points"] = "Data Points",
            ["corporations.moon_energy"] = "Moon Energy",

            ["map.title"] = "Interactive Map",
            ["map.locked_hint"] = "Unlock the map by reaching Moon Energy Level 3",

            ["badges.title"] = "Achievements",
            ["badges.earned"] = "Earned",
            ["badges.locked"] = "Locked"
        };
    }

    public void RegisterServices(IServiceCollection services)
    {
        // Register StarRupture-specific services
        services.AddSingleton<SaveDiscoveryService>();
        services.AddSingleton<SaveParserService>();
        services.AddSingleton<ProgressionAnalyzerService>();
        services.AddSingleton<MapDataService>();
        services.AddSingleton<IThemeProvider, StarRuptureThemeProvider>();
        services.AddSingleton<IProgressionMap, StarRuptureProgressionMap>();
    }
}
