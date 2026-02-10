namespace ArcadiaTracker.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using ArcadiaTracker.App.Controls;

/// <summary>
/// ViewModel for the native map view.
/// </summary>
public sealed partial class NativeMapViewModel : ObservableObject
{
    private readonly MapDataService _mapDataService;

    [ObservableProperty]
    private MapData? _mapData;

    [ObservableProperty]
    private BaseCluster? _selectedBase;

    [ObservableProperty]
    private MapLayerFlags _visibleLayers = MapLayerFlags.All;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Waiting for save data...";

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _showLayerGrid = true;

    [ObservableProperty]
    private bool _showLayerBases = true;

    [ObservableProperty]
    private bool _showLayerConnections = true;

    [ObservableProperty]
    private bool _showLayerPlayer = true;

    [ObservableProperty]
    private bool _showLayerLabels = true;

    public NativeMapViewModel(MapDataService mapDataService)
    {
        _mapDataService = mapDataService;
    }

    public void LoadFromSave(StarRuptureSave save)
    {
        IsLoading = true;
        StatusMessage = "Building map...";

        var result = _mapDataService.BuildMapData(save);

        if (result.IsSuccess && result.Value != null)
        {
            MapData = result.Value;
            HasData = true;
            StatusMessage = $"{result.Value.Bases.Count} bases | {result.Value.TotalBuildingCount} buildings | {result.Value.TotalPowerGrids} power grids";
        }
        else
        {
            HasData = false;
            StatusMessage = result.Error ?? "Failed to build map data";
        }

        IsLoading = false;
    }

    partial void OnShowLayerGridChanged(bool value) => UpdateVisibleLayers();
    partial void OnShowLayerBasesChanged(bool value) => UpdateVisibleLayers();
    partial void OnShowLayerConnectionsChanged(bool value) => UpdateVisibleLayers();
    partial void OnShowLayerPlayerChanged(bool value) => UpdateVisibleLayers();
    partial void OnShowLayerLabelsChanged(bool value) => UpdateVisibleLayers();

    private void UpdateVisibleLayers()
    {
        var flags = MapLayerFlags.None;
        if (ShowLayerGrid) flags |= MapLayerFlags.Grid;
        if (ShowLayerBases) flags |= MapLayerFlags.Bases;
        if (ShowLayerConnections) flags |= MapLayerFlags.Connections;
        if (ShowLayerPlayer) flags |= MapLayerFlags.Player;
        if (ShowLayerLabels) flags |= MapLayerFlags.Labels;
        VisibleLayers = flags;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedBase = null;
    }
}
