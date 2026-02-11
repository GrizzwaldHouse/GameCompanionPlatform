namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Research Path Optimizer view.
/// </summary>
public sealed partial class ResearchPathViewModel : ObservableObject
{
    [ObservableProperty]
    private ResearchPath? _path;

    [ObservableProperty]
    private ObservableCollection<ResearchStep> _recommendedPath = [];

    [ObservableProperty]
    private ObservableCollection<ResearchNode> _highPriority = [];

    [ObservableProperty]
    private ObservableCollection<ResearchNode> _available = [];

    [ObservableProperty]
    private ResearchGoal? _currentGoal;

    [ObservableProperty]
    private int _dataPointsNeeded;

    [ObservableProperty]
    private double _completionPercent;

    [ObservableProperty]
    private string _completionDisplay = "0%";

    [ObservableProperty]
    private ResearchGoalType _selectedGoalType = ResearchGoalType.Automation;

    public void UpdatePath(ResearchPath path)
    {
        Path = path;
        RecommendedPath = new ObservableCollection<ResearchStep>(path.RecommendedPath);
        HighPriority = new ObservableCollection<ResearchNode>(path.HighPriorityUnlocks);
        Available = new ObservableCollection<ResearchNode>(path.CurrentlyAvailable);
        CurrentGoal = path.CurrentGoal;
        DataPointsNeeded = path.EstimatedDataPointsNeeded;
        CompletionPercent = path.CompletionPercent;
        CompletionDisplay = $"{path.CompletionPercent:F1}%";
    }
}
