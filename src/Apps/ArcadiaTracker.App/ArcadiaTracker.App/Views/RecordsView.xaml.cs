using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Services;

namespace ArcadiaTracker.App.Views;

public partial class RecordsView : UserControl
{
    private readonly RecordsViewModel _viewModel = new();

    public RecordsView()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void UpdateRecords(PersonalRecords records)
    {
        _viewModel.UpdateRecords(records);
    }

    public void ShowBrokenRecords(IReadOnlyList<string> broken)
    {
        _viewModel.ShowBrokenRecords(broken);
    }
}
