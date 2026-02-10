namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Progression;

/// <summary>
/// Analyzes a StarRupture save to extract progression metrics and earned badges.
/// </summary>
public sealed class ProgressionAnalyzerService
{
    private readonly SaveParserService _parser;

    public ProgressionAnalyzerService(SaveParserService parser)
    {
        _parser = parser;
    }

    /// <summary>
    /// Analyzes a save file and returns player progress.
    /// </summary>
    public async Task<Result<PlayerProgress>> AnalyzeAsync(
        string savePath,
        CancellationToken ct = default)
    {
        var parseResult = await _parser.ParseSaveAsync(savePath, ct);
        if (parseResult.IsFailure)
            return Result<PlayerProgress>.Failure(parseResult.Error!);

        var save = parseResult.Value!;
        return Result<PlayerProgress>.Success(AnalyzeSave(save));
    }

    /// <summary>
    /// Analyzes a parsed save.
    /// </summary>
    public PlayerProgress AnalyzeSave(StarRuptureSave save)
    {
        // Calculate earned badges
        var earnedBadges = Badges.AllBadges
            .Select(def => def.ToBadgeIfEarned(save))
            .Where(b => b != null)
            .Cast<Badge>()
            .ToList();

        // Determine current phase based on progression
        var phase = DeterminePhase(save);

        // Calculate overall progress (0-100%)
        var overallProgress = CalculateOverallProgress(save, earnedBadges);

        // Check if map is unlocked (Moon Energy Level 3+)
        var moonCorp = save.Corporations.Corporations
            .FirstOrDefault(c => c.Name == "MoonCorporation");
        var mapUnlocked = moonCorp?.CurrentLevel >= 3;

        // Find highest corporation level
        var highestCorp = save.Corporations.Corporations
            .OrderByDescending(c => c.CurrentLevel)
            .ThenByDescending(c => c.CurrentXP)
            .FirstOrDefault();

        return new PlayerProgress
        {
            SessionName = save.SessionName,
            TotalPlayTime = save.PlayTime,
            CurrentPhase = phase,
            OverallProgress = overallProgress,
            BlueprintsUnlocked = save.Crafting.UnlockedRecipeCount,
            BlueprintsTotal = save.Crafting.TotalRecipeCount,
            DataPointsEarned = save.Corporations.DataPoints,
            HighestCorporationLevel = highestCorp?.CurrentLevel ?? 0,
            HighestCorporationName = highestCorp?.DisplayName ?? "None",
            MapUnlocked = mapUnlocked,
            Corporations = save.Corporations.Corporations,
            UniqueItemsDiscovered = save.Crafting.PickedUpItems.Count,
            CurrentWave = save.EnviroWave.Wave,
            CurrentWaveStage = save.EnviroWave.Stage,
            EarnedBadges = earnedBadges
        };
    }

    private ProgressionPhase DeterminePhase(StarRuptureSave save)
    {
        // Early Game: < 5 hours, few blueprints, low data points
        if (save.PlayTime.TotalHours < 5 && save.Crafting.UnlockedRecipeCount < 30)
            return ProgressionPhase.EarlyGame;

        // Mastery: All blueprints unlocked
        if (save.Crafting.LockedRecipes.Count == 0)
            return ProgressionPhase.Mastery;

        // End Game: > 20 hours, 80+ blueprints, high data points
        if (save.PlayTime.TotalHours >= 20 &&
            save.Crafting.UnlockedRecipeCount >= 80 &&
            save.Corporations.DataPoints >= 15000)
            return ProgressionPhase.EndGame;

        // Mid Game: Everything else
        return ProgressionPhase.MidGame;
    }

    private double CalculateOverallProgress(StarRuptureSave save, IReadOnlyList<Badge> earnedBadges)
    {
        // Weight different progression factors
        const double BlueprintWeight = 0.4;
        const double BadgeWeight = 0.3;
        const double DataPointWeight = 0.2;
        const double PlaytimeWeight = 0.1;

        // Blueprint progress (0-1)
        var blueprintProgress = save.Crafting.TotalRecipeCount > 0
            ? (double)save.Crafting.UnlockedRecipeCount / save.Crafting.TotalRecipeCount
            : 0;

        // Badge progress (0-1)
        var badgeProgress = Badges.AllBadges.Count > 0
            ? (double)earnedBadges.Count / Badges.AllBadges.Count
            : 0;

        // Data point progress (capped at 50k)
        const int MaxDataPoints = 50000;
        var dataProgress = Math.Min(1.0, (double)save.Corporations.DataPoints / MaxDataPoints);

        // Playtime progress (capped at 100 hours)
        const double MaxPlaytimeHours = 100;
        var playtimeProgress = Math.Min(1.0, save.PlayTime.TotalHours / MaxPlaytimeHours);

        return (blueprintProgress * BlueprintWeight) +
               (badgeProgress * BadgeWeight) +
               (dataProgress * DataPointWeight) +
               (playtimeProgress * PlaytimeWeight);
    }
}
