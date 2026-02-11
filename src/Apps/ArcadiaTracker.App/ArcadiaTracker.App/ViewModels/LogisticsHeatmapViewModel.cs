namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Logistics Heatmap view.
/// </summary>
public sealed partial class LogisticsHeatmapViewModel : ObservableObject
{
    [ObservableProperty]
    private LogisticsHeatmap? _heatmap;

    [ObservableProperty]
    private ObservableCollection<HeatmapCell> _cells = [];

    [ObservableProperty]
    private ObservableCollection<CongestionZone> _congestionZones = [];

    [ObservableProperty]
    private ObservableCollection<DeadZone> _deadZones = [];

    [ObservableProperty]
    private double _gridCellSize;

    [ObservableProperty]
    private int _totalActiveRequests;

    [ObservableProperty]
    private double _averageTrafficDensity;

    [ObservableProperty]
    private string _averageDensityDisplay = "0.0";

    [ObservableProperty]
    private int _congestionCount;

    [ObservableProperty]
    private int _deadZoneCount;

    [ObservableProperty]
    private int _criticalCellCount;

    [ObservableProperty]
    private int _highTrafficCellCount;

    [ObservableProperty]
    private int _mediumTrafficCellCount;

    [ObservableProperty]
    private int _lowTrafficCellCount;

    [ObservableProperty]
    private CongestionZone? _selectedCongestion;

    [ObservableProperty]
    private DeadZone? _selectedDeadZone;

    public void UpdateHeatmap(LogisticsHeatmap heatmap)
    {
        Heatmap = heatmap;
        Cells = new ObservableCollection<HeatmapCell>(heatmap.Cells);
        CongestionZones = new ObservableCollection<CongestionZone>(heatmap.CongestionZones);
        DeadZones = new ObservableCollection<DeadZone>(heatmap.DeadZones);

        GridCellSize = heatmap.GridCellSize;
        TotalActiveRequests = heatmap.TotalActiveRequests;
        AverageTrafficDensity = heatmap.AverageTrafficDensity;
        AverageDensityDisplay = $"{AverageTrafficDensity:F1}";

        CongestionCount = heatmap.CongestionZones.Count;
        DeadZoneCount = heatmap.DeadZones.Count;

        CriticalCellCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.Critical);
        HighTrafficCellCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.High);
        MediumTrafficCellCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.Medium);
        LowTrafficCellCount = heatmap.Cells.Count(c => c.Heat == HeatLevel.Low);
    }
}
