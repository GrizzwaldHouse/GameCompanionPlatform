namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Result of ratio calculation for target production.
/// </summary>
public sealed class RatioCalculation
{
    public required string TargetItem { get; init; }
    public required double TargetRate { get; init; } // Items per minute
    public required IReadOnlyList<MachineRequirement> Requirements { get; init; }
    public required IReadOnlyList<ComparisonDelta> CurrentVsRequired { get; init; }
    public required bool CanAchieveTarget { get; init; }
    public string? BottleneckReason { get; init; }
}

/// <summary>
/// Required machines for a production target.
/// </summary>
public sealed class MachineRequirement
{
    public required string MachineType { get; init; }
    public required string ProducesItem { get; init; }
    public required int RequiredCount { get; init; }
    public required double ProductionRate { get; init; } // Per machine per minute
    public required IReadOnlyList<InputRequirement> Inputs { get; init; }
}

/// <summary>
/// Input resource requirement.
/// </summary>
public sealed class InputRequirement
{
    public required string ItemType { get; init; }
    public required double RequiredRate { get; init; } // Per minute
}

/// <summary>
/// Comparison between current build and required.
/// </summary>
public sealed class ComparisonDelta
{
    public required string MachineType { get; init; }
    public required int CurrentCount { get; init; }
    public required int RequiredCount { get; init; }
    public required DeltaStatus Status { get; init; }

    public int Delta => RequiredCount - CurrentCount;
}

/// <summary>
/// Status of current vs required comparison.
/// </summary>
public enum DeltaStatus
{
    Sufficient,   // Have enough or more
    NeedMore,     // Need to build more
    Excess        // Have more than needed
}
