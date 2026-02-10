namespace GameCompanion.Engine.RageClickDetector.Detection;

using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Scoring;

/// <summary>
/// Detects dead-end interactions: 2+ clicks on disabled or non-interactive UI
/// within 3 seconds.
/// </summary>
public sealed class DeadEndInteractionDetector : IPatternDetector
{
    public RageClickPattern PatternType => RageClickPattern.DeadEndInteraction;

    public IReadOnlyList<RageClickEvent> Detect(
        IReadOnlyList<InteractionRecord> interactions,
        DetectorConfiguration config)
    {
        var events = new List<RageClickEvent>();

        var disabledClicks = interactions
            .Where(i => i.InteractionType == InteractionType.Click && i.TargetWasDisabled)
            .OrderBy(i => i.Timestamp)
            .ToList();

        // Group by element
        var byElement = disabledClicks.GroupBy(i => new { i.UiElementId, i.ScreenName });

        foreach (var group in byElement)
        {
            var ordered = group.ToList();

            for (int i = 0; i <= ordered.Count - config.DeadEndMinClicks; i++)
            {
                var windowStart = ordered[i].Timestamp;
                var windowEnd = windowStart + config.DeadEndWindow;

                var windowClicks = ordered
                    .Skip(i)
                    .TakeWhile(c => c.Timestamp <= windowEnd)
                    .ToList();

                if (windowClicks.Count >= config.DeadEndMinClicks)
                {
                    var intensity = UxRiskScorer.CalculateDeadEndIntensity(
                        windowClicks.Count, config.DeadEndMinClicks);

                    var confidence = UxRiskScorer.CalculateConfidence(
                        windowClicks.Count, config.DeadEndMinClicks, PatternType);

                    events.Add(new RageClickEvent
                    {
                        ScreenName = group.Key.ScreenName,
                        UiElementId = group.Key.UiElementId,
                        Pattern = PatternType,
                        RageIntensity = intensity,
                        Confidence = confidence,
                        RootCause = RootCauseAnalyzer.Analyze(PatternType, windowClicks),
                        TriggeringInteractions = windowClicks,
                        DetectedAt = DateTimeOffset.UtcNow
                    });

                    i += windowClicks.Count - 1;
                }
            }
        }

        return events;
    }
}
