namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// Main shell ViewModel for the StarRupture Companion app.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly SaveDiscoveryService _discovery;
    private readonly SaveParserService _parser;
    private readonly ProgressionAnalyzerService _analyzer;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statusMessage = "Loading...";

    [ObservableProperty]
    private string _selectedSession = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SaveSession> _availableSessions = [];

    [ObservableProperty]
    private PlayerProgress? _currentProgress;

    [ObservableProperty]
    private StarRuptureSave? _currentSave;

    [ObservableProperty]
    private string _selectedNavItem = "Dashboard";

    [ObservableProperty]
    private object? _currentView;

    public MainViewModel(
        SaveDiscoveryService discovery,
        SaveParserService parser,
        ProgressionAnalyzerService analyzer)
    {
        _discovery = discovery;
        _parser = parser;
        _analyzer = analyzer;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Discovering save files...";

        try
        {
            var sessionsResult = await _discovery.DiscoverSessionsAsync();
            if (sessionsResult.IsFailure)
            {
                StatusMessage = $"Error: {sessionsResult.Error}";
                IsLoading = false;
                return;
            }

            var sessions = sessionsResult.Value!;
            AvailableSessions = new ObservableCollection<SaveSession>(sessions);

            if (sessions.Count > 0)
            {
                // Select the session with the most recent save
                var newest = sessions.OrderByDescending(s => s.LastModified).First();
                SelectedSession = newest.SessionName;
                await LoadSessionAsync(newest);
            }
            else
            {
                StatusMessage = "No save files found. Start playing StarRupture!";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await InitializeAsync();
    }

    private async Task LoadSessionAsync(SaveSession session)
    {
        StatusMessage = $"Analyzing {session.SessionName}...";

        var newestSlot = session.Slots.FirstOrDefault();
        if (newestSlot == null)
        {
            StatusMessage = "No saves in this session";
            return;
        }

        // Parse save once, then use it for both progression analysis and map data
        var parseResult = await _parser.ParseSaveAsync(newestSlot.SaveFilePath);
        if (parseResult.IsFailure || parseResult.Value == null)
        {
            StatusMessage = $"Error: {parseResult.Error}";
            return;
        }

        CurrentSave = parseResult.Value;
        CurrentProgress = _analyzer.AnalyzeSave(CurrentSave);
        StatusMessage = $"Session: {session.SessionName} | {CurrentProgress.TotalPlayTime.TotalHours:F1}h played";
    }

    partial void OnSelectedSessionChanged(string value)
    {
        var session = AvailableSessions.FirstOrDefault(s => s.SessionName == value);
        if (session != null)
        {
            _ = LoadSessionAsync(session);
        }
    }
}
