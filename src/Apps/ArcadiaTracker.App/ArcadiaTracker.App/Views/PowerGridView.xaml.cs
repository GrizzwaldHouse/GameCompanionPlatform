using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for PowerGridView.xaml
/// </summary>
public partial class PowerGridView : UserControl
{
    public PowerGridView()
    {
        InitializeComponent();
    }

    public void UpdateAnalysis(PowerGridAnalysis analysis)
    {
        GenerationText.Text = $"{analysis.TotalGeneration:F0} MW";
        ConsumptionText.Text = $"{analysis.TotalConsumption:F0} MW";

        var balance = analysis.TotalGeneration - analysis.TotalConsumption;
        BalanceText.Text = balance >= 0 ? $"+{balance:F0} MW" : $"{balance:F0} MW";
        BalanceText.Foreground = balance >= 0
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");

        var utilization = analysis.TotalGeneration > 0
            ? (analysis.TotalConsumption / analysis.TotalGeneration) * 100
            : 0;
        UtilizationText.Text = $"{utilization:F1}%";

        StatusText.Text = analysis.OverallStatus switch
        {
            GridStatus.Healthy => "Healthy",
            GridStatus.Stable => "Stable",
            GridStatus.Strained => "Strained",
            GridStatus.Brownout => "BROWNOUT",
            GridStatus.Disconnected => "Disconnected",
            _ => "Unknown"
        };

        StatusText.Foreground = analysis.OverallStatus switch
        {
            GridStatus.Healthy => (Brush)FindResource("SuccessBrush"),
            GridStatus.Stable => (Brush)FindResource("PrimaryBrush"),
            GridStatus.Strained => (Brush)FindResource("WarningBrush"),
            GridStatus.Brownout => (Brush)FindResource("ErrorBrush"),
            _ => (Brush)FindResource("TextSecondaryBrush")
        };

        NetworkList.ItemsSource = analysis.Networks;
        WarningList.ItemsSource = analysis.Warnings;
        SuggestionList.ItemsSource = analysis.PlacementSuggestions;

        NoWarningsText.Visibility = analysis.Warnings.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
