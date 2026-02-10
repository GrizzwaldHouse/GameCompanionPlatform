namespace GameCompanion.Engine.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Core.Interfaces;

/// <summary>
/// Base ViewModel for the application shell.
/// Manages navigation, theming, and the overall app layout.
/// </summary>
public abstract partial class ShellViewModel : ViewModelBase
{
    protected readonly IGameModule GameModule;

    [ObservableProperty]
    private string _gameTitle = string.Empty;

    [ObservableProperty]
    private string _currentSaveHealth = "Unknown";

    [ObservableProperty]
    private ObservableCollection<NavItem> _navigationItems = [];

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentPhase = string.Empty;

    [ObservableProperty]
    private string _nextAction = string.Empty;

    [ObservableProperty]
    private ObservableCollection<QuickAction> _quickActions = [];

    protected ShellViewModel(IGameModule gameModule)
    {
        GameModule = gameModule;
        GameTitle = gameModule.DisplayName;
        InitializeNavigation();
    }

    /// <summary>
    /// Override in derived classes to set up navigation items.
    /// </summary>
    protected abstract void InitializeNavigation();

    /// <summary>
    /// Override to handle navigation item selection.
    /// </summary>
    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value != null)
        {
            NavigateTo(value);
        }
    }

    /// <summary>
    /// Navigates to the view associated with a navigation item.
    /// Override in derived classes to implement specific navigation logic.
    /// </summary>
    protected abstract void NavigateTo(NavItem item);

    [RelayCommand]
    protected virtual Task CreateBackupAsync() => Task.CompletedTask;

    [RelayCommand]
    protected virtual Task RefreshAsync() => Task.CompletedTask;
}

/// <summary>
/// Represents a navigation item in the sidebar.
/// </summary>
public sealed class NavItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Icon { get; init; }
    public int Order { get; init; }
}

/// <summary>
/// Represents a quick action button in the bottom bar.
/// </summary>
public sealed class QuickAction
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required Action Execute { get; init; }
    public bool IsEnabled { get; set; } = true;
}
