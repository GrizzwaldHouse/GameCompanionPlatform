namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Expansion recommendations and analysis.
/// </summary>
public sealed class ExpansionPlan
{
    public required IReadOnlyList<ExpansionSite> RecommendedSites { get; init; }
    public required IReadOnlyList<ResourceDeposit> NearbyResources { get; init; }
    public required ExpansionReadiness Readiness { get; init; }
    public required IReadOnlyList<ExpansionWarning> Warnings { get; init; }
    public required BaseStatistics CurrentBaseStats { get; init; }
}

/// <summary>
/// A recommended site for expansion.
/// </summary>
public sealed class ExpansionSite
{
    public required WorldPosition Position { get; init; }
    public required double Score { get; init; }
    public required string Reason { get; init; }
    public required IReadOnlyList<string> NearbyResources { get; init; }
    public required double DistanceFromMainBase { get; init; }
    public required ExpansionPurpose RecommendedPurpose { get; init; }
    public required IReadOnlyList<string> Advantages { get; init; }
    public required IReadOnlyList<string> Challenges { get; init; }
}

/// <summary>
/// A resource deposit in the world.
/// </summary>
public sealed class ResourceDeposit
{
    public required string ResourceType { get; init; }
    public required WorldPosition Position { get; init; }
    public required bool IsExploited { get; init; }
    public required double DistanceFromBase { get; init; }
    public required ResourceRichness Richness { get; init; }
}

/// <summary>
/// Readiness for expansion.
/// </summary>
public sealed class ExpansionReadiness
{
    public required bool HasRequiredResearch { get; init; }
    public required bool HasSufficientResources { get; init; }
    public required bool HasLogisticsCapacity { get; init; }
    public required bool HasPowerCapacity { get; init; }
    public required IReadOnlyList<string> MissingRequirements { get; init; }
    public bool IsReady => HasRequiredResearch && HasSufficientResources && HasLogisticsCapacity && HasPowerCapacity;
}

/// <summary>
/// Warning about expansion concerns.
/// </summary>
public sealed class ExpansionWarning
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required WarningSeverity Severity { get; init; }
    public required string Mitigation { get; init; }
}

/// <summary>
/// Statistics about current base.
/// </summary>
public sealed class BaseStatistics
{
    public required int TotalBuildings { get; init; }
    public required double BaseArea { get; init; }
    public required int ResourceTypes { get; init; }
    public required double AverageEfficiency { get; init; }
}

/// <summary>
/// Purpose for an expansion.
/// </summary>
public enum ExpansionPurpose
{
    ResourceExtraction,
    Production,
    Defense,
    Logistics,
    Research,
    Mixed
}

/// <summary>
/// Richness level of a resource deposit.
/// </summary>
public enum ResourceRichness
{
    Poor,
    Normal,
    Rich,
    VeryRich
}
