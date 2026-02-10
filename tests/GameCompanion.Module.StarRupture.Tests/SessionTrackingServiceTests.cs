using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class SessionTrackingServiceTests
{
    [Fact]
    public async Task GetHistory_WithNoFile_ShouldReturnEmptyHistory()
    {
        using var service = new SessionTrackingService();
        var sessionName = $"TestSession_{Guid.NewGuid()}";

        var result = await service.GetHistoryAsync(sessionName);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SessionName.Should().Be(sessionName);
        result.Value.Snapshots.Should().BeEmpty();
        result.Value.TotalTrackedTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task RecordSnapshot_ShouldPersistAndRetrieve()
    {
        using var service = new SessionTrackingService();
        var sessionName = $"TestSession_{Guid.NewGuid()}";

        var progress = new PlayerProgress
        {
            SessionName = sessionName,
            TotalPlayTime = TimeSpan.FromHours(5),
            CurrentPhase = ProgressionPhase.MidGame,
            OverallProgress = 0.5,
            BlueprintsUnlocked = 90,
            BlueprintsTotal = 180,
            DataPointsEarned = 5000,
            HighestCorporationLevel = 3,
            HighestCorporationName = "Moon Energy",
            MapUnlocked = true,
            Corporations = [],
            UniqueItemsDiscovered = 50,
            CurrentWave = "Wave 2",
            CurrentWaveStage = "Stage 1",
            EarnedBadges = []
        };

        var recordResult = await service.RecordSnapshotAsync(sessionName, progress);
        recordResult.IsSuccess.Should().BeTrue();

        var historyResult = await service.GetHistoryAsync(sessionName);

        historyResult.IsSuccess.Should().BeTrue();
        historyResult.Value!.Snapshots.Should().HaveCount(1);
        historyResult.Value.Snapshots[0].Phase.Should().Be(ProgressionPhase.MidGame);
        historyResult.Value.Snapshots[0].BlueprintsUnlocked.Should().Be(90);
        historyResult.Value.Snapshots[0].DataPoints.Should().Be(5000);
    }

    [Fact]
    public async Task RecordSnapshot_MultipleTimes_ShouldAccumulate()
    {
        using var service = new SessionTrackingService();
        var sessionName = $"TestSession_{Guid.NewGuid()}";

        var progress1 = CreateProgress(sessionName, TimeSpan.FromHours(2), ProgressionPhase.EarlyGame, 20);
        var progress2 = CreateProgress(sessionName, TimeSpan.FromHours(5), ProgressionPhase.MidGame, 50);
        var progress3 = CreateProgress(sessionName, TimeSpan.FromHours(10), ProgressionPhase.EndGame, 100);

        await service.RecordSnapshotAsync(sessionName, progress1);
        await service.RecordSnapshotAsync(sessionName, progress2);
        await service.RecordSnapshotAsync(sessionName, progress3);

        var historyResult = await service.GetHistoryAsync(sessionName);

        historyResult.IsSuccess.Should().BeTrue();
        historyResult.Value!.Snapshots.Should().HaveCount(3);
        historyResult.Value.Snapshots[0].Phase.Should().Be(ProgressionPhase.EarlyGame);
        historyResult.Value.Snapshots[1].Phase.Should().Be(ProgressionPhase.MidGame);
        historyResult.Value.Snapshots[2].Phase.Should().Be(ProgressionPhase.EndGame);
        historyResult.Value.TotalTrackedTime.Should().Be(TimeSpan.FromHours(8));
    }

    // --- Helpers ---

    private static PlayerProgress CreateProgress(
        string sessionName,
        TimeSpan playTime,
        ProgressionPhase phase,
        int blueprints)
    {
        return new PlayerProgress
        {
            SessionName = sessionName,
            TotalPlayTime = playTime,
            CurrentPhase = phase,
            OverallProgress = 0.5,
            BlueprintsUnlocked = blueprints,
            BlueprintsTotal = 180,
            DataPointsEarned = 5000,
            HighestCorporationLevel = 3,
            HighestCorporationName = "Test Corp",
            MapUnlocked = true,
            Corporations = [],
            UniqueItemsDiscovered = 50,
            CurrentWave = "Wave 1",
            CurrentWaveStage = "Stage 1",
            EarnedBadges = []
        };
    }
}
