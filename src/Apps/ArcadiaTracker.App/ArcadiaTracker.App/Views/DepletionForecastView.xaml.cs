using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for DepletionForecastView.xaml
/// </summary>
public partial class DepletionForecastView : UserControl
{
    public DepletionForecastView()
    {
        InitializeComponent();
    }

    public void UpdateForecast(DepletionForecast forecast)
    {
        SustainabilityText.Text = $"{forecast.Sustainability.OverallScore:F0}%";
        SustainabilityText.Foreground = forecast.Sustainability.Level switch
        {
            SustainabilityLevel.Excellent or SustainabilityLevel.Good => (Brush)FindResource("SuccessBrush"),
            SustainabilityLevel.Moderate => (Brush)FindResource("PrimaryBrush"),
            SustainabilityLevel.Poor => (Brush)FindResource("WarningBrush"),
            _ => (Brush)FindResource("ErrorBrush")
        };

        SustainableText.Text = forecast.Sustainability.SustainableResources.ToString();
        DepletingText.Text = forecast.Sustainability.DepletingResources.ToString();
        CriticalText.Text = forecast.Sustainability.CriticalResources.ToString();
        AlertsText.Text = forecast.Alerts.Count.ToString();

        ForecastsList.ItemsSource = forecast.Forecasts;
        AlertsList.ItemsSource = forecast.Alerts;
        MitigationsList.ItemsSource = forecast.Mitigations;

        NoAlertsText.Visibility = forecast.Alerts.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
