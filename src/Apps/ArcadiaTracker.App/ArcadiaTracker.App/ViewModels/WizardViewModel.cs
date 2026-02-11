namespace ArcadiaTracker.App.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for the What's Next Wizard view.
/// </summary>
public sealed partial class WizardViewModel : ObservableObject
{
    private readonly WizardService _wizardService;
    private StarRuptureSave? _currentSave;

    [ObservableProperty]
    private WizardRecommendations? _recommendations;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private WizardSuggestion? _selectedSuggestion;

    [ObservableProperty]
    private WizardGoal? _primaryGoal;

    [ObservableProperty]
    private string _currentPhase = "Unknown";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private ObservableCollection<WizardSuggestion> _suggestions = [];

    [ObservableProperty]
    private ObservableCollection<WizardGoal> _availableGoals = [];

    [ObservableProperty]
    private ObservableCollection<GoalMilestone> _primaryGoalMilestones = [];

    public WizardViewModel(WizardService wizardService)
    {
        _wizardService = wizardService;
    }

    /// <summary>
    /// Loads recommendations for the given save file.
    /// </summary>
    public async Task LoadAsync(StarRuptureSave save, CancellationToken cancellationToken = default)
    {
        _currentSave = save;
        await RefreshAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSave == null)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await Task.Run(() =>
            {
                var result = _wizardService.GetRecommendations(_currentSave);

                if (result.IsSuccess && result.Value != null)
                {
                    Recommendations = result.Value;
                    CurrentPhase = result.Value.CurrentPhase;
                    ProgressPercent = result.Value.ProgressPercent;
                    Suggestions = new ObservableCollection<WizardSuggestion>(result.Value.Suggestions);
                    AvailableGoals = new ObservableCollection<WizardGoal>(result.Value.AvailableGoals);
                    PrimaryGoal = result.Value.PrimaryGoal;

                    if (PrimaryGoal != null)
                    {
                        PrimaryGoalMilestones = new ObservableCollection<GoalMilestone>(PrimaryGoal.Milestones);
                    }
                    else
                    {
                        PrimaryGoalMilestones = [];
                    }

                    // Auto-select first suggestion if none selected
                    if (SelectedSuggestion == null && Suggestions.Count > 0)
                    {
                        SelectedSuggestion = Suggestions[0];
                    }
                }
                else
                {
                    ErrorMessage = result.Error ?? "Failed to load recommendations";
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading recommendations: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectSuggestion(WizardSuggestion? suggestion)
    {
        SelectedSuggestion = suggestion;
    }

    [RelayCommand]
    private async Task SetActiveGoalAsync(WizardGoal? goal, CancellationToken cancellationToken = default)
    {
        if (goal == null)
            return;

        PrimaryGoal = goal;
        PrimaryGoalMilestones = new ObservableCollection<GoalMilestone>(goal.Milestones);

        // Optionally refresh recommendations based on new goal
        await RefreshAsync(cancellationToken);
    }
}
