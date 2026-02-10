using System.Windows;
using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;
using Microsoft.Extensions.DependencyInjection;
using ArcadiaTracker.App.ViewModels;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Native map view showing save-derived spatial data.
/// </summary>
public partial class NativeMapView : UserControl
{
    private readonly NativeMapViewModel _viewModel;

    public NativeMapView()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<NativeMapViewModel>();
        DataContext = _viewModel;

        // Wire up base selection events
        MapCanvasControl.BaseClicked += OnBaseClicked;
        MapCanvasControl.ViewChanged += OnViewChanged;

        // Update zoom text when view changes
        OnViewChanged(this, EventArgs.Empty);
    }

    public void LoadFromSave(StarRuptureSave save)
    {
        _viewModel.LoadFromSave(save);
    }

    private void OnBaseClicked(object? sender, GameCompanion.Module.StarRupture.Models.BaseCluster baseCluster)
    {
        _viewModel.SelectedBase = baseCluster;
        ShowBaseDetail(baseCluster);
    }

    private void ShowBaseDetail(BaseCluster baseCluster)
    {
        BaseDetailPanel.Visibility = Visibility.Visible;
        BaseNameText.Text = baseCluster.Name;
        BaseBuildingCount.Text = $"Buildings: {baseCluster.TotalBuildingCount}";
        BaseOperationalCount.Text = $"Operational: {baseCluster.OperationalCount}";
        BaseDisabledCount.Text = $"Disabled: {baseCluster.DisabledCount}";
        BaseMalfunctionCount.Text = $"Malfunction: {baseCluster.MalfunctionCount}";
        BaseCoreLevelText.Text = $"Core Level: {baseCluster.BaseCoreLevel}";
        MachineList.ItemsSource = baseCluster.Machines;
    }

    private void OnViewChanged(object? sender, EventArgs e)
    {
        ZoomText.Text = $"Zoom: {MapCanvasControl.ZoomLevel * 100:F0}%";
    }

    private void FitAll_Click(object sender, RoutedEventArgs e)
    {
        MapCanvasControl.FitToContent();
    }

    private void CenterPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.MapData != null)
        {
            MapCanvasControl.CenterOnPosition(_viewModel.MapData.PlayerPosition);
        }
    }

    private void CloseDetail_Click(object sender, RoutedEventArgs e)
    {
        BaseDetailPanel.Visibility = Visibility.Collapsed;
        _viewModel.SelectedBase = null;
    }
}
