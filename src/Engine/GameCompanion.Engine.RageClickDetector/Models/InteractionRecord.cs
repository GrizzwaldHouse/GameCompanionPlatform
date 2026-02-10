namespace GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// A single user interaction event captured by the detector.
/// All fields are privacy-safe: no user identity, input text, IP, or device fingerprinting.
/// </summary>
public sealed record InteractionRecord
{
    /// <summary>
    /// Ephemeral, anonymized session identifier. Not linked to user identity.
    /// </summary>
    public required string AnonymizedSessionId { get; init; }

    /// <summary>
    /// Hashed identifier of the UI element that was interacted with.
    /// </summary>
    public required string UiElementId { get; init; }

    /// <summary>
    /// The type of interaction (click, submit, navigation).
    /// </summary>
    public required InteractionType InteractionType { get; init; }

    /// <summary>
    /// UTC timestamp of the interaction.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Name of the screen/view where the interaction occurred.
    /// </summary>
    public required string ScreenName { get; init; }

    /// <summary>
    /// Whether the interaction caused a state change in the UI.
    /// </summary>
    public bool CausedStateChange { get; init; }

    /// <summary>
    /// Whether the target element was in a disabled state.
    /// </summary>
    public bool TargetWasDisabled { get; init; }

    /// <summary>
    /// Whether the interaction resulted in a validation error being displayed.
    /// </summary>
    public bool ResultedInValidationError { get; init; }

    /// <summary>
    /// Whether new guidance was shown to the user after this interaction.
    /// </summary>
    public bool NewGuidanceShown { get; init; }

    /// <summary>
    /// Navigation direction, if this was a navigation interaction.
    /// </summary>
    public NavigationDirection? Direction { get; init; }
}

/// <summary>
/// Direction of a navigation interaction for oscillation detection.
/// </summary>
public enum NavigationDirection
{
    Forward,
    Back,
    Open,
    Close
}
