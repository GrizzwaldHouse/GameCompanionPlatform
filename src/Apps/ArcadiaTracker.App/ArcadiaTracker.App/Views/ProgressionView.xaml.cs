using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;
using ArcadiaTracker.App.ViewModels;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Progression view showing detailed phase progress.
/// </summary>
public partial class ProgressionView : UserControl
{
    private readonly ProgressionViewModel _viewModel = new();

    public ProgressionView()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void UpdateProgress(PlayerProgress progress)
    {
        _viewModel.UpdateProgress(progress);
        PhasesList.ItemsSource = _viewModel.Phases;

        // Update stats
        ItemsDiscoveredText.Text = progress.UniqueItemsDiscovered.ToString("N0");
        LockedBlueprintsText.Text = (progress.BlueprintsTotal - progress.BlueprintsUnlocked).ToString("N0");
        BadgesEarnedText.Text = progress.EarnedBadges.Count.ToString();
    }
}
