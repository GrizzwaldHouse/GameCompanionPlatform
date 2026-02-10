namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Scoring;

public class UxRiskScorerTests
{
    [Fact]
    public void CalculateRapidClickIntensity_AtThreshold_ReturnsBaseScore()
    {
        var intensity = UxRiskScorer.CalculateRapidClickIntensity(
            clickCount: 3, minRequired: 3,
            maxWindow: TimeSpan.FromSeconds(2),
            actualDuration: TimeSpan.FromSeconds(2));

        intensity.Should().BeInRange(30, 50);
    }

    [Fact]
    public void CalculateRapidClickIntensity_HighExcess_ReturnsHighScore()
    {
        var intensity = UxRiskScorer.CalculateRapidClickIntensity(
            clickCount: 10, minRequired: 3,
            maxWindow: TimeSpan.FromSeconds(2),
            actualDuration: TimeSpan.FromMilliseconds(500));

        intensity.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public void CalculateRapidClickIntensity_AlwaysClampsTo100()
    {
        var intensity = UxRiskScorer.CalculateRapidClickIntensity(
            clickCount: 100, minRequired: 3,
            maxWindow: TimeSpan.FromSeconds(2),
            actualDuration: TimeSpan.FromMilliseconds(100));

        intensity.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void CalculateConfidence_AtThreshold_ReturnsBaseConfidence()
    {
        var confidence = UxRiskScorer.CalculateConfidence(
            observedCount: 3, minRequired: 3,
            pattern: RageClickPattern.RapidRepeatClick);

        confidence.Should().BeApproximately(0.5, 0.1);
    }

    [Fact]
    public void CalculateConfidence_HighExcess_ReturnsHighConfidence()
    {
        var confidence = UxRiskScorer.CalculateConfidence(
            observedCount: 10, minRequired: 3,
            pattern: RageClickPattern.RapidRepeatClick);

        confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void CalculateConfidence_NeverExceeds1()
    {
        var confidence = UxRiskScorer.CalculateConfidence(
            observedCount: 100, minRequired: 3,
            pattern: RageClickPattern.RapidRepeatClick);

        confidence.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void CalculateFormFailureIntensity_StartHigherThanOtherPatterns()
    {
        var formIntensity = UxRiskScorer.CalculateFormFailureIntensity(
            failedAttempts: 2, minRequired: 2);
        var deadEndIntensity = UxRiskScorer.CalculateDeadEndIntensity(
            clickCount: 2, minRequired: 2);

        formIntensity.Should().BeGreaterThan(deadEndIntensity);
    }

    [Fact]
    public void CalculateOscillationIntensity_AtThreshold_ReturnsMidRange()
    {
        var intensity = UxRiskScorer.CalculateOscillationIntensity(
            cycles: 2, minRequired: 2);

        intensity.Should().BeInRange(30, 60);
    }
}
