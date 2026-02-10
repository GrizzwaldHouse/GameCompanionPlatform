using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Notifications view showing notification center.
/// </summary>
public partial class NotificationsView : UserControl
{
    private NotificationHistory? _currentHistory;
    private NotificationSeverity? _currentFilter;

    public NotificationsView()
    {
        InitializeComponent();
    }

    public event EventHandler? MarkAllReadClicked;
    public event EventHandler? ClearClicked;
    public event EventHandler<string>? NotificationClicked;

    public void UpdateNotifications(NotificationHistory history)
    {
        _currentHistory = history;

        // Unread count
        UnreadCountText.Text = history.UnreadCount == 1 ? "1 unread" : $"{history.UnreadCount} unread";
        UnreadBadge.Visibility = history.UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Apply current filter
        ApplyFilter(_currentFilter);
    }

    private void ApplyFilter(NotificationSeverity? filter)
    {
        _currentFilter = filter;

        if (_currentHistory == null)
            return;

        var filtered = filter.HasValue
            ? _currentHistory.Notifications.Where(n => n.Severity == filter.Value)
            : _currentHistory.Notifications;

        var displayItems = filtered
            .OrderByDescending(n => n.Timestamp)
            .Select(n => new NotificationDisplayItem(n))
            .ToList();

        NotificationsList.ItemsSource = displayItems;
        NoNotificationsText.Visibility = displayItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void FilterAll_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(null);
    }

    private void FilterInfo_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(NotificationSeverity.Info);
    }

    private void FilterSuccess_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(NotificationSeverity.Success);
    }

    private void FilterWarning_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(NotificationSeverity.Warning);
    }

    private void FilterCritical_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(NotificationSeverity.Critical);
    }

    private void MarkAllRead_Click(object sender, RoutedEventArgs e)
    {
        MarkAllReadClicked?.Invoke(this, EventArgs.Empty);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ClearClicked?.Invoke(this, EventArgs.Empty);
    }

    private void Notification_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string notificationId)
        {
            NotificationClicked?.Invoke(this, notificationId);
        }
    }
}

public class NotificationDisplayItem
{
    public NotificationDisplayItem(AppNotification notification)
    {
        Id = notification.Id;
        Icon = notification.Icon;
        Title = notification.Title;
        Message = notification.Message;
        TimestampDisplay = FormatTimestamp(notification.Timestamp);
        IsUnread = !notification.IsRead;

        // Color-coded based on severity
        var (iconColor, backgroundColor) = notification.Severity switch
        {
            NotificationSeverity.Info => ("#00D4FF", "#2A2A3E"),
            NotificationSeverity.Success => ("#2ED573", "#2A2A3E"),
            NotificationSeverity.Warning => ("#FF6B35", "#2A2A3E"),
            NotificationSeverity.Critical => ("#FF4757", "#2A2A3E"),
            _ => ("#00D4FF", "#2A2A3E")
        };

        IconBackgroundBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(iconColor));

        // Unread items have a highlight background
        var bgColor = IsUnread ? "#3A3A4E" : backgroundColor;
        BackgroundBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(bgColor));
    }

    public string Id { get; }
    public string Icon { get; }
    public string Title { get; }
    public string Message { get; }
    public string TimestampDisplay { get; }
    public bool IsUnread { get; }
    public Brush IconBackgroundBrush { get; }
    public Brush BackgroundBrush { get; }

    private static string FormatTimestamp(DateTime timestamp)
    {
        var now = DateTime.Now;
        var diff = now - timestamp;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return timestamp.ToString("yyyy-MM-dd HH:mm");
    }
}
