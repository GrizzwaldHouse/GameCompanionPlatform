namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Result of bottleneck analysis on production chains.
/// </summary>
public sealed class BottleneckAnalysis
{
    public required IReadOnlyList<BottleneckInfo> Bottlenecks { get; init; }
    public required IReadOnlyList<ProductionChain> Chains { get; init; }
    public required int TotalMachines { get; init; }
    public required int BottleneckCount { get; init; }
    public double EfficiencyScore => TotalMachines > 0 ? 1.0 - ((double)BottleneckCount / TotalMachines) : 1.0;
}

/// <summary>
/// Information about a detected bottleneck.
/// </summary>
public sealed class BottleneckInfo
{
    public required int EntityId { get; init; }
    public required string EntityType { get; init; }
    public required WorldPosition Position { get; init; }
    public required BottleneckSeverity Severity { get; init; }
    public required string Reason { get; init; }
    public required string Recommendation { get; init; }
    public double ThroughputRatio { get; init; }
}

/// <summary>
/// A production chain from inputs to outputs.
/// </summary>
public sealed class ProductionChain
{
    public required string OutputItem { get; init; }
    public required IReadOnlyList<ChainNode> Nodes { get; init; }
    public required double TheoreticalThroughput { get; init; }
    public required double ActualThroughput { get; init; }
    public double Efficiency => TheoreticalThroughput > 0 ? ActualThroughput / TheoreticalThroughput : 0;
}

/// <summary>
/// A node in a production chain.
/// </summary>
public sealed class ChainNode
{
    public required int EntityId { get; init; }
    public required string EntityType { get; init; }
    public required WorldPosition Position { get; init; }
    public required bool IsBottleneck { get; init; }
    public required IReadOnlyList<int> InputsFrom { get; init; }
    public required IReadOnlyList<int> OutputsTo { get; init; }
}

/// <summary>
/// Severity level of a bottleneck.
/// </summary>
public enum BottleneckSeverity
{
    Low,      // Minor impact, optimization opportunity
    Medium,   // Noticeable impact on production
    High,     // Significant production loss
    Critical  // Major blockage, immediate attention needed
}
