namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for displaying personal best records.
/// </summary>
public sealed partial class RecordsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<RecordDisplayItem> _speedRecords = [];

    [ObservableProperty]
    private ObservableCollection<RecordDisplayItem> _achievementRecords = [];

    [ObservableProperty]
    private ObservableCollection<RecordDisplayItem> _enduranceRecords = [];

    [ObservableProperty]
    private string _statusMessage = "Loading records...";

    [ObservableProperty]
    private ObservableCollection<string> _recentlyBroken = [];

    [ObservableProperty]
    private bool _hasRecords;

    public void UpdateRecords(PersonalRecords records)
    {
        // Speed records
        var speed = new List<RecordDisplayItem>();
        if (records.FastestToMidGame.HasValue)
        {
            var r = records.FastestToMidGame.Value;
            speed.Add(new RecordDisplayItem
            {
                Category = "Fastest to Mid-Game",
                Value = FormatTime(r.Time),
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        if (records.FastestToEndGame.HasValue)
        {
            var r = records.FastestToEndGame.Value;
            speed.Add(new RecordDisplayItem
            {
                Category = "Fastest to End-Game",
                Value = FormatTime(r.Time),
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        if (records.FastestToMastery.HasValue)
        {
            var r = records.FastestToMastery.Value;
            speed.Add(new RecordDisplayItem
            {
                Category = "Fastest to Mastery",
                Value = FormatTime(r.Time),
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        SpeedRecords = new ObservableCollection<RecordDisplayItem>(speed);

        // Achievement records
        var achievements = new List<RecordDisplayItem>();
        if (records.HighestDataPoints.HasValue)
        {
            var r = records.HighestDataPoints.Value;
            achievements.Add(new RecordDisplayItem
            {
                Category = "Highest Data Points",
                Value = r.Value.ToString("N0"),
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        if (records.HighestWaveSurvived.HasValue)
        {
            var r = records.HighestWaveSurvived.Value;
            achievements.Add(new RecordDisplayItem
            {
                Category = "Highest Wave Survived",
                Value = $"Wave {r.Value}",
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        if (records.MostBasesBuilt.HasValue)
        {
            var r = records.MostBasesBuilt.Value;
            achievements.Add(new RecordDisplayItem
            {
                Category = "Most Bases Built",
                Value = r.Value.ToString(),
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        AchievementRecords = new ObservableCollection<RecordDisplayItem>(achievements);

        // Endurance records
        var endurance = new List<RecordDisplayItem>();
        if (records.LongestSession.HasValue)
        {
            var r = records.LongestSession.Value;
            endurance.Add(new RecordDisplayItem
            {
                Category = "Longest Session",
                Value = FormatTime(r.Time),
                Session = r.SessionName,
                AchievedAt = r.AchievedAt
            });
        }
        EnduranceRecords = new ObservableCollection<RecordDisplayItem>(endurance);

        HasRecords = speed.Count > 0 || achievements.Count > 0 || endurance.Count > 0;
        StatusMessage = HasRecords ? "Personal Records" : "No records yet - keep playing!";
    }

    public void ShowBrokenRecords(IReadOnlyList<string> broken)
    {
        RecentlyBroken = new ObservableCollection<string>(broken);
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}h {time.Minutes}m"
            : $"{time.Minutes}m {time.Seconds}s";
    }
}

/// <summary>
/// Display item for a record entry.
/// </summary>
public sealed record RecordDisplayItem
{
    public required string Category { get; init; }
    public required string Value { get; init; }
    public required string Session { get; init; }
    public required DateTime AchievedAt { get; init; }

    public string AchievedAtDisplay => AchievedAt.ToLocalTime().ToString("MMM d, yyyy");
}
