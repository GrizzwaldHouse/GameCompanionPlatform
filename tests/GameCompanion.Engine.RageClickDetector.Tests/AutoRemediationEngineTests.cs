namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Remediation;

public class AutoRemediationEngineTests
{
    private readonly AutoRemediationEngine _engine = new();

    [Fact]
    public void GenerateRemediations_MissingFeedback_SuggestsInlineFeedback()
    {
        var events = new List<RageClickEvent>
        {
            CreateEvent(LikelyRootCause.MissingFeedback)
        };

        var remediations = _engine.GenerateRemediations(events);

        remediations.Should().ContainSingle(r => r.Type == RemediationType.AddInlineFeedback);
    }

    [Fact]
    public void GenerateRemediations_UnclearCopy_SuggestsCopyImprovement()
    {
        var events = new List<RageClickEvent>
        {
            CreateEvent(LikelyRootCause.UnclearCopy)
        };

        var remediations = _engine.GenerateRemediations(events);

        remediations.Should().ContainSingle(r => r.Type == RemediationType.ImproveCopyOrLabeling);
    }

    [Fact]
    public void GenerateRemediations_DisabledStateAmbiguity_SuggestsAffordanceAndGuidance()
    {
        var events = new List<RageClickEvent>
        {
            CreateEvent(LikelyRootCause.DisabledStateAmbiguity)
        };

        var remediations = _engine.GenerateRemediations(events);

        remediations.Should().HaveCount(2);
        remediations.Should().Contain(r => r.Type == RemediationType.AddVisualAffordance);
        remediations.Should().Contain(r => r.Type == RemediationType.IntroduceMicroGuidance);
    }

    [Fact]
    public void GenerateRemediations_ValidationOpacity_SuggestsFeedbackAndGuidance()
    {
        var events = new List<RageClickEvent>
        {
            CreateEvent(LikelyRootCause.ValidationOpacity)
        };

        var remediations = _engine.GenerateRemediations(events);

        remediations.Should().HaveCount(2);
        remediations.Should().Contain(r => r.Type == RemediationType.AddInlineFeedback);
        remediations.Should().Contain(r => r.Type == RemediationType.IntroduceMicroGuidance);
    }

    [Fact]
    public void GenerateRemediations_NavigationAmbiguity_SuggestsCopyAndFeedback()
    {
        var events = new List<RageClickEvent>
        {
            CreateEvent(LikelyRootCause.NavigationAmbiguity)
        };

        var remediations = _engine.GenerateRemediations(events);

        remediations.Should().HaveCount(2);
        remediations.Should().Contain(r => r.Type == RemediationType.ImproveCopyOrLabeling);
        remediations.Should().Contain(r => r.Type == RemediationType.AddInlineFeedback);
    }

    [Fact]
    public void GenerateRemediations_EmptyEvents_ReturnsEmpty()
    {
        var remediations = _engine.GenerateRemediations([]);

        remediations.Should().BeEmpty();
    }

    [Fact]
    public void GenerateRemediations_MultipleEvents_ReturnsRemediationsForAll()
    {
        var events = new List<RageClickEvent>
        {
            CreateEvent(LikelyRootCause.MissingFeedback, screen: "Settings"),
            CreateEvent(LikelyRootCause.ValidationOpacity, screen: "Report")
        };

        var remediations = _engine.GenerateRemediations(events);

        remediations.Should().HaveCountGreaterThan(2);
        remediations.Should().Contain(r => r.ScreenName == "Settings");
        remediations.Should().Contain(r => r.ScreenName == "Report");
    }

    private static RageClickEvent CreateEvent(
        LikelyRootCause rootCause, string screen = "TestScreen")
    {
        return new RageClickEvent
        {
            ScreenName = screen,
            UiElementId = "test_element",
            Pattern = RageClickPattern.RapidRepeatClick,
            RageIntensity = 50,
            Confidence = 0.7,
            RootCause = rootCause,
            TriggeringInteractions = [],
            DetectedAt = DateTimeOffset.UtcNow
        };
    }
}
