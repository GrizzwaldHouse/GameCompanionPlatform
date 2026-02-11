namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Bottleneck Detector view.
/// </summary>
public sealed partial class BottleneckViewModel : ObservableObject
{
    [ObservableProperty]
    private BottleneckAnalysis? _analysis;

    [ObservableProperty]
    private ObservableCollection<BottleneckInfo> _bottlenecks = [];

    [ObservableProperty]
    private ObservableCollection<ProductionChain> _chains = [];

    [ObservableProperty]
    private int _totalMachines;

    [ObservableProperty]
    private int _bottleneckCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _highCount;

    [ObservableProperty]
    private int _mediumCount;

    [ObservableProperty]
    private int _lowCount;

    [ObservableProperty]
    private double _healthPercent;

    [ObservableProperty]
    private string _healthDisplay = "0%";

    [ObservableProperty]
    private BottleneckInfo? _selectedBottleneck;

    public void UpdateAnalysis(BottleneckAnalysis analysis)
    {
        Analysis = analysis;
        Bottlenecks = new ObservableCollection<BottleneckInfo>(analysis.Bottlenecks);
        Chains = new ObservableCollection<ProductionChain>(analysis.Chains);
        TotalMachines = analysis.TotalMachines;
        BottleneckCount = analysis.BottleneckCount;

        CriticalCount = analysis.Bottlenecks.Count(b => b.Severity == BottleneckSeverity.Critical);
        HighCount = analysis.Bottlenecks.Count(b => b.Severity == BottleneckSeverity.High);
        MediumCount = analysis.Bottlenecks.Count(b => b.Severity == BottleneckSeverity.Medium);
        LowCount = analysis.Bottlenecks.Count(b => b.Severity == BottleneckSeverity.Low);

        if (TotalMachines > 0)
        {
            HealthPercent = ((TotalMachines - BottleneckCount) / (double)TotalMachines) * 100;
            HealthDisplay = $"{HealthPercent:F1}%";
        }
        else
        {
            HealthPercent = 0;
            HealthDisplay = "N/A";
        }
    }
}
