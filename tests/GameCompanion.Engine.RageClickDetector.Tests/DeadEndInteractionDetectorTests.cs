namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Detection;
using GameCompanion.Engine.RageClickDetector.Models;

public class DeadEndInteractionDetectorTests
{
    private readonly DeadEndInteractionDetector _detector = new();
    private readonly DetectorConfiguration _config = new();

    [Fact]
    public void Detect_TwoDisabledClicksWithinWindow_DetectsDeadEnd()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateDisabledClick(baseTime, 0),
            CreateDisabledClick(baseTime, 1000)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().HaveCount(1);
        events[0].Pattern.Should().Be(RageClickPattern.DeadEndInteraction);
        events[0].RootCause.Should().Be(LikelyRootCause.DisabledStateAmbiguity);
    }

    [Fact]
    public void Detect_SingleDisabledClick_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateDisabledClick(baseTime, 0)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_DisabledClicksExceedingWindow_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateDisabledClick(baseTime, 0),
            CreateDisabledClick(baseTime, 4000) // 4 seconds > 3 second window
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_EnabledClicks_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            new()
            {
                AnonymizedSessionId = "session1",
                UiElementId = "btn_export",
                InteractionType = InteractionType.Click,
                Timestamp = baseTime,
                ScreenName = "Export",
                TargetWasDisabled = false
            },
            new()
            {
                AnonymizedSessionId = "session1",
                UiElementId = "btn_export",
                InteractionType = InteractionType.Click,
                Timestamp = baseTime.AddSeconds(1),
                ScreenName = "Export",
                TargetWasDisabled = false
            }
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_FourRapidDisabledClicks_HigherIntensity()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var twoClicks = new List<InteractionRecord>
        {
            CreateDisabledClick(baseTime, 0),
            CreateDisabledClick(baseTime, 500)
        };
        var fourClicks = new List<InteractionRecord>
        {
            CreateDisabledClick(baseTime, 0),
            CreateDisabledClick(baseTime, 500),
            CreateDisabledClick(baseTime, 1000),
            CreateDisabledClick(baseTime, 1500)
        };

        var twoResult = _detector.Detect(twoClicks, _config);
        var fourResult = _detector.Detect(fourClicks, _config);

        fourResult[0].RageIntensity.Should().BeGreaterThan(twoResult[0].RageIntensity);
    }

    private static InteractionRecord CreateDisabledClick(DateTimeOffset baseTime, int offsetMs)
    {
        return new InteractionRecord
        {
            AnonymizedSessionId = "session1",
            UiElementId = "btn_export",
            InteractionType = InteractionType.Click,
            Timestamp = baseTime.AddMilliseconds(offsetMs),
            ScreenName = "Export",
            TargetWasDisabled = true
        };
    }
}
