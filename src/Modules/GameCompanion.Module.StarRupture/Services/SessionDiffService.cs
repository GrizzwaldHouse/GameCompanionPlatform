namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for comparing two save files and generating diff reports.
/// </summary>
public sealed class SessionDiffService
{
    /// <summary>
    /// Compares two saves and generates a detailed diff report.
    /// </summary>
    public Result<SaveDifference> CompareSaves(StarRuptureSave before, StarRuptureSave after)
    {
        try
        {
            // Calculate recipe delta (unlocked = total - locked)
            var beforeUnlocked = before.Crafting.TotalRecipeCount - before.Crafting.LockedRecipes.Count;
            var afterUnlocked = after.Crafting.TotalRecipeCount - after.Crafting.LockedRecipes.Count;

            var diff = new SaveDifference
            {
                BeforeSession = before.SessionName,
                AfterSession = after.SessionName,
                BeforeTimestamp = before.SaveTimestamp,
                AfterTimestamp = after.SaveTimestamp,
                PlayTimeDelta = after.PlayTime - before.PlayTime,

                // Corporation changes
                DataPointsDelta = after.Corporations.DataPoints - before.Corporations.DataPoints,
                InventorySlotsDelta = after.Corporations.UnlockedInventorySlots - before.Corporations.UnlockedInventorySlots,

                // Crafting changes - recipes newly unlocked (were locked, now aren't)
                NewUnlockedRecipes = before.Crafting.LockedRecipes
                    .Except(after.Crafting.LockedRecipes)
                    .ToList(),
                NewPickedItems = after.Crafting.PickedUpItems
                    .Except(before.Crafting.PickedUpItems)
                    .ToList(),

                // Wave progress (strings, compare as text)
                WaveChange = $"{before.EnviroWave.Wave} → {after.EnviroWave.Wave}",
                StageChange = $"{before.EnviroWave.Stage} → {after.EnviroWave.Stage}",

                // Entity changes (if spatial data available)
                EntitiesBuilt = CountEntityDelta(before.Spatial, after.Spatial, true),
                EntitiesDestroyed = CountEntityDelta(before.Spatial, after.Spatial, false),

                // Base changes
                NewBases = CountNewBases(before.Spatial, after.Spatial),
            };

            return Result<SaveDifference>.Success(diff);
        }
        catch (Exception ex)
        {
            return Result<SaveDifference>.Failure($"Failed to compare saves: {ex.Message}");
        }
    }

    private static int CountEntityDelta(SpatialData? before, SpatialData? after, bool countNew)
    {
        if (before?.Entities == null || after?.Entities == null)
            return 0;

        var beforeIds = before.Entities.Select(e => e.PersistentId).ToHashSet();
        var afterIds = after.Entities.Select(e => e.PersistentId).ToHashSet();

        return countNew
            ? afterIds.Except(beforeIds).Count()
            : beforeIds.Except(afterIds).Count();
    }

    private static int CountNewBases(SpatialData? before, SpatialData? after)
    {
        if (before?.BaseCores == null || after?.BaseCores == null)
            return 0;

        var beforeCount = before.BaseCores.Count;
        var afterCount = after.BaseCores.Count;

        return Math.Max(0, afterCount - beforeCount);
    }

    /// <summary>
    /// Generates a summary string for the diff.
    /// </summary>
    public static string GenerateSummary(SaveDifference diff)
    {
        var lines = new List<string>
        {
            $"Session: {diff.BeforeSession} → {diff.AfterSession}",
            $"Play Time: +{diff.PlayTimeDelta:hh\\:mm\\:ss}",
            ""
        };

        if (diff.DataPointsDelta != 0)
            lines.Add($"Data Points: {FormatDelta(diff.DataPointsDelta)}");

        if (!string.IsNullOrEmpty(diff.WaveChange))
            lines.Add($"Cataclysm Wave: {diff.WaveChange}");

        if (diff.EntitiesBuilt > 0)
            lines.Add($"Entities Built: +{diff.EntitiesBuilt}");

        if (diff.EntitiesDestroyed > 0)
            lines.Add($"Entities Lost: -{diff.EntitiesDestroyed}");

        if (diff.NewBases > 0)
            lines.Add($"New Bases: +{diff.NewBases}");

        if (diff.NewUnlockedRecipes.Count > 0)
            lines.Add($"Recipes Unlocked: +{diff.NewUnlockedRecipes.Count}");

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatDelta(int delta) => delta > 0 ? $"+{delta}" : delta.ToString();
}

/// <summary>
/// Represents the difference between two save files.
/// </summary>
public sealed record SaveDifference
{
    public required string BeforeSession { get; init; }
    public required string AfterSession { get; init; }
    public required DateTime BeforeTimestamp { get; init; }
    public required DateTime AfterTimestamp { get; init; }
    public required TimeSpan PlayTimeDelta { get; init; }

    // Corporation deltas
    public int DataPointsDelta { get; init; }
    public int InventorySlotsDelta { get; init; }

    // Crafting deltas
    public IReadOnlyList<string> NewUnlockedRecipes { get; init; } = [];
    public IReadOnlyList<string> NewPickedItems { get; init; } = [];

    // Wave changes (strings)
    public string WaveChange { get; init; } = "";
    public string StageChange { get; init; } = "";

    // Entity deltas
    public int EntitiesBuilt { get; init; }
    public int EntitiesDestroyed { get; init; }
    public int NewBases { get; init; }
}
