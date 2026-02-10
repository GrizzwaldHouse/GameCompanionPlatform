namespace GameCompanion.Engine.RageClickDetector.Validation;

using GameCompanion.Engine.RageClickDetector.Detection;
using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Re-runs rage-click detection against a modified interaction set to validate
/// that remediations reduce frustration. Reports delta in rage intensity and
/// flags any remaining high-confidence frustration points.
/// </summary>
public sealed class RageClickSimulator
{
    private readonly IReadOnlyList<IPatternDetector> _detectors;
    private readonly DetectorConfiguration _config;

    public RageClickSimulator(
        IReadOnlyList<IPatternDetector> detectors,
        DetectorConfiguration config)
    {
        _detectors = detectors;
        _config = config;
    }

    /// <summary>
    /// Simulates rage-click detection on a modified interaction set
    /// where remediation effects are applied (e.g., state changes now occur,
    /// guidance is now shown, disabled states are clearer).
    /// </summary>
    public ValidationDelta Validate(
        IReadOnlyList<RageClickEvent> originalEvents,
        IReadOnlyList<InteractionRecord> remediatedInteractions)
    {
        // Run all detectors against the remediated interaction set
        var postRemediationEvents = new List<RageClickEvent>();
        foreach (var detector in _detectors)
        {
            postRemediationEvents.AddRange(detector.Detect(remediatedInteractions, _config));
        }

        double avgBefore = originalEvents.Count > 0
            ? originalEvents.Average(e => e.RageIntensity)
            : 0;

        double avgAfter = postRemediationEvents.Count > 0
            ? postRemediationEvents.Average(e => e.RageIntensity)
            : 0;

        // High-confidence threshold: confidence >= 0.7 and intensity >= 50
        var remainingHighConfidence = postRemediationEvents
            .Where(e => e.Confidence >= 0.7 && e.RageIntensity >= 50)
            .ToList();

        return new ValidationDelta
        {
            AverageIntensityBefore = avgBefore,
            AverageIntensityAfter = avgAfter,
            RemainingHighConfidenceEvents = remainingHighConfidence
        };
    }

    /// <summary>
    /// Creates a modified interaction set simulating the effect of remediations.
    /// For each remediation applied, the corresponding interactions are adjusted:
    /// - AddInlineFeedback: CausedStateChange set to true
    /// - ImproveCopyOrLabeling: no direct interaction change (reduces future confusion)
    /// - AddVisualAffordance: TargetWasDisabled interactions have CausedStateChange=true
    /// - IntroduceMicroGuidance: NewGuidanceShown set to true
    /// </summary>
    public static IReadOnlyList<InteractionRecord> ApplyRemediationEffects(
        IReadOnlyList<InteractionRecord> originalInteractions,
        IReadOnlyList<RemediationAction> remediations)
    {
        var remediatedSet = new HashSet<string>(
            remediations.Select(r => $"{r.ScreenName}:{r.TargetElementId}"));

        var remediationTypes = remediations
            .GroupBy(r => $"{r.ScreenName}:{r.TargetElementId}")
            .ToDictionary(g => g.Key, g => g.Select(r => r.Type).ToHashSet());

        return originalInteractions.Select(i =>
        {
            var key = $"{i.ScreenName}:{i.UiElementId}";
            if (!remediatedSet.Contains(key))
                return i;

            var types = remediationTypes[key];

            return i with
            {
                CausedStateChange = i.CausedStateChange
                    || types.Contains(RemediationType.AddInlineFeedback)
                    || (i.TargetWasDisabled && types.Contains(RemediationType.AddVisualAffordance)),
                NewGuidanceShown = i.NewGuidanceShown
                    || types.Contains(RemediationType.IntroduceMicroGuidance)
            };
        }).ToList();
    }
}
