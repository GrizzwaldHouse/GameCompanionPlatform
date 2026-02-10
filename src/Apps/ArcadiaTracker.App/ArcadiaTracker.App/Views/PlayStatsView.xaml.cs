using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Play statistics view showing comprehensive gameplay metrics.
/// </summary>
public partial class PlayStatsView : UserControl
{
    public PlayStatsView()
    {
        InitializeComponent();
    }

    public void UpdateStatistics(PlayStatistics stats)
    {
        // Top stats
        var totalHours = (int)stats.TotalPlayTime.TotalHours;
        var totalMinutes = stats.TotalPlayTime.Minutes;
        TotalPlaytimeText.Text = $"{totalHours}h {totalMinutes}m";

        TotalSessionsText.Text = stats.TotalSessions.ToString();

        var avgHours = (int)stats.AverageSessionLength.TotalHours;
        var avgMinutes = stats.AverageSessionLength.Minutes;
        AvgSessionText.Text = $"{avgHours}h {avgMinutes}m";

        var longestHours = (int)stats.LongestSession.TotalHours;
        var longestMinutes = stats.LongestSession.Minutes;
        LongestSessionText.Text = $"{longestHours}h {longestMinutes}m";

        // Progress rings
        var overallPercent = stats.OverallProgress * 100;
        OverallProgressBar.Value = overallPercent;
        OverallProgressText.Text = $"{overallPercent:F1}%";

        var buildingPercent = stats.BuildingEfficiency * 100;
        BuildingEfficiencyBar.Value = buildingPercent;
        BuildingEfficiencyText.Text = $"{buildingPercent:F1}%";

        var blueprintPercent = stats.BlueprintCompletion * 100;
        BlueprintProgressBar.Value = blueprintPercent;
        BlueprintProgressText.Text = $"{blueprintPercent:F1}%";

        // Progression summary
        CurrentPhaseText.Text = stats.CurrentPhase;

        BadgesEarnedText.Text = $"{stats.BadgesEarned} / {stats.BadgesTotal}";

        var badgePercent = stats.BadgeCompletion * 100;
        BadgeProgressBar.Value = badgePercent;
        BadgeProgressText.Text = $"{badgePercent:F1}%";

        DataPointsText.Text = stats.DataPointsEarned.ToString("N0");
        UniqueItemsText.Text = stats.UniqueItemsDiscovered.ToString();

        // Corporation summary
        HighestCorpNameText.Text = stats.HighestCorporationName ?? "None";
        HighestCorpLevelText.Text = stats.HighestCorporationLevel.ToString();
        TotalCorpXPText.Text = stats.TotalCorporationXP.ToString("N0");

        // Building summary
        TotalBuildingsText.Text = stats.TotalBuildingsPlaced.ToString();
        OperationalBuildingsText.Text = stats.OperationalBuildings.ToString();
        DisabledBuildingsText.Text = stats.DisabledBuildings.ToString();
        MalfunctioningBuildingsText.Text = stats.MalfunctioningBuildings.ToString();
    }
}
