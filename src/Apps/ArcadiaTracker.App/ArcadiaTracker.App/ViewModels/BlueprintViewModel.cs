namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for the Blueprint Library view.
/// </summary>
public sealed partial class BlueprintViewModel : ObservableObject
{
    private readonly BlueprintService _blueprintService;

    [ObservableProperty]
    private BlueprintLibrary? _library;

    [ObservableProperty]
    private ObservableCollection<Blueprint> _blueprints = [];

    [ObservableProperty]
    private ObservableCollection<string> _categories = [];

    [ObservableProperty]
    private Blueprint? _selectedBlueprint;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _totalCountDisplay = "0";

    [ObservableProperty]
    private string? _selectedCategory;

    public BlueprintViewModel(BlueprintService blueprintService)
    {
        _blueprintService = blueprintService;
    }

    /// <summary>
    /// Loads the blueprint library.
    /// </summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _blueprintService.GetLibraryAsync();

            if (result.IsSuccess)
            {
                Library = result.Value;
                Blueprints = new ObservableCollection<Blueprint>(result.Value!.Blueprints);
                Categories = new ObservableCollection<string>(result.Value.Categories);
                TotalCountDisplay = result.Value.TotalCount.ToString();
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load blueprints: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectBlueprint(Blueprint? blueprint)
    {
        SelectedBlueprint = blueprint;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ExportBlueprintAsync(Blueprint? blueprint)
    {
        if (blueprint == null)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _blueprintService.SaveBlueprintsAsync();

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to export blueprint: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void FilterByCategory(string? category)
    {
        SelectedCategory = category;

        if (Library == null)
            return;

        if (string.IsNullOrEmpty(category))
        {
            Blueprints = new ObservableCollection<Blueprint>(Library.Blueprints);
        }
        else
        {
            var filtered = Library.Blueprints
                .Where(b => b.Category == category)
                .ToList();
            Blueprints = new ObservableCollection<Blueprint>(filtered);
        }
    }
}
