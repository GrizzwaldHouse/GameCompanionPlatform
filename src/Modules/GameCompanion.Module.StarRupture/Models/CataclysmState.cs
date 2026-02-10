namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Current cataclysm timer state derived from EnviroWave data.
/// </summary>
public sealed class CataclysmState
{
    public required string CurrentWave { get; init; }
    public required string CurrentStage { get; init; }
    public required double StageProgress { get; init; }
    public required TimeSpan EstimatedTimeRemaining { get; init; }
    public required CataclysmUrgency Urgency { get; init; }
    public string TimeRemainingDisplay => EstimatedTimeRemaining.TotalMinutes >= 1
        ? $"{(int)EstimatedTimeRemaining.TotalMinutes}m {EstimatedTimeRemaining.Seconds}s"
        : $"{EstimatedTimeRemaining.Seconds}s";
}

/// <summary>
/// Urgency level for cataclysm timer.
/// </summary>
public enum CataclysmUrgency
{
    Safe,       // > 15 minutes
    Caution,    // 5-15 minutes
    Warning,    // 1-5 minutes
    Critical    // < 1 minute
}
