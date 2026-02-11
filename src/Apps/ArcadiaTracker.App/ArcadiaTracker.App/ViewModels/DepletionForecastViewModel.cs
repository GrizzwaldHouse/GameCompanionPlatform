namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Resource Depletion Forecast view.
/// </summary>
public sealed partial class DepletionForecastViewModel : ObservableObject
{
    [ObservableProperty]
    private DepletionForecast? _forecast;

    [ObservableProperty]
    private ObservableCollection<ResourceForecast> _forecasts = [];

    [ObservableProperty]
    private ObservableCollection<DepletionAlert> _alerts = [];

    [ObservableProperty]
    private ObservableCollection<ResourceMitigation> _mitigations = [];

    [ObservableProperty]
    private SustainabilityScore? _sustainability;

    [ObservableProperty]
    private string _forecastHorizon = "24 hours";

    [ObservableProperty]
    private int _sustainableCount;

    [ObservableProperty]
    private int _depletingCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _alertCount;

    [ObservableProperty]
    private ResourceForecast? _selectedForecast;

    public void UpdateForecast(DepletionForecast forecast)
    {
        Forecast = forecast;
        Forecasts = new ObservableCollection<ResourceForecast>(forecast.Forecasts);
        Alerts = new ObservableCollection<DepletionAlert>(forecast.Alerts);
        Mitigations = new ObservableCollection<ResourceMitigation>(forecast.Mitigations);
        Sustainability = forecast.Sustainability;

        ForecastHorizon = forecast.ForecastHorizon.TotalHours >= 24
            ? $"{forecast.ForecastHorizon.TotalDays:F1} days"
            : $"{forecast.ForecastHorizon.TotalHours:F0} hours";

        SustainableCount = forecast.Sustainability.SustainableResources;
        DepletingCount = forecast.Sustainability.DepletingResources;
        CriticalCount = forecast.Sustainability.CriticalResources;
        AlertCount = forecast.Alerts.Count;
    }
}
