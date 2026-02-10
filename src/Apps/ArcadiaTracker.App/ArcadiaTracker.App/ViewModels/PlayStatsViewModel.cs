namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Play Statistics view showing aggregated play metrics.
/// </summary>
public sealed partial class PlayStatsViewModel : ObservableObject
{
    [ObservableProperty]
    private PlayStatistics? _statistics;

    [ObservableProperty]
    private string _playTimeDisplay = "0h 0m";

    [ObservableProperty]
    private string _avgSessionDisplay = "—";

    [ObservableProperty]
    private string _longestSessionDisplay = "—";

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private double _buildingEfficiency;

    [ObservableProperty]
    private double _blueprintCompletion;

    [ObservableProperty]
    private double _badgeCompletion;

    [ObservableProperty]
    private string _currentPhase = "Unknown";

    public void UpdateStatistics(PlayStatistics stats)
    {
        Statistics = stats;
        PlayTimeDisplay = stats.PlayTimeDisplay;
        AvgSessionDisplay = stats.AverageSessionLength > TimeSpan.Zero
            ? $"{(int)stats.AverageSessionLength.TotalHours}h {stats.AverageSessionLength.Minutes}m"
            : "—";
        LongestSessionDisplay = stats.LongestSession > TimeSpan.Zero
            ? $"{(int)stats.LongestSession.TotalHours}h {stats.LongestSession.Minutes}m"
            : "—";
        TotalSessions = stats.TotalSessions;
        OverallProgress = stats.OverallProgress * 100;
        BuildingEfficiency = stats.BuildingEfficiency * 100;
        BlueprintCompletion = stats.BlueprintCompletion * 100;
        BadgeCompletion = stats.BadgeCompletion * 100;
        CurrentPhase = stats.CurrentPhase;
    }
}
