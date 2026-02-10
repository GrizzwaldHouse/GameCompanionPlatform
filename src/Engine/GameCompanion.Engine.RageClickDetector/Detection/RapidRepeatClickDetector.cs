namespace GameCompanion.Engine.RageClickDetector.Detection;

using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Scoring;

/// <summary>
/// Detects rapid repeat clicks: 3+ clicks on the same UI element within 2 seconds
/// with no state change or navigation.
/// </summary>
public sealed class RapidRepeatClickDetector : IPatternDetector
{
    public RageClickPattern PatternType => RageClickPattern.RapidRepeatClick;

    public IReadOnlyList<RageClickEvent> Detect(
        IReadOnlyList<InteractionRecord> interactions,
        DetectorConfiguration config)
    {
        var events = new List<RageClickEvent>();

        var clickInteractions = interactions
            .Where(i => i.InteractionType == InteractionType.Click)
            .OrderBy(i => i.Timestamp)
            .ToList();

        // Group by element
        var byElement = clickInteractions.GroupBy(i => new { i.UiElementId, i.ScreenName });

        foreach (var group in byElement)
        {
            var ordered = group.ToList();

            // Sliding window: find sequences of rapid clicks without state change
            for (int i = 0; i <= ordered.Count - config.RapidRepeatClickMinCount; i++)
            {
                var windowStart = ordered[i].Timestamp;
                var windowEnd = windowStart + config.RapidRepeatClickWindow;

                var windowClicks = ordered
                    .Skip(i)
                    .TakeWhile(c => c.Timestamp <= windowEnd)
                    .ToList();

                if (windowClicks.Count >= config.RapidRepeatClickMinCount
                    && windowClicks.All(c => !c.CausedStateChange))
                {
                    var intensity = UxRiskScorer.CalculateRapidClickIntensity(
                        windowClicks.Count,
                        config.RapidRepeatClickMinCount,
                        config.RapidRepeatClickWindow,
                        windowClicks.Last().Timestamp - windowClicks.First().Timestamp);

                    var confidence = UxRiskScorer.CalculateConfidence(
                        windowClicks.Count, config.RapidRepeatClickMinCount, PatternType);

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

                    // Skip past this window to avoid duplicate detections
                    i += windowClicks.Count - 1;
                }
            }
        }

        return events;
    }
}
