namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Build planner data for planning factory layouts.
/// </summary>
public sealed class BuildPlan
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required IReadOnlyList<PlannedBuilding> Buildings { get; init; }
    public required IReadOnlyList<PlannedConnection> Connections { get; init; }
    public required BuildPlanStats Stats { get; init; }
    public required BuildPlanCost TotalCost { get; init; }
}

/// <summary>
/// A building planned but not yet built.
/// </summary>
public sealed class PlannedBuilding
{
    public required string Id { get; init; }
    public required string BuildingType { get; init; }
    public required WorldPosition Position { get; init; }
    public required double Rotation { get; init; }
    public string? Recipe { get; init; }
    public bool IsBuilt { get; init; }
    public required BuildingCost Cost { get; init; }
}

/// <summary>
/// A planned connection (conveyor, pipe, etc).
/// </summary>
public sealed class PlannedConnection
{
    public required string Id { get; init; }
    public required string ConnectionType { get; init; }
    public required string FromBuildingId { get; init; }
    public required string ToBuildingId { get; init; }
    public required IReadOnlyList<WorldPosition> Waypoints { get; init; }
    public bool IsBuilt { get; init; }
}

/// <summary>
/// Cost to build a single building.
/// </summary>
public sealed class BuildingCost
{
    public required IReadOnlyDictionary<string, int> Resources { get; init; }
}

/// <summary>
/// Total cost for a build plan.
/// </summary>
public sealed class BuildPlanCost
{
    public required IReadOnlyDictionary<string, int> TotalResources { get; init; }
    public required IReadOnlyDictionary<string, int> CurrentResources { get; init; }
    public required IReadOnlyDictionary<string, int> MissingResources { get; init; }
    public bool CanAfford => MissingResources.Count == 0 || MissingResources.Values.All(v => v <= 0);
    public double FulfillmentPercent
    {
        get
        {
            var total = TotalResources.Values.Sum();
            if (total == 0) return 100;
            var missing = MissingResources.Values.Where(v => v > 0).Sum();
            return (1 - (double)missing / total) * 100;
        }
    }
}

/// <summary>
/// Statistics about a build plan.
/// </summary>
public sealed class BuildPlanStats
{
    public required int TotalBuildings { get; init; }
    public required int BuiltCount { get; init; }
    public required int RemainingCount { get; init; }
    public required double EstimatedPowerUsage { get; init; }
    public required double EstimatedThroughput { get; init; }
    public double CompletionPercent => TotalBuildings > 0
        ? (double)BuiltCount / TotalBuildings * 100
        : 0;
}

/// <summary>
/// Template for common factory layouts.
/// </summary>
public sealed class BuildTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string OutputItem { get; init; }
    public required double OutputRate { get; init; }
    public required IReadOnlyList<PlannedBuilding> Buildings { get; init; }
    public required BuildPlanCost EstimatedCost { get; init; }
    public string? ThumbnailPath { get; init; }
}
