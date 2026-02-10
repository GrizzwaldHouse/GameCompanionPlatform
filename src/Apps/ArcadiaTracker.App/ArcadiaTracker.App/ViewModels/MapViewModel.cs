namespace ArcadiaTracker.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the interactive map view using WebView2.
/// </summary>
public sealed partial class MapViewModel : ObservableObject
{
    private const string MapUrl = "https://starrupture.tools/map";

    [ObservableProperty]
    private string _currentUrl = MapUrl;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statusMessage = "Loading map...";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    public void NavigateToMap()
    {
        CurrentUrl = MapUrl;
    }

    [RelayCommand]
    public void NavigateToItems()
    {
        CurrentUrl = "https://starrupture.tools/items";
    }

    [RelayCommand]
    public void NavigateToBuildings()
    {
        CurrentUrl = "https://starrupture.tools/buildings";
    }

    [RelayCommand]
    public void NavigateToResearch()
    {
        CurrentUrl = "https://starrupture.tools/research";
    }

    public void OnNavigationStarted()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        StatusMessage = "Loading...";
    }

    public void OnNavigationCompleted(bool success, string? errorMessage = null)
    {
        IsLoading = false;
        if (success)
        {
            StatusMessage = "Ready";
        }
        else
        {
            HasError = true;
            ErrorMessage = errorMessage ?? "Failed to load page";
            StatusMessage = "Error loading page";
        }
    }
}
