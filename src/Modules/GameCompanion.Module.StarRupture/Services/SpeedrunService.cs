namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Manages speedrun timing and records.
/// </summary>
public sealed class SpeedrunService
{
    private readonly string _speedrunDir;
    private SpeedrunSession? _currentSession;
    private readonly List<LeaderboardEntry> _personalBests = [];

    private static readonly List<SpeedrunCategory> Categories =
    [
        new SpeedrunCategory
        {
            Id = "any_percent",
            Name = "Any%",
            Description = "Complete the game as fast as possible",
            SplitNames = ["First Smelter", "First Constructor", "Wave 1 Survived", "Hub Tier 2", "Wave 5 Survived", "Victory"]
        },
        new SpeedrunCategory
        {
            Id = "all_research",
            Name = "All Research",
            Description = "Unlock all research before completing",
            SplitNames = ["First Research", "25% Research", "50% Research", "75% Research", "100% Research", "Victory"]
        },
        new SpeedrunCategory
        {
            Id = "wave_10",
            Name = "Wave 10",
            Description = "Survive to Wave 10 as fast as possible",
            SplitNames = ["Wave 1", "Wave 3", "Wave 5", "Wave 7", "Wave 10"]
        }
    ];

    public SpeedrunService()
    {
        _speedrunDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArcadiaTracker", "speedruns");
        Directory.CreateDirectory(_speedrunDir);
    }

    /// <summary>
    /// Gets available speedrun categories.
    /// </summary>
    public Result<IReadOnlyList<SpeedrunCategory>> GetCategories()
    {
        return Result<IReadOnlyList<SpeedrunCategory>>.Success(Categories);
    }

    /// <summary>
    /// Starts a new speedrun session.
    /// </summary>
    public Result<SpeedrunSession> StartSession(string categoryId)
    {
        try
        {
            var category = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
                return Result<SpeedrunSession>.Failure("Invalid category");

            var splits = category.SplitNames.Select((name, i) => new SpeedrunSplit
            {
                Name = name,
                Order = i + 1,
                SplitTime = null,
                PersonalBest = GetPersonalBestSplit(categoryId, name),
                GoldSplit = GetGoldSplit(categoryId, name)
            }).ToList();

            var pb = GetPersonalBest(categoryId);

            _currentSession = new SpeedrunSession
            {
                Id = Guid.NewGuid().ToString(),
                Category = categoryId,
                StartTime = DateTime.Now,
                EndTime = null,
                Splits = splits,
                Comparison = pb != null ? new SpeedrunComparison
                {
                    PersonalBest = pb.Value,
                    SumOfBest = CalculateSumOfBest(categoryId),
                    WorldRecord = null, // Would need online integration
                    PersonalBestDate = DateTime.Now // Simplified
                } : null
            };

            return Result<SpeedrunSession>.Success(_currentSession);
        }
        catch (Exception ex)
        {
            return Result<SpeedrunSession>.Failure($"Failed to start session: {ex.Message}");
        }
    }

    /// <summary>
    /// Marks a split as completed.
    /// </summary>
    public Result<SpeedrunSession> CompleteSplit(int splitIndex)
    {
        if (_currentSession == null)
            return Result<SpeedrunSession>.Failure("No active session");

        if (splitIndex < 0 || splitIndex >= _currentSession.Splits.Count)
            return Result<SpeedrunSession>.Failure("Invalid split index");

        var split = _currentSession.Splits[splitIndex];
        if (split.IsCompleted)
            return Result<SpeedrunSession>.Failure("Split already completed");

        // Update split time
        var updatedSplits = _currentSession.Splits.ToList();
        updatedSplits[splitIndex] = new SpeedrunSplit
        {
            Name = split.Name,
            Order = split.Order,
            SplitTime = _currentSession.CurrentTime,
            PersonalBest = split.PersonalBest,
            GoldSplit = split.GoldSplit
        };

        _currentSession = new SpeedrunSession
        {
            Id = _currentSession.Id,
            Category = _currentSession.Category,
            StartTime = _currentSession.StartTime,
            EndTime = _currentSession.EndTime,
            Splits = updatedSplits,
            Comparison = _currentSession.Comparison
        };

        return Result<SpeedrunSession>.Success(_currentSession);
    }

    /// <summary>
    /// Ends the current speedrun session.
    /// </summary>
    public Result<SpeedrunSession> EndSession()
    {
        if (_currentSession == null)
            return Result<SpeedrunSession>.Failure("No active session");

        _currentSession = new SpeedrunSession
        {
            Id = _currentSession.Id,
            Category = _currentSession.Category,
            StartTime = _currentSession.StartTime,
            EndTime = DateTime.Now,
            Splits = _currentSession.Splits,
            Comparison = _currentSession.Comparison
        };

        // Check for personal best
        var pb = GetPersonalBest(_currentSession.Category);
        if (!pb.HasValue || _currentSession.CurrentTime < pb.Value)
        {
            SavePersonalBest(_currentSession);
        }

        var result = _currentSession;
        _currentSession = null;
        return Result<SpeedrunSession>.Success(result);
    }

    /// <summary>
    /// Gets the current session if active.
    /// </summary>
    public SpeedrunSession? GetCurrentSession() => _currentSession;

    private TimeSpan? GetPersonalBest(string categoryId)
    {
        var entry = _personalBests.FirstOrDefault(e => e.Category == categoryId);
        return entry?.Time;
    }

    private TimeSpan? GetPersonalBestSplit(string categoryId, string splitName)
    {
        // Would load from persistent storage
        return null;
    }

    private TimeSpan? GetGoldSplit(string categoryId, string splitName)
    {
        // Would load best individual split times
        return null;
    }

    private TimeSpan CalculateSumOfBest(string categoryId)
    {
        // Would sum up gold splits
        return TimeSpan.Zero;
    }

    private void SavePersonalBest(SpeedrunSession session)
    {
        _personalBests.RemoveAll(e => e.Category == session.Category && e.IsPersonalBest);
        _personalBests.Add(new LeaderboardEntry
        {
            Rank = 1,
            PlayerName = "You",
            Time = session.CurrentTime,
            Date = DateTime.Now,
            Category = session.Category,
            IsPersonalBest = true
        });
    }
}
