using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for ExpansionAdvisorView.xaml
/// </summary>
public partial class ExpansionAdvisorView : UserControl
{
    public ExpansionAdvisorView()
    {
        InitializeComponent();
    }

    public void UpdatePlan(ExpansionPlan plan)
    {
        ReadyText.Text = plan.Readiness.IsReady ? "Yes" : "No";
        ReadyText.Foreground = plan.Readiness.IsReady
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");

        SitesText.Text = plan.RecommendedSites.Count.ToString();
        BuildingsText.Text = plan.CurrentBaseStats.TotalBuildings.ToString();
        EfficiencyText.Text = $"{plan.CurrentBaseStats.AverageEfficiency:F0}%";
        WarningsText.Text = plan.Warnings.Count.ToString();

        SitesList.ItemsSource = plan.RecommendedSites;
        WarningsList.ItemsSource = plan.Warnings;
        MissingList.ItemsSource = plan.Readiness.MissingRequirements;

        // Build requirements list
        RequirementsList.Children.Clear();
        AddRequirementItem("Research", plan.Readiness.HasRequiredResearch);
        AddRequirementItem("Resources", plan.Readiness.HasSufficientResources);
        AddRequirementItem("Logistics Capacity", plan.Readiness.HasLogisticsCapacity);
        AddRequirementItem("Power Capacity", plan.Readiness.HasPowerCapacity);

        NoWarningsText.Visibility = plan.Warnings.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void AddRequirementItem(string name, bool isMet)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

        var icon = new TextBlock
        {
            Text = isMet ? "✓" : "✗",
            FontWeight = FontWeights.Bold,
            Foreground = isMet
                ? (Brush)FindResource("SuccessBrush")
                : (Brush)FindResource("ErrorBrush"),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var text = new TextBlock
        {
            Text = name,
            Foreground = (Brush)FindResource("TextPrimaryBrush")
        };

        panel.Children.Add(icon);
        panel.Children.Add(text);
        RequirementsList.Children.Add(panel);
    }
}
