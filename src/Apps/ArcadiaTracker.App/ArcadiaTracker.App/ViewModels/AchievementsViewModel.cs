namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Achievements view showing Steam achievement status.
/// </summary>
public sealed partial class AchievementsViewModel : ObservableObject
{
    [ObservableProperty]
    private AchievementSummary? _summary;

    [ObservableProperty]
    private ObservableCollection<SteamAchievementStatus> _achievements = [];

    [ObservableProperty]
    private int _earnedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private double _completionPercent;

    [ObservableProperty]
    private bool _steamApiAvailable;

    [ObservableProperty]
    private int _mismatchCount;

    [ObservableProperty]
    private string _filterMode = "All"; // All, Earned, Locked, Mismatched

    public void UpdateAchievements(AchievementSummary summary)
    {
        Summary = summary;
        EarnedCount = summary.EarnedLocally;
        TotalCount = summary.TotalAchievements;
        CompletionPercent = summary.CompletionPercentage * 100;
        SteamApiAvailable = summary.SteamApiAvailable;
        MismatchCount = summary.Mismatches;

        ApplyFilter();
    }

    public void ApplyFilter()
    {
        if (Summary == null) return;

        var filtered = FilterMode switch
        {
            "Earned" => Summary.Achievements.Where(a => a.IsEarnedLocally),
            "Locked" => Summary.Achievements.Where(a => !a.IsEarnedLocally),
            "Mismatched" => Summary.Achievements.Where(a => a.HasMismatch),
            _ => Summary.Achievements.AsEnumerable()
        };

        Achievements = new ObservableCollection<SteamAchievementStatus>(filtered);
    }
}
