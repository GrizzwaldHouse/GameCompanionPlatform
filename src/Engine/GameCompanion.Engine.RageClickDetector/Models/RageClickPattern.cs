namespace GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Categorizes the type of rage-click pattern detected.
/// </summary>
public enum RageClickPattern
{
    /// <summary>
    /// 3+ clicks on the same element within 2 seconds with no state change.
    /// </summary>
    RapidRepeatClick,

    /// <summary>
    /// Back-forward or open-close oscillation repeated 2+ times within 5 seconds.
    /// </summary>
    OscillatingNavigation,

    /// <summary>
    /// Submit button clicked 2+ times with persistent validation errors and no new guidance.
    /// </summary>
    FormSubmissionFailureLoop,

    /// <summary>
    /// 2+ clicks on disabled or non-interactive UI within 3 seconds.
    /// </summary>
    DeadEndInteraction
}
