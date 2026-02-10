using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Dashboard view showing progress overview and badges.
/// </summary>
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    public void UpdateProgress(PlayerProgress progress)
    {
        // Playtime
        var hours = (int)progress.TotalPlayTime.TotalHours;
        var minutes = progress.TotalPlayTime.Minutes;
        PlaytimeText.Text = $"{hours}h {minutes}m";

        // Phase
        PhaseText.Text = progress.CurrentPhase switch
        {
            ProgressionPhase.EarlyGame => "Early Game",
            ProgressionPhase.MidGame => "Mid Game",
            ProgressionPhase.EndGame => "End Game",
            ProgressionPhase.Mastery => "Mastery",
            _ => "Unknown"
        };

        // Data points
        DataPointsText.Text = progress.DataPointsEarned.ToString("N0");

        // Corporation
        CorporationText.Text = $"{progress.HighestCorporationName} (Lvl {progress.HighestCorporationLevel})";

        // Overall progress
        var overallPercent = progress.OverallProgress * 100;
        OverallProgressBar.Value = overallPercent;
        OverallProgressText.Text = $"{overallPercent:F1}%";

        // Blueprint progress
        var blueprintPercent = progress.BlueprintProgress * 100;
        BlueprintProgressBar.Value = blueprintPercent;
        BlueprintProgressText.Text = $"{progress.BlueprintsUnlocked} / {progress.BlueprintsTotal}";

        // Map status
        if (progress.MapUnlocked)
        {
            MapStatusText.Text = "Unlocked";
            MapStatusText.Foreground = (Brush)FindResource("SuccessBrush");
        }
        else
        {
            MapStatusText.Text = "Locked (Need Moon Energy Lvl 3)";
            MapStatusText.Foreground = (Brush)FindResource("WarningBrush");
        }

        // Corporation breakdown
        var corpDisplayItems = progress.Corporations
            .OrderByDescending(c => c.CurrentLevel)
            .ThenByDescending(c => c.CurrentXP)
            .Select(c => new CorporationDisplayInfo
            {
                Name = c.DisplayName,
                Level = c.CurrentLevel,
                XP = c.CurrentXP
            })
            .ToList();

        CorporationsList.ItemsSource = corpDisplayItems;
        NoCorporationsText.Visibility = corpDisplayItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Wave
        WaveText.Text = string.IsNullOrEmpty(progress.CurrentWave)
            ? "N/A"
            : $"{progress.CurrentWave} - {progress.CurrentWaveStage}";

        // Badges
        BadgesList.ItemsSource = progress.EarnedBadges;
        BadgeCountText.Text = $"{progress.EarnedBadges.Count} / {GameCompanion.Module.StarRupture.Progression.Badges.AllBadges.Count}";

        NoBadgesText.Visibility = progress.EarnedBadges.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void UpdateCataclysm(CataclysmState state)
    {
        CataclysmWaveText.Text = state.CurrentWave;
        CataclysmStageText.Text = state.CurrentStage;
        CataclysmProgressBar.Value = state.StageProgress * 100;
        CataclysmTimeText.Text = state.TimeRemainingDisplay;

        var (color, label) = state.Urgency switch
        {
            CataclysmUrgency.Safe => ("#00FF88", "SAFE"),
            CataclysmUrgency.Caution => ("#FFB800", "CAUTION"),
            CataclysmUrgency.Warning => ("#FF6B35", "WARNING"),
            CataclysmUrgency.Critical => ("#FF4757", "CRITICAL"),
            _ => ("#00FF88", "SAFE")
        };

        var brush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(color));
        CataclysmTimeText.Foreground = brush;
        CataclysmUrgencyText.Text = label;
        CataclysmUrgencyText.Foreground = brush;
    }
}
