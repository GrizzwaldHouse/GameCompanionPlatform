namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using System.Windows.Threading;

/// <summary>
/// ViewModel for the Speedrun Timer feature.
/// </summary>
public sealed partial class SpeedrunViewModel : ObservableObject
{
    private readonly SpeedrunService _speedrunService;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private IReadOnlyList<SpeedrunCategory> _categories = [];

    [ObservableProperty]
    private SpeedrunCategory? _selectedCategory;

    [ObservableProperty]
    private SpeedrunSession? _currentSession;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _currentTimeDisplay = "00:00:00.00";

    [ObservableProperty]
    private ObservableCollection<SpeedrunSplit> _splits = [];

    [ObservableProperty]
    private string _personalBestDisplay = "—";

    [ObservableProperty]
    private string _sumOfBestDisplay = "—";

    public SpeedrunViewModel(SpeedrunService speedrunService)
    {
        _speedrunService = speedrunService;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        _timer.Tick += OnTimerTick;

        Load();
    }

    /// <summary>
    /// Loads speedrun categories.
    /// </summary>
    public void Load()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = _speedrunService.GetCategories();
            if (result.IsSuccess && result.Value != null)
            {
                Categories = result.Value;
                if (Categories.Count > 0)
                {
                    SelectedCategory = Categories[0];
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load categories";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading categories: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void StartRun(string categoryId)
    {
        ErrorMessage = null;

        try
        {
            var result = _speedrunService.StartSession(categoryId);
            if (result.IsSuccess && result.Value != null)
            {
                CurrentSession = result.Value;
                Splits = new ObservableCollection<SpeedrunSplit>(CurrentSession.Splits);
                IsRunning = true;
                _timer.Start();
                UpdateComparisonDisplay();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to start run";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error starting run: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CompleteSplit(int splitIndex)
    {
        if (!IsRunning || CurrentSession == null)
            return;

        ErrorMessage = null;

        try
        {
            var result = _speedrunService.CompleteSplit(splitIndex);
            if (result.IsSuccess && result.Value != null)
            {
                CurrentSession = result.Value;
                Splits = new ObservableCollection<SpeedrunSplit>(CurrentSession.Splits);

                // Check if all splits are completed
                if (CurrentSession.Splits.All(s => s.IsCompleted))
                {
                    EndRun();
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to complete split";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error completing split: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EndRun()
    {
        if (!IsRunning || CurrentSession == null)
            return;

        ErrorMessage = null;

        try
        {
            var result = _speedrunService.EndSession();
            if (result.IsSuccess)
            {
                _timer.Stop();
                IsRunning = false;
                CurrentSession = result.Value;
                if (CurrentSession != null)
                {
                    Splits = new ObservableCollection<SpeedrunSplit>(CurrentSession.Splits);
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to end run";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error ending run: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Reset()
    {
        _timer.Stop();
        IsRunning = false;
        CurrentSession = null;
        Splits.Clear();
        CurrentTimeDisplay = "00:00:00.00";
        PersonalBestDisplay = "—";
        SumOfBestDisplay = "—";
        ErrorMessage = null;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (CurrentSession != null && IsRunning)
        {
            var time = CurrentSession.CurrentTime;
            CurrentTimeDisplay = FormatTime(time);
        }
    }

    private void UpdateComparisonDisplay()
    {
        if (CurrentSession?.Comparison != null)
        {
            PersonalBestDisplay = FormatTime(CurrentSession.Comparison.PersonalBest);
            SumOfBestDisplay = FormatTime(CurrentSession.Comparison.SumOfBest);
        }
        else
        {
            PersonalBestDisplay = "—";
            SumOfBestDisplay = "—";
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
    }
}
