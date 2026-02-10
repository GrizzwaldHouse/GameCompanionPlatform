namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Detection;
using GameCompanion.Engine.RageClickDetector.Models;

public class RapidRepeatClickDetectorTests
{
    private readonly RapidRepeatClickDetector _detector = new();
    private readonly DetectorConfiguration _config = new();

    [Fact]
    public void Detect_ThreeClicksSameElementWithinWindow_DetectsRageClick()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = CreateRapidClicks("btn_save", "Settings", baseTime, count: 3,
            intervalMs: 500);

        var events = _detector.Detect(interactions, _config);

        events.Should().HaveCount(1);
        events[0].Pattern.Should().Be(RageClickPattern.RapidRepeatClick);
        events[0].ScreenName.Should().Be("Settings");
        events[0].RageIntensity.Should().BeInRange(30, 100);
        events[0].Confidence.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Detect_TwoClicksOnly_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = CreateRapidClicks("btn_save", "Settings", baseTime, count: 2,
            intervalMs: 500);

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ThreeClicksExceedingWindow_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = CreateRapidClicks("btn_save", "Settings", baseTime, count: 3,
            intervalMs: 1500); // 3 seconds total > 2 second window

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ClicksWithStateChange_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = Enumerable.Range(0, 3).Select(i => new InteractionRecord
        {
            AnonymizedSessionId = "session1",
            UiElementId = "btn_save",
            InteractionType = InteractionType.Click,
            Timestamp = baseTime.AddMilliseconds(i * 300),
            ScreenName = "Settings",
            CausedStateChange = true // State change occurs
        }).ToList();

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_FiveRapidClicks_HigherIntensityThanThree()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var threeClicks = CreateRapidClicks("btn_save", "Settings", baseTime, count: 3,
            intervalMs: 500);
        var fiveClicks = CreateRapidClicks("btn_save", "Settings", baseTime, count: 5,
            intervalMs: 300);

        var threeResult = _detector.Detect(threeClicks, _config);
        var fiveResult = _detector.Detect(fiveClicks, _config);

        fiveResult.Should().HaveCount(1);
        threeResult.Should().HaveCount(1);
        fiveResult[0].RageIntensity.Should().BeGreaterThanOrEqualTo(threeResult[0].RageIntensity);
    }

    [Fact]
    public void Detect_DifferentElements_DetectsSeparateEvents()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var clicks1 = CreateRapidClicks("btn_save", "Settings", baseTime, count: 3,
            intervalMs: 300);
        var clicks2 = CreateRapidClicks("btn_cancel", "Settings", baseTime, count: 3,
            intervalMs: 300);

        var allInteractions = clicks1.Concat(clicks2).OrderBy(i => i.Timestamp).ToList();
        var events = _detector.Detect(allInteractions, _config);

        events.Should().HaveCount(2);
    }

    private static List<InteractionRecord> CreateRapidClicks(
        string elementId, string screenName, DateTimeOffset baseTime,
        int count, int intervalMs)
    {
        return Enumerable.Range(0, count).Select(i => new InteractionRecord
        {
            AnonymizedSessionId = "session1",
            UiElementId = elementId,
            InteractionType = InteractionType.Click,
            Timestamp = baseTime.AddMilliseconds(i * intervalMs),
            ScreenName = screenName,
            CausedStateChange = false
        }).ToList();
    }
}
