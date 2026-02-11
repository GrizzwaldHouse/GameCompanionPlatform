namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Optimal research path recommendations.
/// </summary>
public sealed class ResearchPath
{
    public required IReadOnlyList<ResearchStep> RecommendedPath { get; init; }
    public required IReadOnlyList<ResearchNode> HighPriorityUnlocks { get; init; }
    public required IReadOnlyList<ResearchNode> CurrentlyAvailable { get; init; }
    public required ResearchGoal CurrentGoal { get; init; }
    public required int EstimatedDataPointsNeeded { get; init; }
    public required double CompletionPercent { get; init; }
}

/// <summary>
/// A single step in the research path.
/// </summary>
public sealed class ResearchStep
{
    public required int Order { get; init; }
    public required ResearchNode Node { get; init; }
    public required string Reason { get; init; }
    public required IReadOnlyList<string> UnlocksAbilities { get; init; }
    public required IReadOnlyList<string> Prerequisites { get; init; }
    public bool IsUnlocked { get; init; }
    public int DataPointsCost { get; init; }
}

/// <summary>
/// Current research goal being worked towards.
/// </summary>
public sealed class ResearchGoal
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ResearchGoalType Type { get; init; }
    public required IReadOnlyList<string> RequiredResearch { get; init; }
    public required int CompletedSteps { get; init; }
    public required int TotalSteps { get; init; }
    public double ProgressPercent => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;
}

/// <summary>
/// Type of research goal.
/// </summary>
public enum ResearchGoalType
{
    Automation,
    Defense,
    Production,
    Exploration,
    PowerGeneration,
    Logistics,
    Custom
}
