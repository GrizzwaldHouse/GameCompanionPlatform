namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Progression;

/// <summary>
/// Aggregates play statistics from save data and session history.
/// </summary>
public sealed class PlayStatisticsService
{
    private readonly ProgressionAnalyzerService _analyzer;
    private readonly SessionTrackingService _sessionTracker;

    public PlayStatisticsService(
        ProgressionAnalyzerService analyzer,
        SessionTrackingService sessionTracker)
    {
        _analyzer = analyzer;
        _sessionTracker = sessionTracker;
    }

    /// <summary>
    /// Computes comprehensive play statistics from save data.
    /// </summary>
    public Result<PlayStatistics> ComputeStatistics(StarRuptureSave save)
    {
        try
        {
            var progress = _analyzer.AnalyzeSave(save);

            // Building counts from spatial data
            int totalBuildings = 0, operational = 0, disabled = 0, malfunctioning = 0;
            if (save.Spatial != null)
            {
                var buildings = save.Spatial.Entities.Where(e => e.IsBuilding).ToList();
                totalBuildings = buildings.Count;
                operational = buildings.Count(b => !b.IsDisabled && !b.HasMalfunction);
                disabled = buildings.Count(b => b.IsDisabled);
                malfunctioning = buildings.Count(b => b.HasMalfunction);
            }

            // Corporation XP total
            int totalCorpXP = save.Corporations.Corporations
                .Sum(c => c.CurrentXP);

            var stats = new PlayStatistics
            {
                TotalPlayTime = progress.TotalPlayTime,
                AverageSessionLength = TimeSpan.Zero,
                LongestSession = TimeSpan.Zero,
                TotalSessions = 0,
                OverallProgress = progress.OverallProgress,
                BlueprintsUnlocked = progress.BlueprintsUnlocked,
                BlueprintsTotal = progress.BlueprintsTotal,
                DataPointsEarned = progress.DataPointsEarned,
                UniqueItemsDiscovered = progress.UniqueItemsDiscovered,
                HighestCorporationLevel = progress.HighestCorporationLevel,
                HighestCorporationName = progress.HighestCorporationName,
                TotalCorporationXP = totalCorpXP,
                TotalBuildingsPlaced = totalBuildings,
                OperationalBuildings = operational,
                DisabledBuildings = disabled,
                MalfunctioningBuildings = malfunctioning,
                BadgesEarned = progress.EarnedBadges.Count,
                BadgesTotal = Badges.AllBadges.Count,
                CurrentPhase = progress.CurrentPhase.ToString(),
                CurrentWave = 0,
            };

            return Result<PlayStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            return Result<PlayStatistics>.Failure($"Failed to compute statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Enriches statistics with session history data if available.
    /// </summary>
    public async Task<Result<PlayStatistics>> ComputeStatisticsWithHistoryAsync(
        StarRuptureSave save,
        string sessionName,
        CancellationToken ct = default)
    {
        var baseResult = ComputeStatistics(save);
        if (baseResult.IsFailure)
            return baseResult;

        var stats = baseResult.Value!;
        var historyResult = await _sessionTracker.GetHistoryAsync(sessionName, ct);

        if (historyResult.IsSuccess && historyResult.Value!.Snapshots.Count > 1)
        {
            var snapshots = historyResult.Value.Snapshots.OrderBy(s => s.Timestamp).ToList();
            var sessionLengths = new List<TimeSpan>();

            // Estimate session lengths from snapshot gaps
            // Sessions are separated by gaps > 30 minutes
            var sessionStart = snapshots[0].Timestamp;
            for (int i = 1; i < snapshots.Count; i++)
            {
                var gap = snapshots[i].Timestamp - snapshots[i - 1].Timestamp;
                if (gap > TimeSpan.FromMinutes(30))
                {
                    sessionLengths.Add(snapshots[i - 1].Timestamp - sessionStart);
                    sessionStart = snapshots[i].Timestamp;
                }
            }
            // Add final session
            sessionLengths.Add(snapshots[^1].Timestamp - sessionStart);
            sessionLengths = sessionLengths.Where(s => s > TimeSpan.Zero).ToList();

            if (sessionLengths.Count > 0)
            {
                stats = new PlayStatistics
                {
                    TotalPlayTime = stats.TotalPlayTime,
                    AverageSessionLength = TimeSpan.FromTicks(
                        (long)sessionLengths.Average(s => s.Ticks)),
                    LongestSession = sessionLengths.Max(),
                    TotalSessions = sessionLengths.Count,
                    OverallProgress = stats.OverallProgress,
                    BlueprintsUnlocked = stats.BlueprintsUnlocked,
                    BlueprintsTotal = stats.BlueprintsTotal,
                    DataPointsEarned = stats.DataPointsEarned,
                    UniqueItemsDiscovered = stats.UniqueItemsDiscovered,
                    HighestCorporationLevel = stats.HighestCorporationLevel,
                    HighestCorporationName = stats.HighestCorporationName,
                    TotalCorporationXP = stats.TotalCorporationXP,
                    TotalBuildingsPlaced = stats.TotalBuildingsPlaced,
                    OperationalBuildings = stats.OperationalBuildings,
                    DisabledBuildings = stats.DisabledBuildings,
                    MalfunctioningBuildings = stats.MalfunctioningBuildings,
                    BadgesEarned = stats.BadgesEarned,
                    BadgesTotal = stats.BadgesTotal,
                    CurrentPhase = stats.CurrentPhase,
                    CurrentWave = stats.CurrentWave,
                };
            }
        }

        return Result<PlayStatistics>.Success(stats);
    }
}
