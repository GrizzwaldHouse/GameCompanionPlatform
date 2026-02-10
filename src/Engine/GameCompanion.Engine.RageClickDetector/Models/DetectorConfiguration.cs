namespace GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Configuration for the rage-click detector thresholds.
/// All defaults match the specification heuristics.
/// </summary>
public sealed record DetectorConfiguration
{
    /// <summary>
    /// Minimum number of clicks on the same element to trigger rapid repeat detection.
    /// Default: 3.
    /// </summary>
    public int RapidRepeatClickMinCount { get; init; } = 3;

    /// <summary>
    /// Maximum time window for rapid repeat click detection.
    /// Default: 2 seconds.
    /// </summary>
    public TimeSpan RapidRepeatClickWindow { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Minimum number of oscillation cycles for navigation pattern detection.
    /// Default: 2.
    /// </summary>
    public int OscillationMinCycles { get; init; } = 2;

    /// <summary>
    /// Maximum time window for oscillating navigation detection.
    /// Default: 5 seconds.
    /// </summary>
    public TimeSpan OscillationWindow { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Minimum number of failed form submissions to trigger loop detection.
    /// Default: 2.
    /// </summary>
    public int FormSubmissionFailureMinCount { get; init; } = 2;

    /// <summary>
    /// Minimum number of clicks on disabled/non-interactive UI for dead-end detection.
    /// Default: 2.
    /// </summary>
    public int DeadEndMinClicks { get; init; } = 2;

    /// <summary>
    /// Maximum time window for dead-end interaction detection.
    /// Default: 3 seconds.
    /// </summary>
    public TimeSpan DeadEndWindow { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Maximum number of interaction records to retain in the rolling buffer.
    /// </summary>
    public int MaxBufferSize { get; init; } = 1000;
}
