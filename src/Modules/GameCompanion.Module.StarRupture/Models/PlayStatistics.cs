namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Aggregated play statistics computed from save data and session history.
/// </summary>
public sealed class PlayStatistics
{
    // Time stats
    public required TimeSpan TotalPlayTime { get; init; }
    public required TimeSpan AverageSessionLength { get; init; }
    public required TimeSpan LongestSession { get; init; }
    public required int TotalSessions { get; init; }

    // Progression stats
    public required double OverallProgress { get; init; }
    public required int BlueprintsUnlocked { get; init; }
    public required int BlueprintsTotal { get; init; }
    public required int DataPointsEarned { get; init; }
    public required int UniqueItemsDiscovered { get; init; }

    // Corporation stats
    public required int HighestCorporationLevel { get; init; }
    public required string HighestCorporationName { get; init; }
    public required int TotalCorporationXP { get; init; }

    // Building stats
    public required int TotalBuildingsPlaced { get; init; }
    public required int OperationalBuildings { get; init; }
    public required int DisabledBuildings { get; init; }
    public required int MalfunctioningBuildings { get; init; }

    // Achievement stats
    public required int BadgesEarned { get; init; }
    public required int BadgesTotal { get; init; }

    // Phase info
    public required string CurrentPhase { get; init; }
    public required int CurrentWave { get; init; }

    // Efficiency metrics
    public double BuildingEfficiency => TotalBuildingsPlaced > 0 ? (double)OperationalBuildings / TotalBuildingsPlaced : 0;
    public double BlueprintCompletion => BlueprintsTotal > 0 ? (double)BlueprintsUnlocked / BlueprintsTotal : 0;
    public double BadgeCompletion => BadgesTotal > 0 ? (double)BadgesEarned / BadgesTotal : 0;

    // Computed display helpers
    public string PlayTimeDisplay => $"{(int)TotalPlayTime.TotalHours}h {TotalPlayTime.Minutes}m";
    public string EfficiencyDisplay => $"{BuildingEfficiency:P0}";
}
