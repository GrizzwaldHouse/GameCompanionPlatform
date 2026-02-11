namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for the time-lapse replay feature.
/// </summary>
public sealed partial class TimeLapseViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SnapshotMetadata> _snapshots = [];

    [ObservableProperty]
    private SnapshotMetadata? _selectedSnapshot;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private int _playbackSpeed = 1;

    [ObservableProperty]
    private string _statusMessage = "Select a session to view time-lapse";

    [ObservableProperty]
    private ObservableCollection<string> _availableSessions = [];

    [ObservableProperty]
    private string? _selectedSession;

    public int TotalSnapshots => Snapshots.Count;
    public bool HasSnapshots => Snapshots.Count > 0;

    partial void OnCurrentIndexChanged(int value)
    {
        if (value >= 0 && value < Snapshots.Count)
        {
            SelectedSnapshot = Snapshots[value];
        }
    }

    partial void OnSelectedSessionChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadSnapshotsForSessionAsync(value);
        }
    }

    public void UpdateSessions(IReadOnlyList<string> sessions)
    {
        AvailableSessions = new ObservableCollection<string>(sessions);
        if (sessions.Count > 0 && SelectedSession == null)
        {
            SelectedSession = sessions[0];
        }
    }

    public void UpdateSnapshots(IReadOnlyList<SnapshotMetadata> snapshots)
    {
        Snapshots = new ObservableCollection<SnapshotMetadata>(snapshots);
        CurrentIndex = 0;
        StatusMessage = snapshots.Count > 0
            ? $"Loaded {snapshots.Count} snapshots"
            : "No snapshots available for this session";
        OnPropertyChanged(nameof(TotalSnapshots));
        OnPropertyChanged(nameof(HasSnapshots));
    }

    [RelayCommand]
    private void Play()
    {
        IsPlaying = true;
        StatusMessage = "Playing...";
    }

    [RelayCommand]
    private void Pause()
    {
        IsPlaying = false;
        StatusMessage = "Paused";
    }

    [RelayCommand]
    private void StepForward()
    {
        if (CurrentIndex < Snapshots.Count - 1)
        {
            CurrentIndex++;
        }
    }

    [RelayCommand]
    private void StepBackward()
    {
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
        }
    }

    [RelayCommand]
    private void GoToStart()
    {
        CurrentIndex = 0;
    }

    [RelayCommand]
    private void GoToEnd()
    {
        CurrentIndex = Math.Max(0, Snapshots.Count - 1);
    }

    private Task LoadSnapshotsForSessionAsync(string session)
    {
        // Will be called from code-behind with actual service
        StatusMessage = $"Loading snapshots for {session}...";
        return Task.CompletedTask;
    }
}
