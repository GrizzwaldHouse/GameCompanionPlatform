namespace GameCompanion.Engine.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Tasks.Interfaces;

/// <summary>
/// ViewModel for the step guide view.
/// Shows detailed step information with why it matters, actions, and checklist.
/// </summary>
public partial class StepGuideViewModel : ViewModelBase
{
    private readonly ITaskOrchestrator _taskOrchestrator;

    [ObservableProperty]
    private Step? _currentStep;

    [ObservableProperty]
    private string _stepTitle = string.Empty;

    [ObservableProperty]
    private string _whyItMatters = string.Empty;

    [ObservableProperty]
    private ObservableCollection<StepActionViewModel> _actions = [];

    [ObservableProperty]
    private ObservableCollection<ChecklistItemViewModel> _checklist = [];

    [ObservableProperty]
    private bool _isAdvancedVisible;

    [ObservableProperty]
    private double _stepProgress;

    public StepGuideViewModel(ITaskOrchestrator taskOrchestrator)
    {
        _taskOrchestrator = taskOrchestrator;
    }

    /// <summary>
    /// Loads a step for display.
    /// </summary>
    public void LoadStep(Step step)
    {
        CurrentStep = step;
        StepTitle = step.Title;
        WhyItMatters = step.WhyItMatters;

        Actions.Clear();
        foreach (var action in step.Actions)
        {
            Actions.Add(new StepActionViewModel
            {
                Order = action.Order,
                Description = action.Description,
                Hint = action.Hint
            });
        }

        Checklist.Clear();
        foreach (var item in step.Checklist)
        {
            Checklist.Add(new ChecklistItemViewModel
            {
                Id = item.Id,
                Text = item.Text,
                IsAutoDetectable = item.IsAutoDetectable,
                IsCompleted = false
            });
        }

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (Checklist.Count == 0)
        {
            StepProgress = 0;
            return;
        }

        var completed = Checklist.Count(c => c.IsCompleted);
        StepProgress = (double)completed / Checklist.Count;
    }

    [RelayCommand]
    private async Task ToggleChecklistItemAsync(ChecklistItemViewModel item)
    {
        if (CurrentStep == null || _taskOrchestrator.CurrentTaskListId == null)
            return;

        item.IsCompleted = !item.IsCompleted;

        var result = await _taskOrchestrator.MarkChecklistItemAsync(
            _taskOrchestrator.CurrentTaskListId,
            CurrentStep.Id,
            item.Id,
            item.IsCompleted);

        if (result.IsFailure)
        {
            item.IsCompleted = !item.IsCompleted; // Revert
            SetError(result.Error!);
        }

        UpdateProgress();
    }

    [RelayCommand]
    private async Task MarkCompleteAsync()
    {
        if (CurrentStep == null || _taskOrchestrator.CurrentTaskListId == null)
            return;

        var result = await _taskOrchestrator.CompleteStepAsync(
            _taskOrchestrator.CurrentTaskListId,
            CurrentStep.Id);

        if (result.IsFailure)
        {
            SetError(result.Error!);
        }
        else
        {
            SetStatus("Step completed!");
        }
    }

    [RelayCommand]
    private async Task SkipStepAsync()
    {
        if (CurrentStep == null || _taskOrchestrator.CurrentTaskListId == null)
            return;

        var result = await _taskOrchestrator.SkipStepAsync(
            _taskOrchestrator.CurrentTaskListId,
            CurrentStep.Id);

        if (result.IsFailure)
        {
            SetError(result.Error!);
        }
        else
        {
            SetStatus("Step skipped.");
        }
    }

    [RelayCommand]
    private void ToggleAdvanced()
    {
        IsAdvancedVisible = !IsAdvancedVisible;
    }
}

/// <summary>
/// ViewModel for a step action.
/// </summary>
public partial class StepActionViewModel : ObservableObject
{
    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _hint;
}

/// <summary>
/// ViewModel for a checklist item.
/// </summary>
public partial class ChecklistItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _isAutoDetectable;
}
