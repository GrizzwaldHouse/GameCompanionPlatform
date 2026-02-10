namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Detection;
using GameCompanion.Engine.RageClickDetector.Models;

public class FormSubmissionFailureDetectorTests
{
    private readonly FormSubmissionFailureDetector _detector = new();
    private readonly DetectorConfiguration _config = new();

    [Fact]
    public void Detect_TwoFailedSubmitsNoGuidance_DetectsLoop()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateSubmit(baseTime, 0, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 1000, validationError: true, guidanceShown: false)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().HaveCount(1);
        events[0].Pattern.Should().Be(RageClickPattern.FormSubmissionFailureLoop);
        events[0].RootCause.Should().Be(LikelyRootCause.ValidationOpacity);
    }

    [Fact]
    public void Detect_FailedSubmitWithGuidance_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateSubmit(baseTime, 0, validationError: true, guidanceShown: true),
            CreateSubmit(baseTime, 1000, validationError: true, guidanceShown: true)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_SingleFailedSubmit_DoesNotTrigger()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateSubmit(baseTime, 0, validationError: true, guidanceShown: false)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Detect_ThreeFailedSubmits_HigherIntensityThanTwo()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var twoFails = new List<InteractionRecord>
        {
            CreateSubmit(baseTime, 0, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 1000, validationError: true, guidanceShown: false)
        };
        var threeFails = new List<InteractionRecord>
        {
            CreateSubmit(baseTime, 0, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 1000, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 2000, validationError: true, guidanceShown: false)
        };

        var twoResult = _detector.Detect(twoFails, _config);
        var threeResult = _detector.Detect(threeFails, _config);

        threeResult[0].RageIntensity.Should().BeGreaterThan(twoResult[0].RageIntensity);
    }

    [Fact]
    public void Detect_SuccessfulSubmitBreaksRun_SeparateEvents()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var interactions = new List<InteractionRecord>
        {
            CreateSubmit(baseTime, 0, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 1000, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 2000, validationError: false, guidanceShown: false), // Success
            CreateSubmit(baseTime, 3000, validationError: true, guidanceShown: false),
            CreateSubmit(baseTime, 4000, validationError: true, guidanceShown: false)
        };

        var events = _detector.Detect(interactions, _config);

        events.Should().HaveCount(2);
    }

    private static InteractionRecord CreateSubmit(
        DateTimeOffset baseTime, int offsetMs,
        bool validationError, bool guidanceShown)
    {
        return new InteractionRecord
        {
            AnonymizedSessionId = "session1",
            UiElementId = "btn_submit_report",
            InteractionType = InteractionType.Submit,
            Timestamp = baseTime.AddMilliseconds(offsetMs),
            ScreenName = "ReportForm",
            ResultedInValidationError = validationError,
            NewGuidanceShown = guidanceShown
        };
    }
}
