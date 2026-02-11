namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Challenge mode tracking data.
/// </summary>
public sealed class ChallengeTracker
{
    public required IReadOnlyList<Challenge> Challenges { get; init; }
    public required int CompletedCount { get; init; }
    public required int TotalCount { get; init; }
    public required double CompletionPercent { get; init; }
    public required IReadOnlyList<ChallengeProgress> InProgress { get; init; }
}

/// <summary>
/// A challenge definition.
/// </summary>
public sealed class Challenge
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ChallengeCategory Category { get; init; }
    public required ChallengeDifficulty Difficulty { get; init; }
    public required IReadOnlyList<ChallengeObjective> Objectives { get; init; }
    public required IReadOnlyList<ChallengeReward> Rewards { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// An objective within a challenge.
/// </summary>
public sealed class ChallengeObjective
{
    public required string Description { get; init; }
    public required int Target { get; init; }
    public required int Current { get; init; }
    public bool IsCompleted => Current >= Target;
    public double Progress => Target > 0 ? Math.Min(100, (double)Current / Target * 100) : 0;
}

/// <summary>
/// A reward for completing a challenge.
/// </summary>
public sealed class ChallengeReward
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required int Amount { get; init; }
}

/// <summary>
/// Progress on an active challenge.
/// </summary>
public sealed class ChallengeProgress
{
    public required Challenge Challenge { get; init; }
    public required double OverallProgress { get; init; }
    public required IReadOnlyList<ChallengeObjective> ObjectiveProgress { get; init; }
    public required TimeSpan TimeSpent { get; init; }
}

/// <summary>
/// Challenge categories.
/// </summary>
public enum ChallengeCategory
{
    Production,
    Survival,
    Exploration,
    Efficiency,
    Speed,
    Creative
}

/// <summary>
/// Challenge difficulty levels.
/// </summary>
public enum ChallengeDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert,
    Insane
}
