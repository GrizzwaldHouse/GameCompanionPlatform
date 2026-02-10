using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class CataclysmTimerServiceTests
{
    private readonly CataclysmTimerService _service = new();

    [Fact]
    public void AnalyzeWave_WithZeroProgress_ShouldReturnSafeUrgency()
    {
        var waveData = new EnviroWaveData
        {
            Wave = "Wave 1",
            Stage = "Stage 1",
            Progress = 0.0
        };

        var result = _service.AnalyzeWave(waveData, TimeSpan.FromHours(5));

        result.CurrentWave.Should().Be("Wave 1");
        result.CurrentStage.Should().Be("Stage 1");
        result.StageProgress.Should().Be(0.0);
        result.Urgency.Should().Be(CataclysmUrgency.Safe);
        result.EstimatedTimeRemaining.TotalMinutes.Should().BeGreaterThan(15);
    }

    [Fact]
    public void AnalyzeWave_WithHighProgress_ShouldReturnCriticalUrgency()
    {
        var waveData = new EnviroWaveData
        {
            Wave = "Wave 1",
            Stage = "Stage 3",
            Progress = 0.99
        };

        var result = _service.AnalyzeWave(waveData, TimeSpan.FromHours(8));

        result.CurrentWave.Should().Be("Wave 1");
        result.CurrentStage.Should().Be("Stage 3");
        result.StageProgress.Should().Be(0.99);
        result.Urgency.Should().Be(CataclysmUrgency.Critical);
        result.EstimatedTimeRemaining.TotalMinutes.Should().BeLessThan(1);
    }

    [Fact]
    public void AnalyzeWave_WithMidProgress_ShouldReturnCaution()
    {
        var waveData = new EnviroWaveData
        {
            Wave = "Wave 3",
            Stage = "Stage 2",
            Progress = 0.7
        };

        var result = _service.AnalyzeWave(waveData, TimeSpan.FromHours(15));

        result.CurrentWave.Should().Be("Wave 3");
        result.StageProgress.Should().Be(0.7);
        result.Urgency.Should().BeOneOf(CataclysmUrgency.Caution, CataclysmUrgency.Warning);
        result.EstimatedTimeRemaining.TotalMinutes.Should().BePositive();
    }

    [Fact]
    public void AnalyzeWave_WithEmptyWave_ShouldReturnDefaultEstimate()
    {
        var waveData = new EnviroWaveData
        {
            Wave = "",
            Stage = "",
            Progress = 0.5
        };

        var result = _service.AnalyzeWave(waveData, TimeSpan.FromHours(10));

        result.CurrentWave.Should().Be("Unknown");
        result.CurrentStage.Should().Be("Unknown");
        result.StageProgress.Should().Be(0.5);
        // Should use default duration of 30 minutes
        result.EstimatedTimeRemaining.TotalMinutes.Should().BeApproximately(15, 0.1);
    }
}
