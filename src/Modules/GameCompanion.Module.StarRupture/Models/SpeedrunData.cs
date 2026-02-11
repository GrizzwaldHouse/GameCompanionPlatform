namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Speedrun timer and tracking data.
/// </summary>
public sealed class SpeedrunSession
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public required IReadOnlyList<SpeedrunSplit> Splits { get; init; }
    public required SpeedrunComparison? Comparison { get; init; }
    public TimeSpan CurrentTime => EndTime.HasValue
        ? EndTime.Value - StartTime
        : DateTime.Now - StartTime;
    public bool IsRunning => !EndTime.HasValue;
}

/// <summary>
/// A split (checkpoint) in a speedrun.
/// </summary>
public sealed class SpeedrunSplit
{
    public required string Name { get; init; }
    public required int Order { get; init; }
    public TimeSpan? SplitTime { get; init; }
    public TimeSpan? PersonalBest { get; init; }
    public TimeSpan? GoldSplit { get; init; }
    public bool IsCompleted => SplitTime.HasValue;
    public SplitDelta? Delta => SplitTime.HasValue && PersonalBest.HasValue
        ? new SplitDelta
        {
            Time = SplitTime.Value - PersonalBest.Value,
            IsAhead = SplitTime.Value < PersonalBest.Value
        }
        : null;
}

/// <summary>
/// Delta compared to personal best.
/// </summary>
public sealed class SplitDelta
{
    public required TimeSpan Time { get; init; }
    public required bool IsAhead { get; init; }
    public string Display => $"{(IsAhead ? "-" : "+")}{Time:mm\\:ss\\.ff}";
}

/// <summary>
/// Comparison data for a speedrun.
/// </summary>
public sealed class SpeedrunComparison
{
    public required TimeSpan PersonalBest { get; init; }
    public required TimeSpan SumOfBest { get; init; }
    public TimeSpan? WorldRecord { get; init; }
    public required DateTime PersonalBestDate { get; init; }
}

/// <summary>
/// Speedrun leaderboard entry.
/// </summary>
public sealed class LeaderboardEntry
{
    public required int Rank { get; init; }
    public required string PlayerName { get; init; }
    public required TimeSpan Time { get; init; }
    public required DateTime Date { get; init; }
    public required string Category { get; init; }
    public bool IsPersonalBest { get; init; }
}

/// <summary>
/// Speedrun categories available.
/// </summary>
public sealed class SpeedrunCategory
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> SplitNames { get; init; }
    public TimeSpan? PersonalBest { get; init; }
}
