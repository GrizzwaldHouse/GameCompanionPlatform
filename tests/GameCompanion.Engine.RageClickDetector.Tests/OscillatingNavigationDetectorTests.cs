namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Detection;
using GameCompanion.Engine.RageClickDetector.Models;

public class OscillatingNavigationDetectorTests
{
    private readonly OscillatingNavigationDetector _detector = new();
    private readonly DetectorConfiguration _config = new();

    [Fact]
    public void Detect_BackForwardBackForwardBack_DetectsOscillation()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateNav(baseTime, NavigationDirection.Back, 0),
            CreateNav(baseTime, NavigationDirection.Forward, 500),
            CreateNav(baseTime, NavigationDirection.Back, 1000),
            CreateNav(baseTime, NavigationDirection.Forward, 1500),
            CreateNav(baseTime, NavigationDirection.Back, 2000)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().HaveCount(1);
        events[0].Pattern.Should().Be(RageClickPattern.OscillatingNavigation);
        events[0].RootCause.Should().Be(LikelyRootCause.NavigationAmbiguity);
    }

    [Fact]
    public void Detect_OpenCloseOpenCloseOpen_DetectsOscillation()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateNav(baseTime, NavigationDirection.Open, 0),
            CreateNav(baseTime, NavigationDirection.Close, 500),
            CreateNav(baseTime, NavigationDirection.Open, 1000),
            CreateNav(baseTime, NavigationDirection.Close, 1500),
            CreateNav(baseTime, NavigationDirection.Open, 2000)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().HaveCount(1);
    }

    [Fact]
    public void Detect_SingleBackForward_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateNav(baseTime, NavigationDirection.Back, 0),
            CreateNav(baseTime, NavigationDirection.Forward, 500)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_OscillationExceedingWindow_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateNav(baseTime, NavigationDirection.Back, 0),
            CreateNav(baseTime, NavigationDirection.Forward, 2000),
            CreateNav(baseTime, NavigationDirection.Back, 4000),
            CreateNav(baseTime, NavigationDirection.Forward, 6000), // Beyond 5s window
            CreateNav(baseTime, NavigationDirection.Back, 8000)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    private static InteractionRecord CreateNav(
        DateTimeOffset baseTime, NavigationDirection direction, int offsetMs)
    {
        return new InteractionRecord
        {
            AnonymizedSessionId = "session1",
            UiElementId = "nav_main",
            InteractionType = InteractionType.Navigation,
            Timestamp = baseTime.AddMilliseconds(offsetMs),
            ScreenName = "Dashboard",
            Direction = direction
        };
    }
}
