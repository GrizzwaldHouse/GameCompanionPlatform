namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Notifications view showing app notification history.
/// </summary>
public sealed partial class NotificationsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AppNotification> _notifications = [];

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private bool _hasNotifications;

    [ObservableProperty]
    private string _filterSeverity = "All"; // All, Info, Success, Warning, Critical

    public void UpdateNotifications(NotificationHistory history)
    {
        var filtered = FilterSeverity switch
        {
            "Info" => history.Notifications.Where(n => n.Severity == NotificationSeverity.Info),
            "Success" => history.Notifications.Where(n => n.Severity == NotificationSeverity.Success),
            "Warning" => history.Notifications.Where(n => n.Severity == NotificationSeverity.Warning),
            "Critical" => history.Notifications.Where(n => n.Severity == NotificationSeverity.Critical),
            _ => history.Notifications.AsEnumerable()
        };

        Notifications = new ObservableCollection<AppNotification>(
            filtered.OrderByDescending(n => n.Timestamp));
        UnreadCount = history.UnreadCount;
        HasNotifications = history.Notifications.Count > 0;
    }

    public void MarkAsRead(string notificationId)
    {
        var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            UnreadCount = Notifications.Count(n => !n.IsRead);
        }
    }
}
