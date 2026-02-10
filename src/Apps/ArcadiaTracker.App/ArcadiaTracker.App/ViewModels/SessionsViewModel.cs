namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Sessions view showing play history timeline.
/// </summary>
public sealed partial class SessionsViewModel : ObservableObject
{
    [ObservableProperty]
    private SessionHistory? _history;

    [ObservableProperty]
    private ObservableCollection<SessionSnapshot> _snapshots = [];

    [ObservableProperty]
    private string _totalTrackedTimeDisplay = "0h 0m";

    [ObservableProperty]
    private string _sessionCountDisplay = "0 snapshots";

    [ObservableProperty]
    private string _progressDeltaDisplay = "—";

    public void UpdateHistory(SessionHistory history)
    {
        History = history;
        Snapshots = new ObservableCollection<SessionSnapshot>(history.Snapshots.OrderByDescending(s => s.Timestamp));
        var hours = (int)history.TotalTrackedTime.TotalHours;
        var minutes = history.TotalTrackedTime.Minutes;
        TotalTrackedTimeDisplay = $"{hours}h {minutes}m";
        SessionCountDisplay = $"{history.Snapshots.Count} snapshots";

        if (history.Snapshots.Count >= 2)
        {
            var first = history.Snapshots.First();
            var last = history.Snapshots.Last();
            var delta = last.OverallProgress - first.OverallProgress;
            ProgressDeltaDisplay = delta >= 0 ? $"+{delta * 100:F1}%" : $"{delta * 100:F1}%";
        }
        else
        {
            ProgressDeltaDisplay = "—";
        }
    }
}
