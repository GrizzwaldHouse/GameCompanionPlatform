namespace GameCompanion.Engine.RageClickDetector.Scoring;

using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Analyzes rage-click events to determine the most likely root cause of user frustration.
/// </summary>
public static class RootCauseAnalyzer
{
    /// <summary>
    /// Determines the likely root cause based on the pattern type and triggering interactions.
    /// </summary>
    public static LikelyRootCause Analyze(
        RageClickPattern pattern,
        IReadOnlyList<InteractionRecord> interactions)
    {
        return pattern switch
        {
            RageClickPattern.RapidRepeatClick => AnalyzeRapidClicks(interactions),
            RageClickPattern.OscillatingNavigation => LikelyRootCause.NavigationAmbiguity,
            RageClickPattern.FormSubmissionFailureLoop => AnalyzeFormFailure(interactions),
            RageClickPattern.DeadEndInteraction => LikelyRootCause.DisabledStateAmbiguity,
            _ => LikelyRootCause.MissingFeedback
        };
    }

    private static LikelyRootCause AnalyzeRapidClicks(IReadOnlyList<InteractionRecord> interactions)
    {
        // If clicking on disabled elements, it's state ambiguity
        if (interactions.Any(i => i.TargetWasDisabled))
            return LikelyRootCause.DisabledStateAmbiguity;

        // If no state changes occurred, likely missing feedback
        if (interactions.All(i => !i.CausedStateChange))
            return LikelyRootCause.MissingFeedback;

        return LikelyRootCause.UnclearCopy;
    }

    private static LikelyRootCause AnalyzeFormFailure(IReadOnlyList<InteractionRecord> interactions)
    {
        // All failed with no guidance = validation opacity
        if (interactions.All(i => i.ResultedInValidationError && !i.NewGuidanceShown))
            return LikelyRootCause.ValidationOpacity;

        return LikelyRootCause.UnclearCopy;
    }
}
