using System.Windows;
using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for BottleneckView.xaml
/// </summary>
public partial class BottleneckView : UserControl
{
    public BottleneckView()
    {
        InitializeComponent();
    }

    public void UpdateAnalysis(BottleneckAnalysis analysis)
    {
        TotalMachinesText.Text = analysis.TotalMachines.ToString();

        var healthPercent = analysis.TotalMachines > 0
            ? ((analysis.TotalMachines - analysis.BottleneckCount) / (double)analysis.TotalMachines) * 100
            : 0;
        HealthText.Text = $"{healthPercent:F1}%";

        var criticalCount = analysis.Bottlenecks.Count(b => b.Severity == BottleneckSeverity.Critical);
        var highCount = analysis.Bottlenecks.Count(b => b.Severity == BottleneckSeverity.High);
        var lowMediumCount = analysis.Bottlenecks.Count(b =>
            b.Severity == BottleneckSeverity.Low || b.Severity == BottleneckSeverity.Medium);

        CriticalText.Text = criticalCount.ToString();
        HighText.Text = highCount.ToString();
        LowMediumText.Text = lowMediumCount.ToString();

        BottleneckList.ItemsSource = analysis.Bottlenecks;

        EmptyState.Visibility = analysis.BottleneckCount == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
