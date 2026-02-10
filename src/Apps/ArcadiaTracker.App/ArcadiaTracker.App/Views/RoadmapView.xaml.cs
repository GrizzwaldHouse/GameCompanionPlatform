using System.Windows;
using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;
using ArcadiaTracker.App.ViewModels;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Roadmap view showing personalized recommendations.
/// </summary>
public partial class RoadmapView : UserControl
{
    private readonly RoadmapViewModel _viewModel = new();

    public RoadmapView()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void UpdateProgress(PlayerProgress progress)
    {
        _viewModel.UpdateProgress(progress);

        PhaseAdviceText.Text = _viewModel.CurrentPhaseAdvice;
        RecommendationsList.ItemsSource = _viewModel.Recommendations;

        NoRecommendationsText.Visibility = _viewModel.Recommendations.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
