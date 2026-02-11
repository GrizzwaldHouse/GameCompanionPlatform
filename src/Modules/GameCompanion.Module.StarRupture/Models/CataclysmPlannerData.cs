namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Comprehensive cataclysm survival planning data.
/// </summary>
public sealed class CataclysmPlan
{
    public required CataclysmState CurrentState { get; init; }
    public required IReadOnlyList<SurvivalTask> Tasks { get; init; }
    public required IReadOnlyList<ResourceRequirement> RequiredResources { get; init; }
    public required IReadOnlyList<DefenseRecommendation> DefenseRecommendations { get; init; }
    public required ReadinessScore Readiness { get; init; }
    public required string NextMilestone { get; init; }
    public required TimeSpan TimeToNextWave { get; init; }
}

/// <summary>
/// A task to complete for cataclysm survival.
/// </summary>
public sealed class SurvivalTask
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required TaskPriority Priority { get; init; }
    public required bool IsCompleted { get; init; }
    public required string Category { get; init; }
    public TimeSpan? EstimatedTime { get; init; }
}

/// <summary>
/// Resource needed for cataclysm preparation.
/// </summary>
public sealed class ResourceRequirement
{
    public required string ResourceType { get; init; }
    public required int RequiredAmount { get; init; }
    public required int CurrentAmount { get; init; }
    public int Deficit => Math.Max(0, RequiredAmount - CurrentAmount);
    public bool IsSatisfied => CurrentAmount >= RequiredAmount;
    public double FulfillmentPercent => RequiredAmount > 0 ? Math.Min(100, (double)CurrentAmount / RequiredAmount * 100) : 100;
}

/// <summary>
/// Defense structure recommendation.
/// </summary>
public sealed class DefenseRecommendation
{
    public required string StructureType { get; init; }
    public required int RecommendedCount { get; init; }
    public required int CurrentCount { get; init; }
    public required WorldPosition SuggestedPosition { get; init; }
    public required string Reason { get; init; }
}

/// <summary>
/// Overall readiness score for cataclysm.
/// </summary>
public sealed class ReadinessScore
{
    public required double OverallScore { get; init; }
    public required double DefenseScore { get; init; }
    public required double ResourceScore { get; init; }
    public required double PowerScore { get; init; }
    public required string Assessment { get; init; }
    public required ReadinessLevel Level { get; init; }
}

/// <summary>
/// Readiness level classification.
/// </summary>
public enum ReadinessLevel
{
    Unprepared,
    Minimal,
    Adequate,
    WellPrepared,
    Fortified
}

/// <summary>
/// Task priority for survival tasks.
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}
