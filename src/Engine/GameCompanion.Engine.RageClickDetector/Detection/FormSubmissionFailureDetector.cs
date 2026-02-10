namespace GameCompanion.Engine.RageClickDetector.Detection;

using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Scoring;

/// <summary>
/// Detects form submission failure loops: Submit button clicked 2+ times with
/// persistent validation errors and no new guidance shown.
/// </summary>
public sealed class FormSubmissionFailureDetector : IPatternDetector
{
    public RageClickPattern PatternType => RageClickPattern.FormSubmissionFailureLoop;

    public IReadOnlyList<RageClickEvent> Detect(
        IReadOnlyList<InteractionRecord> interactions,
        DetectorConfiguration config)
    {
        var events = new List<RageClickEvent>();

        var submitInteractions = interactions
            .Where(i => i.InteractionType == InteractionType.Submit)
            .OrderBy(i => i.Timestamp)
            .ToList();

        // Group by element and screen
        var byElement = submitInteractions.GroupBy(i => new { i.UiElementId, i.ScreenName });

        foreach (var group in byElement)
        {
            var ordered = group.ToList();

            // Find consecutive failed submissions without new guidance
            var failedRun = new List<InteractionRecord>();

            foreach (var interaction in ordered)
            {
                if (interaction.ResultedInValidationError && !interaction.NewGuidanceShown)
                {
                    failedRun.Add(interaction);
                }
                else
                {
                    if (failedRun.Count >= config.FormSubmissionFailureMinCount)
                    {
                        events.Add(CreateEvent(failedRun, config));
                    }
                    failedRun.Clear();
                }
            }

            // Check the final run
            if (failedRun.Count >= config.FormSubmissionFailureMinCount)
            {
                events.Add(CreateEvent(failedRun, config));
            }
        }

        return events;
    }

    private RageClickEvent CreateEvent(
        List<InteractionRecord> failedSubmissions,
        DetectorConfiguration config)
    {
        var intensity = UxRiskScorer.CalculateFormFailureIntensity(
            failedSubmissions.Count, config.FormSubmissionFailureMinCount);

        var confidence = UxRiskScorer.CalculateConfidence(
            failedSubmissions.Count, config.FormSubmissionFailureMinCount, PatternType);

        return new RageClickEvent
        {
            ScreenName = failedSubmissions.First().ScreenName,
            UiElementId = failedSubmissions.First().UiElementId,
            Pattern = PatternType,
            RageIntensity = intensity,
            Confidence = confidence,
            RootCause = RootCauseAnalyzer.Analyze(PatternType, failedSubmissions),
            TriggeringInteractions = failedSubmissions,
            DetectedAt = DateTimeOffset.UtcNow
        };
    }
}
