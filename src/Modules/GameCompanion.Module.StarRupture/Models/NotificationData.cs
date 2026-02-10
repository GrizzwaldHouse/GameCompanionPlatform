namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Types of notifications the app can emit.
/// </summary>
public enum NotificationType
{
    SaveDetected,
    ProgressMilestone,
    BadgeEarned,
    CataclysmWarning,
    CorporationLevelUp,
    PhaseTransition,
    BuildingMalfunction,
    ExportComplete
}

/// <summary>
/// Severity level for notifications.
/// </summary>
public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Critical
}

/// <summary>
/// A notification event to display to the user.
/// </summary>
public sealed class AppNotification
{
    public required string Id { get; init; }
    public required NotificationType Type { get; init; }
    public required NotificationSeverity Severity { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required DateTime Timestamp { get; init; }
    public bool IsRead { get; set; }
    public string? ActionLabel { get; init; }
    public string? ActionTarget { get; init; }

    public string Icon => Type switch
    {
        NotificationType.SaveDetected => "💾",
        NotificationType.ProgressMilestone => "🎯",
        NotificationType.BadgeEarned => "🏆",
        NotificationType.CataclysmWarning => "⚡",
        NotificationType.CorporationLevelUp => "📈",
        NotificationType.PhaseTransition => "🚀",
        NotificationType.BuildingMalfunction => "⚠️",
        NotificationType.ExportComplete => "📊",
        _ => "🔔"
    };
}

/// <summary>
/// Persistent notification history.
/// </summary>
public sealed class NotificationHistory
{
    public List<AppNotification> Notifications { get; set; } = [];

    public int UnreadCount => Notifications.Count(n => !n.IsRead);

    public DateTime? LastNotificationTime => Notifications.MaxBy(n => n.Timestamp)?.Timestamp;
}
