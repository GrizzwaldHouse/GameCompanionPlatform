namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Power Grid Analyzer view.
/// </summary>
public sealed partial class PowerGridViewModel : ObservableObject
{
    [ObservableProperty]
    private PowerGridAnalysis? _analysis;

    [ObservableProperty]
    private ObservableCollection<GridNetwork> _networks = [];

    [ObservableProperty]
    private ObservableCollection<PowerWarning> _warnings = [];

    [ObservableProperty]
    private ObservableCollection<GeneratorPlacement> _suggestions = [];

    [ObservableProperty]
    private double _totalGeneration;

    [ObservableProperty]
    private double _totalConsumption;

    [ObservableProperty]
    private double _powerBalance;

    [ObservableProperty]
    private double _utilizationPercent;

    [ObservableProperty]
    private string _utilizationDisplay = "0%";

    [ObservableProperty]
    private GridStatus _overallStatus = GridStatus.Disconnected;

    [ObservableProperty]
    private string _statusDisplay = "Disconnected";

    [ObservableProperty]
    private int _networkCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _brownoutRiskCount;

    [ObservableProperty]
    private GridNetwork? _selectedNetwork;

    public void UpdateAnalysis(PowerGridAnalysis analysis)
    {
        Analysis = analysis;
        Networks = new ObservableCollection<GridNetwork>(analysis.Networks);
        Warnings = new ObservableCollection<PowerWarning>(analysis.Warnings);
        Suggestions = new ObservableCollection<GeneratorPlacement>(analysis.PlacementSuggestions);

        TotalGeneration = analysis.TotalGeneration;
        TotalConsumption = analysis.TotalConsumption;
        PowerBalance = TotalGeneration - TotalConsumption;

        if (TotalGeneration > 0)
        {
            UtilizationPercent = (TotalConsumption / TotalGeneration) * 100;
            UtilizationDisplay = $"{UtilizationPercent:F1}%";
        }
        else
        {
            UtilizationPercent = 0;
            UtilizationDisplay = "N/A";
        }

        OverallStatus = analysis.OverallStatus;
        StatusDisplay = OverallStatus switch
        {
            GridStatus.Healthy => "Healthy",
            GridStatus.Stable => "Stable",
            GridStatus.Strained => "Strained",
            GridStatus.Brownout => "BROWNOUT",
            GridStatus.Disconnected => "Disconnected",
            _ => "Unknown"
        };

        NetworkCount = analysis.Networks.Count;
        WarningCount = analysis.Warnings.Count;
        BrownoutRiskCount = analysis.Networks.Count(n => n.IsBrownoutRisk);
    }
}
