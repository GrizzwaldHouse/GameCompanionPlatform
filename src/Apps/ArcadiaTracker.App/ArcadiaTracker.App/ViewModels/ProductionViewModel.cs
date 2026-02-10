namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Production view showing machine status and efficiency.
/// </summary>
public sealed partial class ProductionViewModel : ObservableObject
{
    [ObservableProperty]
    private ProductionSummary? _summary;

    [ObservableProperty]
    private string _efficiencyDisplay = "0%";

    [ObservableProperty]
    private double _efficiencyPercent;

    [ObservableProperty]
    private string _totalMachinesDisplay = "0";

    [ObservableProperty]
    private string _runningDisplay = "0";

    [ObservableProperty]
    private string _disabledDisplay = "0";

    [ObservableProperty]
    private string _malfunctionDisplay = "0";

    [ObservableProperty]
    private ObservableCollection<CategoryBreakdown> _categories = [];

    [ObservableProperty]
    private ObservableCollection<BaseProductionInfo> _bases = [];

    [ObservableProperty]
    private ObservableCollection<PowerGridInfo> _powerGrids = [];

    [ObservableProperty]
    private string _powerGridCountDisplay = "0";

    [ObservableProperty]
    private BaseComparison? _comparison;

    [ObservableProperty]
    private bool _isComparing;

    public void UpdateProduction(ProductionSummary summary)
    {
        Summary = summary;
        EfficiencyPercent = summary.EfficiencyPercent;
        EfficiencyDisplay = $"{summary.EfficiencyPercent:F1}%";
        TotalMachinesDisplay = summary.TotalMachines.ToString();
        RunningDisplay = summary.RunningMachines.ToString();
        DisabledDisplay = summary.DisabledMachines.ToString();
        MalfunctionDisplay = summary.MalfunctioningMachines.ToString();
        Categories = new ObservableCollection<CategoryBreakdown>(summary.ByCategory);
        Bases = new ObservableCollection<BaseProductionInfo>(summary.PerBase);
        PowerGrids = new ObservableCollection<PowerGridInfo>(summary.PowerSummary.Grids);
        PowerGridCountDisplay = summary.PowerSummary.TotalGrids.ToString();
    }
}
