using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Build Planner view for planning and tracking factory layouts.
/// </summary>
public partial class BuildPlannerView : UserControl
{
    private BuildPlannerViewModel? ViewModel => DataContext as BuildPlannerViewModel;

    public BuildPlannerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (ViewModel == null) return;

        switch (e.PropertyName)
        {
            case nameof(ViewModel.SelectedPlan):
                UpdateSelectedPlanDisplay();
                break;
            case nameof(ViewModel.ErrorMessage):
                UpdateErrorDisplay();
                break;
            case nameof(ViewModel.Plans):
                UpdatePlansDisplay();
                break;
        }
    }

    private void UpdateSelectedPlanDisplay()
    {
        if (ViewModel?.SelectedPlan == null)
        {
            SelectedPlanPanel.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
            return;
        }

        var plan = ViewModel.SelectedPlan;

        SelectedPlanPanel.Visibility = Visibility.Visible;
        EmptyStatePanel.Visibility = Visibility.Collapsed;

        // Plan header
        PlanNameText.Text = plan.Name;
        PlanDescriptionText.Text = string.IsNullOrWhiteSpace(plan.Description)
            ? "No description"
            : plan.Description;

        // Stats
        TotalBuildingsText.Text = plan.Stats.TotalBuildings.ToString();
        BuiltCountText.Text = plan.Stats.BuiltCount.ToString();
        RemainingCountText.Text = plan.Stats.RemainingCount.ToString();
        PowerUsageText.Text = $"{plan.Stats.EstimatedPowerUsage:F1} MW";
        ThroughputText.Text = $"{plan.Stats.EstimatedThroughput:F0}/min";

        // Completion
        CompletionProgressBar.Value = plan.Stats.CompletionPercent;
        CompletionText.Text = $"{plan.Stats.CompletionPercent:F1}% Complete";

        // Buildings list
        BuildingsList.ItemsSource = plan.Buildings;
        NoBuildingsText.Visibility = plan.Buildings.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Cost breakdown
        var costs = plan.TotalCost.TotalResources.OrderByDescending(kvp => kvp.Value).ToList();
        CostList.ItemsSource = costs;
        NoCostText.Visibility = costs.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateErrorDisplay()
    {
        if (ViewModel == null) return;

        if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
        {
            ErrorBorder.Visibility = Visibility.Visible;
            ErrorText.Text = ViewModel.ErrorMessage;
        }
        else
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdatePlansDisplay()
    {
        if (ViewModel == null) return;

        NoPlansText.Visibility = ViewModel.Plans.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
