namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Cataclysm Survival Planner view.
/// </summary>
public sealed partial class CataclysmPlannerViewModel : ObservableObject
{
    [ObservableProperty]
    private CataclysmPlan? _plan;

    [ObservableProperty]
    private CataclysmState? _currentState;

    [ObservableProperty]
    private ObservableCollection<SurvivalTask> _tasks = [];

    [ObservableProperty]
    private ObservableCollection<ResourceRequirement> _resources = [];

    [ObservableProperty]
    private ObservableCollection<DefenseRecommendation> _defenses = [];

    [ObservableProperty]
    private ReadinessScore? _readiness;

    [ObservableProperty]
    private string _nextMilestone = "";

    [ObservableProperty]
    private string _timeToNextWave = "";

    [ObservableProperty]
    private int _completedTasks;

    [ObservableProperty]
    private int _totalTasks;

    [ObservableProperty]
    private int _criticalTasks;

    public void UpdatePlan(CataclysmPlan plan)
    {
        Plan = plan;
        CurrentState = plan.CurrentState;
        Tasks = new ObservableCollection<SurvivalTask>(plan.Tasks);
        Resources = new ObservableCollection<ResourceRequirement>(plan.RequiredResources);
        Defenses = new ObservableCollection<DefenseRecommendation>(plan.DefenseRecommendations);
        Readiness = plan.Readiness;
        NextMilestone = plan.NextMilestone;
        TimeToNextWave = plan.TimeToNextWave.TotalMinutes >= 1
            ? $"{(int)plan.TimeToNextWave.TotalMinutes}m {plan.TimeToNextWave.Seconds}s"
            : $"{plan.TimeToNextWave.Seconds}s";

        CompletedTasks = plan.Tasks.Count(t => t.IsCompleted);
        TotalTasks = plan.Tasks.Count;
        CriticalTasks = plan.Tasks.Count(t => t.Priority == TaskPriority.Critical && !t.IsCompleted);
    }
}
