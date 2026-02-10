namespace GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Represents a detected rage-click event with full scoring and root cause analysis.
/// </summary>
public sealed record RageClickEvent
{
    /// <summary>
    /// The screen where the rage-click was detected.
    /// </summary>
    public required string ScreenName { get; init; }

    /// <summary>
    /// Hashed identifier of the element involved.
    /// </summary>
    public required string UiElementId { get; init; }

    /// <summary>
    /// The pattern type that was matched.
    /// </summary>
    public required RageClickPattern Pattern { get; init; }

    /// <summary>
    /// Rage intensity score (0-100).
    /// 30-50: mild confusion, 50-75: clear frustration, 75-100: high abandonment risk.
    /// </summary>
    public required int RageIntensity { get; init; }

    /// <summary>
    /// Confidence score (0.0-1.0) based on frequency and pattern match strength.
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// The probable root cause of user frustration.
    /// </summary>
    public required LikelyRootCause RootCause { get; init; }

    /// <summary>
    /// The individual interactions that contributed to this event.
    /// </summary>
    public required IReadOnlyList<InteractionRecord> TriggeringInteractions { get; init; }

    /// <summary>
    /// UTC timestamp when the rage-click event was detected.
    /// </summary>
    public required DateTimeOffset DetectedAt { get; init; }
}
