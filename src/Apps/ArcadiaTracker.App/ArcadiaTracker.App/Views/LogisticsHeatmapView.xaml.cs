using System.Windows;
using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for LogisticsHeatmapView.xaml
/// </summary>
public partial class LogisticsHeatmapView : UserControl
{
    public LogisticsHeatmapView()
    {
        InitializeComponent();
    }

    public void UpdateHeatmap(LogisticsHeatmap heatmap)
    {
        ActiveRequestsText.Text = heatmap.TotalActiveRequests.ToString();
        AvgDensityText.Text = $"{heatmap.AverageTrafficDensity:F1}";
        CongestionText.Text = heatmap.CongestionZones.Count.ToString();
        DeadZonesText.Text = heatmap.DeadZones.Count.ToString();

        var criticalCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.Critical);
        var highCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.High);
        var mediumCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.Medium);
        var lowCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.Low);
        var noneCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.None);

        CriticalCellsText.Text = criticalCount.ToString();

        // Update legend counts
        NoneCellCount.Text = $"{noneCount} cells";
        LowCellCount.Text = $"{lowCount} cells";
        MediumCellCount.Text = $"{mediumCount} cells";
        HighCellCount.Text = $"{highCount} cells";
        CriticalCellCountLegend.Text = $"{criticalCount} cells";

        // Update lists
        CellList.ItemsSource = heatmap.Cells.Where(c => c.Heat != HeatLevel.None).ToList();
        CongestionList.ItemsSource = heatmap.CongestionZones;
        DeadZoneList.ItemsSource = heatmap.DeadZones;

        // Show/hide empty states
        NoCongestionText.Visibility = heatmap.CongestionZones.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        NoDeadZonesText.Visibility = heatmap.DeadZones.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
