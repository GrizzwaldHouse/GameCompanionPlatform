using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;

namespace ArcadiaTracker.App.Views;

public partial class TimeLapseView : UserControl
{
    private readonly TimeLapseViewModel _viewModel = new();

    public TimeLapseView()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void UpdateSessions(IReadOnlyList<string> sessions)
    {
        _viewModel.UpdateSessions(sessions);
    }

    public void UpdateSnapshots(IReadOnlyList<GameCompanion.Module.StarRupture.Services.SnapshotMetadata> snapshots)
    {
        _viewModel.UpdateSnapshots(snapshots);
    }
}
