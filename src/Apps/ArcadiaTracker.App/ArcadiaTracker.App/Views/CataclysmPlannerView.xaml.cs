using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for CataclysmPlannerView.xaml
/// </summary>
public partial class CataclysmPlannerView : UserControl
{
    public CataclysmPlannerView()
    {
        InitializeComponent();
    }

    public void UpdatePlan(CataclysmPlan plan)
    {
        ReadinessText.Text = $"{plan.Readiness.OverallScore:F0}%";
        TimeToWaveText.Text = plan.TimeToNextWave.TotalMinutes >= 1
            ? $"{(int)plan.TimeToNextWave.TotalMinutes}m"
            : $"{plan.TimeToNextWave.Seconds}s";

        var completed = plan.Tasks.Count(t => t.IsCompleted);
        TasksText.Text = $"{completed}/{plan.Tasks.Count}";

        var critical = plan.Tasks.Count(t => t.Priority == TaskPriority.Critical && !t.IsCompleted);
        CriticalText.Text = critical.ToString();

        DefenseText.Text = $"{plan.Readiness.DefenseScore:F0}%";

        TasksList.ItemsSource = plan.Tasks;
        ResourcesList.ItemsSource = plan.RequiredResources;
        DefensesList.ItemsSource = plan.DefenseRecommendations;
    }
}
