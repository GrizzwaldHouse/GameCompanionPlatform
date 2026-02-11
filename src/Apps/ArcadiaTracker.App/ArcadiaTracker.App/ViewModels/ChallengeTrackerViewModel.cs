namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for the Challenge Mode Tracker view.
/// </summary>
public sealed partial class ChallengeTrackerViewModel : ObservableObject
{
    private readonly ChallengeTrackerService _service;

    [ObservableProperty]
    private ChallengeTracker? _tracker;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private Challenge? _selectedChallenge;

    [ObservableProperty]
    private ObservableCollection<ChallengeProgress> _inProgressChallenges = [];

    [ObservableProperty]
    private ObservableCollection<Challenge> _allChallenges = [];

    [ObservableProperty]
    private ObservableCollection<Challenge> _completedChallenges = [];

    [ObservableProperty]
    private string _filterMode = "All"; // All, InProgress, Completed

    private StarRuptureSave? _currentSave;

    public ChallengeTrackerViewModel(ChallengeTrackerService service)
    {
        _service = service;
    }

    /// <summary>
    /// Loads challenge data from the provided save file.
    /// </summary>
    public async Task LoadAsync(StarRuptureSave save)
    {
        IsLoading = true;
        ErrorMessage = null;
        _currentSave = save;

        try
        {
            await Task.Run(() =>
            {
                var result = _service.GetChallengeTracker(save);

                if (result.IsFailure)
                {
                    ErrorMessage = result.Error;
                    return;
                }

                Tracker = result.Value;
                UpdateCollections();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load challenges: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_currentSave != null)
        {
            await LoadAsync(_currentSave);
        }
    }

    [RelayCommand]
    private void SelectChallenge(Challenge? challenge)
    {
        SelectedChallenge = challenge;
    }

    private void UpdateCollections()
    {
        if (Tracker == null) return;

        AllChallenges = new ObservableCollection<Challenge>(Tracker.Challenges);
        CompletedChallenges = new ObservableCollection<Challenge>(
            Tracker.Challenges.Where(c => c.IsCompleted));
        InProgressChallenges = new ObservableCollection<ChallengeProgress>(Tracker.InProgress);

        ApplyFilter();
    }

    partial void OnFilterModeChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (Tracker == null) return;

        var filtered = FilterMode switch
        {
            "InProgress" => Tracker.Challenges.Where(c => !c.IsCompleted &&
                Tracker.InProgress.Any(p => p.Challenge.Id == c.Id)),
            "Completed" => Tracker.Challenges.Where(c => c.IsCompleted),
            _ => Tracker.Challenges
        };

        AllChallenges = new ObservableCollection<Challenge>(filtered);
    }
}
