namespace GameCompanion.Engine.RageClickDetector.Detection;

using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Scoring;

/// <summary>
/// Detects oscillating navigation: Back-Forward-Back or Open-Close-Open repeated 2+ times
/// within 5 seconds.
/// </summary>
public sealed class OscillatingNavigationDetector : IPatternDetector
{
    public RageClickPattern PatternType => RageClickPattern.OscillatingNavigation;

    public IReadOnlyList<RageClickEvent> Detect(
        IReadOnlyList<InteractionRecord> interactions,
        DetectorConfiguration config)
    {
        var events = new List<RageClickEvent>();

        var navInteractions = interactions
            .Where(i => i.InteractionType == InteractionType.Navigation && i.Direction.HasValue)
            .OrderBy(i => i.Timestamp)
            .ToList();

        if (navInteractions.Count < config.OscillationMinCycles * 2 + 1)
            return events;

        for (int i = 0; i <= navInteractions.Count - 3; i++)
        {
            var windowStart = navInteractions[i].Timestamp;
            var windowEnd = windowStart + config.OscillationWindow;

            var windowNav = navInteractions
                .Skip(i)
                .TakeWhile(n => n.Timestamp <= windowEnd)
                .ToList();

            int cycles = CountOscillationCycles(windowNav);

            if (cycles >= config.OscillationMinCycles)
            {
                var intensity = UxRiskScorer.CalculateOscillationIntensity(
                    cycles, config.OscillationMinCycles);

                var confidence = UxRiskScorer.CalculateConfidence(
                    cycles, config.OscillationMinCycles, PatternType);

                events.Add(new RageClickEvent
                {
                    ScreenName = windowNav.First().ScreenName,
                    UiElementId = windowNav.First().UiElementId,
                    Pattern = PatternType,
                    RageIntensity = intensity,
                    Confidence = confidence,
                    RootCause = RootCauseAnalyzer.Analyze(PatternType, windowNav),
                    TriggeringInteractions = windowNav,
                    DetectedAt = DateTimeOffset.UtcNow
                });

                // Skip past the detected oscillation
                i += windowNav.Count - 1;
            }
        }

        return events;
    }

    private static int CountOscillationCycles(IReadOnlyList<InteractionRecord> navSequence)
    {
        int cycles = 0;

        for (int i = 0; i < navSequence.Count - 1; i++)
        {
            var current = navSequence[i].Direction;
            var next = navSequence[i + 1].Direction;

            bool isOscillation =
                (current == NavigationDirection.Back && next == NavigationDirection.Forward) ||
                (current == NavigationDirection.Forward && next == NavigationDirection.Back) ||
                (current == NavigationDirection.Open && next == NavigationDirection.Close) ||
                (current == NavigationDirection.Close && next == NavigationDirection.Open);

            if (isOscillation)
            {
                cycles++;
                i++; // Skip the paired interaction
            }
        }

        return cycles;
    }
}
