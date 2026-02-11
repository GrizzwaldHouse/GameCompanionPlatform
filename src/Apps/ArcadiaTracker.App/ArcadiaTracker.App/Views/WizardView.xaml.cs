using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// What's Next Wizard view showing recommendations and goals.
/// </summary>
public partial class WizardView : UserControl
{
    private WizardViewModel? _viewModel;

    public WizardView()
    {
        InitializeComponent();
    }

    public void SetViewModel(WizardViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        // Wire up commands
        RefreshButton.Click += (s, e) => _viewModel.RefreshCommand.Execute(null);
        RetryButton.Click += (s, e) => _viewModel.RefreshCommand.Execute(null);

        // Wire up suggestion selection
        SuggestionsList.SelectionChanged += OnSuggestionSelected;

        // Wire up goal selector
        GoalSelector.SelectionChanged += OnGoalSelected;

        // Subscribe to property changes
        viewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(WizardViewModel.IsLoading):
                    LoadingOverlay.Visibility = viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                    MainContent.Visibility = viewModel.IsLoading ? Visibility.Collapsed : Visibility.Visible;
                    break;

                case nameof(WizardViewModel.ErrorMessage):
                    if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
                    {
                        ErrorMessageText.Text = viewModel.ErrorMessage;
                        ErrorDisplay.Visibility = Visibility.Visible;
                        MainContent.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ErrorDisplay.Visibility = Visibility.Collapsed;
                        MainContent.Visibility = Visibility.Visible;
                    }
                    break;

                case nameof(WizardViewModel.CurrentPhase):
                    PhaseText.Text = viewModel.CurrentPhase;
                    break;

                case nameof(WizardViewModel.ProgressPercent):
                    ProgressBar.Value = viewModel.ProgressPercent;
                    ProgressText.Text = $"{viewModel.ProgressPercent:F1}%";
                    break;

                case nameof(WizardViewModel.Suggestions):
                    SuggestionsList.ItemsSource = viewModel.Suggestions;
                    NoSuggestionsText.Visibility = viewModel.Suggestions.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    break;

                case nameof(WizardViewModel.AvailableGoals):
                    UpdateGoalSelector(viewModel.AvailableGoals);
                    break;

                case nameof(WizardViewModel.PrimaryGoal):
                    UpdatePrimaryGoal(viewModel.PrimaryGoal);
                    break;

                case nameof(WizardViewModel.PrimaryGoalMilestones):
                    MilestonesList.ItemsSource = viewModel.PrimaryGoalMilestones;
                    NoMilestonesText.Visibility = viewModel.PrimaryGoalMilestones.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    break;

                case nameof(WizardViewModel.SelectedSuggestion):
                    UpdateSelectedSuggestion(viewModel.SelectedSuggestion);
                    break;
            }
        };
    }

    private void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && SuggestionsList.SelectedItem is WizardSuggestion suggestion)
        {
            _viewModel.SelectSuggestionCommand.Execute(suggestion);
        }
    }

    private void OnGoalSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && GoalSelector.SelectedItem is WizardGoal goal)
        {
            _viewModel.SetActiveGoalCommand.Execute(goal);
        }
    }

    private void UpdateGoalSelector(IEnumerable<WizardGoal> goals)
    {
        GoalSelector.ItemsSource = goals;
        GoalSelector.DisplayMemberPath = "Name";

        var activeGoal = goals.FirstOrDefault(g => g.IsActive);
        if (activeGoal != null)
        {
            GoalSelector.SelectedItem = activeGoal;
        }
    }

    private void UpdatePrimaryGoal(WizardGoal? goal)
    {
        if (goal == null)
        {
            PrimaryGoalName.Text = "No active goal";
            PrimaryGoalDescription.Text = "";
            GoalProgressBar.Value = 0;
            GoalProgressText.Text = "0%";
            PrimaryGoalCard.Visibility = Visibility.Collapsed;
            return;
        }

        PrimaryGoalCard.Visibility = Visibility.Visible;
        PrimaryGoalName.Text = goal.Name;
        PrimaryGoalDescription.Text = goal.Description;
        GoalProgressBar.Value = goal.Progress;
        GoalProgressText.Text = $"{goal.Progress:F1}%";

        // Update goal type icon
        var typeEmoji = goal.Type switch
        {
            GoalType.Tutorial => "📖",
            GoalType.MainQuest => "🎯",
            GoalType.SideQuest => "📋",
            GoalType.Achievement => "🏆",
            GoalType.Challenge => "⚔️",
            GoalType.Personal => "⭐",
            _ => "🎯"
        };

        PrimaryGoalName.Text = $"{typeEmoji} {goal.Name}";
    }

    private void UpdateSelectedSuggestion(WizardSuggestion? suggestion)
    {
        if (suggestion == null)
        {
            SelectedTitle.Text = "Select a suggestion";
            SelectedDescription.Text = "";
            StepsHeader.Visibility = Visibility.Collapsed;
            StepsList.ItemsSource = null;
            ReasoningHeader.Visibility = Visibility.Collapsed;
            ReasoningText.Text = "";
            BenefitsHeader.Visibility = Visibility.Collapsed;
            BenefitsList.ItemsSource = null;
            return;
        }

        SelectedTitle.Text = suggestion.Title;
        SelectedDescription.Text = suggestion.Description;

        // Steps
        if (suggestion.Steps.Count > 0)
        {
            StepsHeader.Visibility = Visibility.Visible;
            var indexedSteps = suggestion.Steps.Select((step, index) => new
            {
                Index = index + 1,
                Text = step
            });
            StepsList.ItemsSource = indexedSteps;
        }
        else
        {
            StepsHeader.Visibility = Visibility.Collapsed;
            StepsList.ItemsSource = null;
        }

        // Reasoning
        if (!string.IsNullOrEmpty(suggestion.Reasoning))
        {
            ReasoningHeader.Visibility = Visibility.Visible;
            ReasoningText.Text = suggestion.Reasoning;
        }
        else
        {
            ReasoningHeader.Visibility = Visibility.Collapsed;
            ReasoningText.Text = "";
        }

        // Benefits
        if (suggestion.Benefits.Count > 0)
        {
            BenefitsHeader.Visibility = Visibility.Visible;
            BenefitsList.ItemsSource = suggestion.Benefits;
        }
        else
        {
            BenefitsHeader.Visibility = Visibility.Collapsed;
            BenefitsList.ItemsSource = null;
        }

        // Highlight selected item in list
        HighlightSelectedSuggestion(suggestion);
    }

    private void HighlightSelectedSuggestion(WizardSuggestion selectedSuggestion)
    {
        // Set the ListBox selection to match the selected suggestion
        SuggestionsList.SelectedItem = selectedSuggestion;
    }
}
