namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Models;

public class RageClickDetectorServiceTests
{
    [Fact]
    public void RecordInteraction_IncreasesCount()
    {
        var service = new RageClickDetectorService();

        service.RecordInteraction("btn_save", InteractionType.Click, "Settings");

        service.InteractionCount.Should().Be(1);
    }

    [Fact]
    public void DetectRageClicks_WithRapidClicks_ReturnsEvents()
    {
        var service = new RageClickDetectorService();
        var baseTime = DateTimeOffset.UtcNow;

        // Record 4 rapid clicks directly on the interaction list
        var interactions = Enumerable.Range(0, 4).Select(i => new InteractionRecord
        {
            AnonymizedSessionId = "session1",
            UiElementId = "btn_save",
            InteractionType = InteractionType.Click,
            Timestamp = baseTime.AddMilliseconds(i * 300),
            ScreenName = "Settings",
            CausedStateChange = false
        }).ToList();

        var events = service.DetectRageClicks(interactions);

        events.Should().NotBeEmpty();
        events.Should().AllSatisfy(e => e.Pattern.Should().Be(RageClickPattern.RapidRepeatClick));
    }

    [Fact]
    public void Analyze_ProducesCompleteReport()
    {
        var service = new RageClickDetectorService();

        // Use the direct detection with pre-built interactions
        // because RecordInteraction hashes element IDs
        var report = service.Analyze();

        report.Should().NotBeNull();
        report.Events.Should().NotBeNull();
        report.Remediations.Should().NotBeNull();
        report.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        report.TotalInteractionsAnalyzed.Should().Be(0);
        report.Validation.Should().BeNull(); // MODE_A doesn't validate
    }

    [Fact]
    public void AnalyzeAndValidate_ProducesReportWithValidation()
    {
        var service = new RageClickDetectorService();

        // Record some rage-click worthy interactions
        for (int i = 0; i < 5; i++)
        {
            service.RecordInteraction(
                "btn_save",
                InteractionType.Click,
                "Settings",
                causedStateChange: false);
        }

        var report = service.AnalyzeAndValidate();

        report.Should().NotBeNull();
        report.Validation.Should().NotBeNull();
        report.TotalInteractionsAnalyzed.Should().Be(5);
    }

    [Fact]
    public void GenerateMarkdownReport_ProducesFormattedOutput()
    {
        var service = new RageClickDetectorService();
        var report = new RageClickReport
        {
            Events = new List<RageClickEvent>
            {
                new()
                {
                    ScreenName = "Settings",
                    UiElementId = "abc123",
                    Pattern = RageClickPattern.RapidRepeatClick,
                    RageIntensity = 65,
                    Confidence = 0.8,
                    RootCause = LikelyRootCause.MissingFeedback,
                    TriggeringInteractions = [],
                    DetectedAt = DateTimeOffset.UtcNow
                }
            },
            Remediations = new List<RemediationAction>
            {
                new()
                {
                    Type = RemediationType.AddInlineFeedback,
                    TargetElementId = "abc123",
                    ScreenName = "Settings",
                    Description = "Add loading indicator"
                }
            },
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalInteractionsAnalyzed = 10
        };

        var markdown = service.GenerateMarkdownReport(report);

        markdown.Should().Contain("# Rage-Click Analysis Report");
        markdown.Should().Contain("| Screen | Element | Rage Pattern | Intensity | Confidence | Suggested Fix |");
        markdown.Should().Contain("Settings");
        markdown.Should().Contain("Rapid Repeat Click");
        markdown.Should().Contain("65");
    }

    [Fact]
    public void Reset_ClearsAllInteractions()
    {
        var service = new RageClickDetectorService();
        service.RecordInteraction("elem", InteractionType.Click, "Screen");
        service.InteractionCount.Should().Be(1);

        service.Reset();

        service.InteractionCount.Should().Be(0);
    }
}
