using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Sessions view showing play history timeline.
/// </summary>
public partial class SessionsView : UserControl
{
    public SessionsView()
    {
        InitializeComponent();
    }

    public void UpdateHistory(SessionHistory history)
    {
        // Summary stats
        var hours = (int)history.TotalTrackedTime.TotalHours;
        var minutes = history.TotalTrackedTime.Minutes;
        TotalTimeText.Text = $"{hours}h {minutes}m";
        SessionCountText.Text = $"{history.Snapshots.Count} snapshots";

        // Progress delta
        if (history.Snapshots.Count >= 2)
        {
            var first = history.Snapshots.First();
            var last = history.Snapshots.Last();
            var delta = last.OverallProgress - first.OverallProgress;
            ProgressDeltaText.Text = delta >= 0 ? $"+{delta * 100:F1}%" : $"{delta * 100:F1}%";
        }
        else
        {
            ProgressDeltaText.Text = "—";
        }

        // Timeline (most recent first)
        var sortedSnapshots = history.Snapshots.OrderByDescending(s => s.Timestamp).ToList();
        SnapshotList.ItemsSource = sortedSnapshots;
        NoSnapshotsText.Visibility = sortedSnapshots.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
