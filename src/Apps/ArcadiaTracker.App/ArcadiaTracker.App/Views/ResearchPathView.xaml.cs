using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for ResearchPathView.xaml
/// </summary>
public partial class ResearchPathView : UserControl
{
    public ResearchPathView()
    {
        InitializeComponent();
    }

    public void UpdatePath(ResearchPath path)
    {
        CompletionText.Text = $"{path.CompletionPercent:F1}%";
        DataPointsText.Text = path.EstimatedDataPointsNeeded.ToString();
        HighPriorityText.Text = path.HighPriorityUnlocks.Count.ToString();
        AvailableText.Text = path.CurrentlyAvailable.Count.ToString();

        PathList.ItemsSource = path.RecommendedPath;
        HighPriorityList.ItemsSource = path.HighPriorityUnlocks.Take(10);
        AvailableList.ItemsSource = path.CurrentlyAvailable.Take(10);
    }
}
