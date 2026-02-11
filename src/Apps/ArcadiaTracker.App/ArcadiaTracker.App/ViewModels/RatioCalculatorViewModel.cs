namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for the Ratio Calculator view.
/// </summary>
public sealed partial class RatioCalculatorViewModel : ObservableObject
{
    private readonly RatioCalculatorService _calculatorService;
    private StarRuptureSave? _currentSave;

    [ObservableProperty]
    private RatioCalculation? _calculation;

    [ObservableProperty]
    private ObservableCollection<string> _availableItems = [];

    [ObservableProperty]
    private string? _selectedItem;

    [ObservableProperty]
    private double _targetRate = 60;

    [ObservableProperty]
    private ObservableCollection<MachineRequirement> _requirements = [];

    [ObservableProperty]
    private ObservableCollection<ComparisonDelta> _comparisons = [];

    [ObservableProperty]
    private bool _canAchieveTarget;

    [ObservableProperty]
    private string? _bottleneckReason;

    [ObservableProperty]
    private bool _hasComparison;

    [ObservableProperty]
    private string _statusMessage = "Select an item and rate to calculate";

    public RatioCalculatorViewModel(RatioCalculatorService calculatorService)
    {
        _calculatorService = calculatorService;
        LoadAvailableItems();
    }

    public void SetCurrentSave(StarRuptureSave? save)
    {
        _currentSave = save;
        if (SelectedItem != null)
        {
            CalculateCommand.Execute(null);
        }
    }

    private void LoadAvailableItems()
    {
        var items = _calculatorService.GetAvailableItems();
        AvailableItems = new ObservableCollection<string>(items);
    }

    [RelayCommand]
    private void Calculate()
    {
        if (string.IsNullOrEmpty(SelectedItem))
        {
            StatusMessage = "Please select an item first";
            return;
        }

        if (TargetRate <= 0)
        {
            StatusMessage = "Target rate must be greater than 0";
            return;
        }

        var result = _calculatorService.CalculateRatio(SelectedItem, TargetRate, _currentSave);

        if (result.IsSuccess)
        {
            Calculation = result.Value;
            Requirements = new ObservableCollection<MachineRequirement>(result.Value!.Requirements);
            Comparisons = new ObservableCollection<ComparisonDelta>(result.Value.CurrentVsRequired);
            CanAchieveTarget = result.Value.CanAchieveTarget;
            BottleneckReason = result.Value.BottleneckReason;
            HasComparison = Comparisons.Count > 0;

            StatusMessage = CanAchieveTarget
                ? "Target can be achieved with current build!"
                : BottleneckReason ?? "Cannot achieve target with current build";
        }
        else
        {
            StatusMessage = result.Error ?? "Calculation failed";
        }
    }

    partial void OnSelectedItemChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            CalculateCommand.Execute(null);
        }
    }

    partial void OnTargetRateChanged(double value)
    {
        if (!string.IsNullOrEmpty(SelectedItem) && value > 0)
        {
            CalculateCommand.Execute(null);
        }
    }
}
