namespace GameCompanion.Engine.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Core.Interfaces;
using GameCompanion.Core.Models;

/// <summary>
/// Base ViewModel for the dashboard view.
/// Shows current phase, next action, save health, and quick buttons.
/// </summary>
public abstract partial class DashboardViewModel : ViewModelBase
{
    protected readonly IGameModule GameModule;
    protected readonly IProgressionMap ProgressionMap;

    [ObservableProperty]
    private string _currentPhaseName = string.Empty;

    [ObservableProperty]
    private string _currentPhaseDescription = string.Empty;

    [ObservableProperty]
    private double _phaseProgress;

    [ObservableProperty]
    private string _nextActionTitle = string.Empty;

    [ObservableProperty]
    private string _nextActionDescription = string.Empty;

    [ObservableProperty]
    private SaveHealthStatus _saveHealth = SaveHealthStatus.Unknown;

    [ObservableProperty]
    private string _saveHealthMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DashboardQuickButton> _quickButtons = [];

    [ObservableProperty]
    private ObservableCollection<RecentActivityItem> _recentActivity = [];

    protected DashboardViewModel(IGameModule gameModule)
    {
        GameModule = gameModule;
        ProgressionMap = gameModule.GetProgressionMap();
        InitializeQuickButtons();
    }

    /// <summary>
    /// Override to set up the quick action buttons.
    /// </summary>
    protected abstract void InitializeQuickButtons();

    /// <summary>
    /// Updates the dashboard with the current progression state.
    /// </summary>
    protected void UpdateFromProgressionState(IProgressionState state)
    {
        var currentPhase = ProgressionMap.GetCurrentPhase(state);
        CurrentPhaseName = currentPhase.Name;
        CurrentPhaseDescription = currentPhase.Description;
        PhaseProgress = ProgressionMap.GetProgressPercentage(state);

        var nextStep = ProgressionMap.GetNextRecommendedStep(state);
        if (nextStep != null)
        {
            NextActionTitle = nextStep.Title;
            NextActionDescription = nextStep.WhyItMatters;
        }
        else
        {
            NextActionTitle = "All steps complete!";
            NextActionDescription = "You've completed all recommended steps in this phase.";
        }
    }

    [RelayCommand]
    protected abstract Task RefreshAsync();

    [RelayCommand]
    protected abstract Task CreateBackupAsync();

    [RelayCommand]
    protected abstract Task ResumeProgressionAsync();
}

/// <summary>
/// Status of the save file health.
/// </summary>
public enum SaveHealthStatus
{
    Unknown,
    Healthy,
    NeedsBackup,
    OutOfSync,
    Corrupted
}

/// <summary>
/// A quick action button on the dashboard.
/// </summary>
public sealed class DashboardQuickButton
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required Func<Task> ExecuteAsync { get; init; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// An item in the recent activity feed.
/// </summary>
public sealed class RecentActivityItem
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Icon { get; init; }
}
