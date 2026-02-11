namespace ArcadiaTracker.App.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for the Build Planner feature to plan factory layouts.
/// </summary>
public sealed partial class BuildPlannerViewModel : ObservableObject
{
    private readonly BuildPlannerService _service;

    [ObservableProperty]
    private IReadOnlyList<BuildPlan> _plans = Array.Empty<BuildPlan>();

    [ObservableProperty]
    private BuildPlan? _selectedPlan;

    [ObservableProperty]
    private IReadOnlyList<BuildTemplate> _templates = Array.Empty<BuildTemplate>();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _newPlanName = string.Empty;

    [ObservableProperty]
    private string _newPlanDescription = string.Empty;

    public BuildPlannerViewModel(BuildPlannerService service)
    {
        _service = service;
    }

    /// <summary>
    /// Loads all plans and templates asynchronously.
    /// </summary>
    public async Task LoadAsync()
    {
        await LoadPlansCommand.ExecuteAsync(null);
        LoadTemplatesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadPlans()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _service.GetPlansAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Plans = result.Value;
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load plans: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void LoadTemplates()
    {
        var result = _service.GetTemplates();
        if (result.IsSuccess && result.Value != null)
        {
            Templates = result.Value;
        }
        else
        {
            ErrorMessage = result.Error;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreatePlan))]
    private async Task CreatePlan()
    {
        if (string.IsNullOrWhiteSpace(NewPlanName))
            return;

        var result = _service.CreatePlan(NewPlanName.Trim(), NewPlanDescription.Trim());
        if (result.IsSuccess)
        {
            await LoadPlansCommand.ExecuteAsync(null);
            SelectedPlan = result.Value;
            NewPlanName = string.Empty;
            NewPlanDescription = string.Empty;
        }
        else
        {
            ErrorMessage = result.Error;
        }
    }

    private bool CanCreatePlan() => !string.IsNullOrWhiteSpace(NewPlanName);

    partial void OnNewPlanNameChanged(string value)
    {
        CreatePlanCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectPlan(BuildPlan? plan)
    {
        SelectedPlan = plan;
    }

    [RelayCommand]
    private void MarkBuildingBuilt(string buildingId)
    {
        if (SelectedPlan == null)
            return;

        var result = _service.MarkBuilt(SelectedPlan.Id, buildingId);
        if (result.IsSuccess)
        {
            SelectedPlan = result.Value;
            UpdatePlansCollection();
        }
        else
        {
            ErrorMessage = result.Error;
        }
    }

    [RelayCommand]
    private async Task SavePlans()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _service.SavePlansAsync();
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save plans: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyTemplate(BuildTemplate template)
    {
        if (SelectedPlan == null)
            return;

        // Apply template buildings to the selected plan
        foreach (var building in template.Buildings)
        {
            var result = _service.AddBuilding(
                SelectedPlan.Id,
                building.BuildingType,
                building.Position,
                building.Rotation);

            if (result.IsSuccess && result.Value != null)
            {
                SelectedPlan = result.Value;
            }
            else
            {
                ErrorMessage = result.Error;
                break;
            }
        }

        UpdatePlansCollection();
    }

    private void UpdatePlansCollection()
    {
        // Update the plans collection with the modified plan
        var plansList = Plans.ToList();
        var index = plansList.FindIndex(p => p.Id == SelectedPlan?.Id);
        if (index >= 0 && SelectedPlan != null)
        {
            plansList[index] = SelectedPlan;
            Plans = plansList;
        }
    }
}
