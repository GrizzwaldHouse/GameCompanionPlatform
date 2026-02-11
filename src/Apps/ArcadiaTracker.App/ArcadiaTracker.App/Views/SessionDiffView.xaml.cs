using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Services;

namespace ArcadiaTracker.App.Views;

public partial class SessionDiffView : UserControl
{
    private readonly SessionDiffViewModel _viewModel = new();

    public SessionDiffView()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void UpdateComparison(SaveDifference diff)
    {
        _viewModel.UpdateComparison(diff);
    }

    public void ClearComparison()
    {
        _viewModel.ClearComparison();
    }
}
