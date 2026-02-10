namespace GameCompanion.Engine.RageClickDetector.Remediation;

using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Generates remediation suggestions for rage-click events.
/// In MODE_B (AUTO_CODE_CHANGE), may apply changes directly.
///
/// Allowed actions:
/// - Add inline feedback (loading, success, error)
/// - Improve copy or button labeling
/// - Add visual affordances (disabled state clarity)
/// - Introduce micro-guidance (helper text)
///
/// Prohibited:
/// - No popups
/// - No blocking modals
/// - No tooltips that require hover (mobile-unfriendly)
/// </summary>
public sealed class AutoRemediationEngine
{
    /// <summary>
    /// Generates remediation actions for a set of rage-click events.
    /// </summary>
    public IReadOnlyList<RemediationAction> GenerateRemediations(
        IReadOnlyList<RageClickEvent> events)
    {
        var remediations = new List<RemediationAction>();

        foreach (var evt in events)
        {
            remediations.AddRange(GenerateForEvent(evt));
        }

        return remediations;
    }

    private static IEnumerable<RemediationAction> GenerateForEvent(RageClickEvent evt)
    {
        return evt.RootCause switch
        {
            LikelyRootCause.MissingFeedback => GenerateFeedbackRemediations(evt),
            LikelyRootCause.UnclearCopy => GenerateCopyRemediations(evt),
            LikelyRootCause.DisabledStateAmbiguity => GenerateAffordanceRemediations(evt),
            LikelyRootCause.ValidationOpacity => GenerateValidationRemediations(evt),
            LikelyRootCause.NavigationAmbiguity => GenerateNavigationRemediations(evt),
            _ => []
        };
    }

    private static IEnumerable<RemediationAction> GenerateFeedbackRemediations(RageClickEvent evt)
    {
        yield return new RemediationAction
        {
            Type = RemediationType.AddInlineFeedback,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Add inline loading/success/error indicator to element on '{evt.ScreenName}' " +
                          $"to provide immediate visual response after interaction."
        };
    }

    private static IEnumerable<RemediationAction> GenerateCopyRemediations(RageClickEvent evt)
    {
        yield return new RemediationAction
        {
            Type = RemediationType.ImproveCopyOrLabeling,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Review and clarify button/label text on '{evt.ScreenName}' " +
                          $"to make the expected action and outcome more obvious."
        };
    }

    private static IEnumerable<RemediationAction> GenerateAffordanceRemediations(RageClickEvent evt)
    {
        yield return new RemediationAction
        {
            Type = RemediationType.AddVisualAffordance,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Improve disabled state visual clarity on '{evt.ScreenName}': " +
                          $"add distinct styling (greyed out, reduced opacity) and " +
                          $"inline text explaining why the element is unavailable."
        };

        yield return new RemediationAction
        {
            Type = RemediationType.IntroduceMicroGuidance,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Add helper text near disabled element on '{evt.ScreenName}' " +
                          $"explaining what conditions must be met to enable it."
        };
    }

    private static IEnumerable<RemediationAction> GenerateValidationRemediations(RageClickEvent evt)
    {
        yield return new RemediationAction
        {
            Type = RemediationType.AddInlineFeedback,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Add field-level inline validation messages on '{evt.ScreenName}' " +
                          $"that clearly explain what needs to be corrected."
        };

        yield return new RemediationAction
        {
            Type = RemediationType.IntroduceMicroGuidance,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Add progressive disclosure guidance on '{evt.ScreenName}': " +
                          $"after repeated validation failures, show increasingly helpful hints."
        };
    }

    private static IEnumerable<RemediationAction> GenerateNavigationRemediations(RageClickEvent evt)
    {
        yield return new RemediationAction
        {
            Type = RemediationType.ImproveCopyOrLabeling,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Clarify navigation labels/breadcrumbs on '{evt.ScreenName}' " +
                          $"to reduce back-and-forth confusion."
        };

        yield return new RemediationAction
        {
            Type = RemediationType.AddInlineFeedback,
            TargetElementId = evt.UiElementId,
            ScreenName = evt.ScreenName,
            Description = $"Add visual navigation state indicator on '{evt.ScreenName}' " +
                          $"to clearly show current position and available destinations."
        };
    }
}
