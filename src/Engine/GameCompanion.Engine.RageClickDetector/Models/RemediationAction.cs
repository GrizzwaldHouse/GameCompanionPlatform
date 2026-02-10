namespace GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Types of auto-remediation actions that can be applied.
/// Restricted to safe, non-intrusive changes per policy:
/// NO popups, NO blocking modals, NO hover-only tooltips.
/// </summary>
public enum RemediationType
{
    AddInlineFeedback,
    ImproveCopyOrLabeling,
    AddVisualAffordance,
    IntroduceMicroGuidance
}

/// <summary>
/// A suggested or applied remediation action for a rage-click event.
/// </summary>
public sealed record RemediationAction
{
    /// <summary>
    /// The type of remediation to apply.
    /// </summary>
    public required RemediationType Type { get; init; }

    /// <summary>
    /// The UI element this remediation targets.
    /// </summary>
    public required string TargetElementId { get; init; }

    /// <summary>
    /// The screen where the remediation should be applied.
    /// </summary>
    public required string ScreenName { get; init; }

    /// <summary>
    /// Human-readable description of the suggested fix.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this action was automatically applied (MODE_B) or only suggested (MODE_A).
    /// </summary>
    public bool WasApplied { get; init; }
}
