using System.Windows;
using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Speedrun timer view with split tracking and comparison.
/// </summary>
public partial class SpeedrunView : UserControl
{
    private readonly SpeedrunViewModel _viewModel;

    public SpeedrunView(SpeedrunViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;

        // Subscribe to property changes to update visibility
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.IsRunning))
        {
            UpdateRunningState();
        }
        else if (e.PropertyName == nameof(_viewModel.CurrentSession))
        {
            UpdateSessionVisibility();
        }
        else if (e.PropertyName == nameof(_viewModel.ErrorMessage))
        {
            UpdateErrorVisibility();
        }
    }

    private void UpdateRunningState()
    {
        StartButton.Visibility = _viewModel.IsRunning ? Visibility.Collapsed : Visibility.Visible;
        StopButton.Visibility = _viewModel.IsRunning ? Visibility.Visible : Visibility.Collapsed;
        CategoryComboBox.IsEnabled = !_viewModel.IsRunning;
    }

    private void UpdateSessionVisibility()
    {
        var hasSession = _viewModel.CurrentSession != null;
        ComparisonPanel.Visibility = hasSession ? Visibility.Visible : Visibility.Collapsed;
        SplitsPanel.Visibility = hasSession ? Visibility.Visible : Visibility.Collapsed;
        InstructionsPanel.Visibility = hasSession ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateErrorVisibility()
    {
        ErrorPanel.Visibility = !string.IsNullOrEmpty(_viewModel.ErrorMessage) ? Visibility.Visible : Visibility.Collapsed;
    }
}
