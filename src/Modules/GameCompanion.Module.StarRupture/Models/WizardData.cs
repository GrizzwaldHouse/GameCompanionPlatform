namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// What's Next Wizard recommendations.
/// </summary>
public sealed class WizardRecommendations
{
    public required IReadOnlyList<WizardSuggestion> Suggestions { get; init; }
    public required string CurrentPhase { get; init; }
    public required double ProgressPercent { get; init; }
    public required WizardGoal? PrimaryGoal { get; init; }
    public required IReadOnlyList<WizardGoal> AvailableGoals { get; init; }
}

/// <summary>
/// A wizard suggestion for what to do next.
/// </summary>
public sealed class WizardSuggestion
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required SuggestionPriority Priority { get; init; }
    public required SuggestionCategory Category { get; init; }
    public required IReadOnlyList<string> Steps { get; init; }
    public required string Reasoning { get; init; }
    public TimeSpan? EstimatedTime { get; init; }
    public IReadOnlyList<string> Prerequisites { get; init; } = [];
    public IReadOnlyList<string> Benefits { get; init; } = [];
}

/// <summary>
/// A goal the player can work towards.
/// </summary>
public sealed class WizardGoal
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required GoalType Type { get; init; }
    public required double Progress { get; init; }
    public required IReadOnlyList<GoalMilestone> Milestones { get; init; }
    public bool IsActive { get; init; }
    public bool IsCompleted => Progress >= 100;
}

/// <summary>
/// A milestone within a goal.
/// </summary>
public sealed class GoalMilestone
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required bool IsCompleted { get; init; }
    public required int Order { get; init; }
}

/// <summary>
/// Suggestion priority levels.
/// </summary>
public enum SuggestionPriority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>
/// Suggestion categories.
/// </summary>
public enum SuggestionCategory
{
    Production,
    Defense,
    Research,
    Exploration,
    Power,
    Logistics,
    Optimization
}

/// <summary>
/// Goal types.
/// </summary>
public enum GoalType
{
    Tutorial,
    MainQuest,
    SideQuest,
    Achievement,
    Challenge,
    Personal
}
