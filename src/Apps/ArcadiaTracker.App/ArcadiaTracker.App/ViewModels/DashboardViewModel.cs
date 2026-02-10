namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Dashboard view showing progress overview and badges.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private PlayerProgress? _progress;

    [ObservableProperty]
    private string _playtimeDisplay = "0h 0m";

    [ObservableProperty]
    private string _phaseDisplay = "Unknown";

    [ObservableProperty]
    private double _overallProgressPercent;

    [ObservableProperty]
    private string _blueprintProgressText = "0 / 0";

    [ObservableProperty]
    private double _blueprintProgressPercent;

    [ObservableProperty]
    private string _dataPointsDisplay = "0";

    [ObservableProperty]
    private string _corporationDisplay = "None";

    [ObservableProperty]
    private bool _mapUnlocked;

    [ObservableProperty]
    private string _waveDisplay = "N/A";

    [ObservableProperty]
    private ObservableCollection<CorporationDisplayInfo> _corporations = [];

    [ObservableProperty]
    private ObservableCollection<Badge> _earnedBadges = [];

    [ObservableProperty]
    private int _earnedBadgeCount;

    [ObservableProperty]
    private int _totalBadgeCount;

    public void UpdateProgress(PlayerProgress progress)
    {
        Progress = progress;

        // Playtime
        var hours = (int)progress.TotalPlayTime.TotalHours;
        var minutes = progress.TotalPlayTime.Minutes;
        PlaytimeDisplay = $"{hours}h {minutes}m";

        // Phase
        PhaseDisplay = progress.CurrentPhase switch
        {
            ProgressionPhase.EarlyGame => "Early Game",
            ProgressionPhase.MidGame => "Mid Game",
            ProgressionPhase.EndGame => "End Game",
            ProgressionPhase.Mastery => "Mastery",
            _ => "Unknown"
        };

        // Overall progress
        OverallProgressPercent = progress.OverallProgress * 100;

        // Blueprints
        BlueprintProgressText = $"{progress.BlueprintsUnlocked} / {progress.BlueprintsTotal}";
        BlueprintProgressPercent = progress.BlueprintProgress * 100;

        // Data points
        DataPointsDisplay = progress.DataPointsEarned.ToString("N0");

        // Corporation
        CorporationDisplay = $"{progress.HighestCorporationName} (Lvl {progress.HighestCorporationLevel})";

        // Map
        MapUnlocked = progress.MapUnlocked;

        // Per-corporation breakdown
        Corporations = new ObservableCollection<CorporationDisplayInfo>(
            progress.Corporations
                .OrderByDescending(c => c.CurrentLevel)
                .ThenByDescending(c => c.CurrentXP)
                .Select(c => new CorporationDisplayInfo
                {
                    Name = c.DisplayName,
                    Level = c.CurrentLevel,
                    XP = c.CurrentXP
                }));

        // Wave
        WaveDisplay = string.IsNullOrEmpty(progress.CurrentWave)
            ? "N/A"
            : $"{progress.CurrentWave} - {progress.CurrentWaveStage}";

        // Badges
        EarnedBadges = new ObservableCollection<Badge>(progress.EarnedBadges);
        EarnedBadgeCount = progress.EarnedBadges.Count;
        TotalBadgeCount = GameCompanion.Module.StarRupture.Progression.Badges.AllBadges.Count;
    }
}

/// <summary>
/// Display-friendly corporation info for the dashboard.
/// </summary>
public sealed class CorporationDisplayInfo
{
    public required string Name { get; init; }
    public int Level { get; init; }
    public int XP { get; init; }
    public string LevelDisplay => $"Lvl {Level}";
    public string XPDisplay => XP.ToString("N0") + " XP";
}
