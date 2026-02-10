namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Aggregated player progress extracted from save file.
/// </summary>
public sealed class PlayerProgress
{
    public required string SessionName { get; init; }
    public required TimeSpan TotalPlayTime { get; init; }

    // Progression Phase
    public required ProgressionPhase CurrentPhase { get; init; }
    public required double OverallProgress { get; init; } // 0.0 to 1.0

    // Blueprints
    public required int BlueprintsUnlocked { get; init; }
    public required int BlueprintsTotal { get; init; }
    public double BlueprintProgress => BlueprintsTotal > 0 ? (double)BlueprintsUnlocked / BlueprintsTotal : 0;

    // Corporations
    public required int DataPointsEarned { get; init; }
    public required int HighestCorporationLevel { get; init; }
    public required string HighestCorporationName { get; init; }
    public required bool MapUnlocked { get; init; } // Moon Energy Level 3+
    public required IReadOnlyList<CorporationInfo> Corporations { get; init; }

    // Items
    public required int UniqueItemsDiscovered { get; init; }

    // Enviro Wave
    public required string CurrentWave { get; init; }
    public required string CurrentWaveStage { get; init; }

    // Achievements earned
    public required IReadOnlyList<Badge> EarnedBadges { get; init; }
}

/// <summary>
/// Game progression phase.
/// </summary>
public enum ProgressionPhase
{
    EarlyGame,      // First base, basic resources
    MidGame,        // Factory automation, blueprint hunting
    EndGame,        // Expansion, defense, advanced production
    Mastery         // All blueprints, max corporations
}

/// <summary>
/// Achievement badge.
/// </summary>
public sealed class Badge
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required BadgeRarity Rarity { get; init; }
    public DateTime? EarnedAt { get; init; }
}

/// <summary>
/// Badge rarity level.
/// </summary>
public enum BadgeRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
