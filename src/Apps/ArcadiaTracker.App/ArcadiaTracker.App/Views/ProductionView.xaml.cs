using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Production view showing machine status and efficiency.
/// </summary>
public partial class ProductionView : UserControl
{
    public ProductionView()
    {
        InitializeComponent();
    }

    public void UpdateProduction(ProductionSummary summary)
    {
        // Top stats
        TotalMachinesText.Text = summary.TotalMachines.ToString();
        RunningText.Text = summary.RunningMachines.ToString();
        DisabledText.Text = summary.DisabledMachines.ToString();
        MalfunctionText.Text = summary.MalfunctioningMachines.ToString();
        EfficiencyText.Text = $"{summary.EfficiencyPercent:F1}%";

        // Category breakdown
        CategoryList.ItemsSource = summary.ByCategory;
        NoCategoriesText.Visibility = summary.ByCategory.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Power grids
        PowerGridCountText.Text = summary.PowerSummary.TotalGrids.ToString();
        PowerGridList.ItemsSource = summary.PowerSummary.Grids;

        // Base comparison list
        BaseList.ItemsSource = summary.PerBase.OrderByDescending(b => b.EfficiencyPercent);
        NoBasesText.Visibility = summary.PerBase.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
