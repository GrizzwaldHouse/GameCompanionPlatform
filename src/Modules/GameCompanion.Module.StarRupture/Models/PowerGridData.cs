namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Analysis result for power grid health.
/// </summary>
public sealed class PowerGridAnalysis
{
    public required IReadOnlyList<GridNetwork> Networks { get; init; }
    public required double TotalGeneration { get; init; }
    public required double TotalConsumption { get; init; }
    public required GridStatus OverallStatus { get; init; }
    public required IReadOnlyList<PowerWarning> Warnings { get; init; }
    public required IReadOnlyList<GeneratorPlacement> PlacementSuggestions { get; init; }

    public double PowerBalance => TotalGeneration - TotalConsumption;
    public double UtilizationPercent => TotalGeneration > 0 ? (TotalConsumption / TotalGeneration) * 100 : 0;
}

/// <summary>
/// A single power network (subgraph).
/// </summary>
public sealed class GridNetwork
{
    public required int NetworkId { get; init; }
    public required IReadOnlyList<PowerNode> Nodes { get; init; }
    public required double Generation { get; init; }
    public required double Consumption { get; init; }
    public required GridStatus Status { get; init; }
    public required bool IsBrownoutRisk { get; init; }
}

/// <summary>
/// A node in the power network.
/// </summary>
public sealed class PowerNode
{
    public required int EntityId { get; init; }
    public required string EntityType { get; init; }
    public required WorldPosition Position { get; init; }
    public required bool IsGenerator { get; init; }
    public required double PowerValue { get; init; } // Positive = generation, negative = consumption
}

/// <summary>
/// A warning about the power grid.
/// </summary>
public sealed class PowerWarning
{
    public required int NetworkId { get; init; }
    public required WarningSeverity Severity { get; init; }
    public required string Message { get; init; }
    public required string Suggestion { get; init; }
}

/// <summary>
/// Suggested generator placement.
/// </summary>
public sealed class GeneratorPlacement
{
    public required int TargetNetworkId { get; init; }
    public required WorldPosition SuggestedPosition { get; init; }
    public required string RecommendedGeneratorType { get; init; }
    public required double PowerDeficit { get; init; }
}

/// <summary>
/// Status of a power grid.
/// </summary>
public enum GridStatus
{
    Healthy,       // Generation > Consumption with buffer
    Stable,        // Generation >= Consumption
    Strained,      // Generation barely meets consumption
    Brownout,      // Consumption exceeds generation
    Disconnected   // No generators in network
}

/// <summary>
/// Severity of a warning.
/// </summary>
public enum WarningSeverity
{
    Info,
    Warning,
    Critical
}
