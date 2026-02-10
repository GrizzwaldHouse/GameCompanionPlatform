namespace GameCompanion.Module.StarRupture.Services;

using System.IO;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for managing application notifications and alert system.
/// </summary>
public sealed class NotificationService
{
    private static readonly string NotificationDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "notifications");

    private static readonly string HistoryFile = Path.Combine(NotificationDir, "history.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private NotificationHistory _history = new();

    public NotificationService()
    {
        Directory.CreateDirectory(NotificationDir);
    }

    /// <summary>
    /// Evaluates changes between previous and current progress to generate notifications.
    /// </summary>
    public List<AppNotification> EvaluateChanges(
        PlayerProgress? previous,
        PlayerProgress current,
        CataclysmState? cataclysm = null)
    {
        var notifications = new List<AppNotification>();

        // If no previous state, this is the first scan - don't generate change notifications
        if (previous == null)
        {
            notifications.Add(CreateNotification(
                NotificationType.SaveDetected,
                NotificationSeverity.Info,
                "Save Detected",
                $"Now tracking {current.SessionName}"));
            return notifications;
        }

        // Check for new badges earned
        var previousBadgeIds = previous.EarnedBadges.Select(b => b.Id).ToHashSet();
        var newBadges = current.EarnedBadges.Where(b => !previousBadgeIds.Contains(b.Id)).ToList();
        foreach (var badge in newBadges)
        {
            notifications.Add(CreateNotification(
                NotificationType.BadgeEarned,
                NotificationSeverity.Success,
                $"Badge Earned: {badge.Name}",
                badge.Description,
                "View Badges",
                "badges"));
        }

        // Check for phase transition
        if (previous.CurrentPhase != current.CurrentPhase)
        {
            notifications.Add(CreateNotification(
                NotificationType.PhaseTransition,
                NotificationSeverity.Success,
                "Phase Transition",
                $"Progressed from {previous.CurrentPhase} to {current.CurrentPhase}!"));
        }

        // Check for blueprint milestones
        CheckBlueprintMilestones(previous, current, notifications);

        // Check for playtime milestones
        CheckPlaytimeMilestones(previous, current, notifications);

        // Check for corporation level-ups
        foreach (var corp in current.Corporations)
        {
            var prevCorp = previous.Corporations.FirstOrDefault(c => c.Name == corp.Name);
            if (prevCorp != null && corp.CurrentLevel > prevCorp.CurrentLevel)
            {
                notifications.Add(CreateNotification(
                    NotificationType.CorporationLevelUp,
                    NotificationSeverity.Success,
                    "Corporation Level Up",
                    $"{corp.DisplayName} reached Level {corp.CurrentLevel}!"));
            }
        }

        // Check for cataclysm warnings
        if (cataclysm != null)
        {
            if (cataclysm.Urgency == CataclysmUrgency.Warning)
            {
                notifications.Add(CreateNotification(
                    NotificationType.CataclysmWarning,
                    NotificationSeverity.Warning,
                    "Cataclysm Warning",
                    $"Only {cataclysm.TimeRemainingDisplay} until {cataclysm.CurrentWave} arrives!"));
            }
            else if (cataclysm.Urgency == CataclysmUrgency.Critical)
            {
                notifications.Add(CreateNotification(
                    NotificationType.CataclysmWarning,
                    NotificationSeverity.Critical,
                    "Cataclysm Imminent!",
                    $"Critical: {cataclysm.TimeRemainingDisplay} remaining for {cataclysm.CurrentWave}!"));
            }
        }

        return notifications;
    }

    /// <summary>
    /// Adds a notification to the in-memory history.
    /// </summary>
    public void AddNotification(AppNotification notification)
    {
        _history.Notifications.Add(notification);
    }

    /// <summary>
    /// Gets recent notifications (most recent first).
    /// </summary>
    public NotificationHistory GetNotifications(int count = 50)
    {
        var recentNotifications = _history.Notifications
            .OrderByDescending(n => n.Timestamp)
            .Take(count)
            .ToList();

        return new NotificationHistory
        {
            Notifications = recentNotifications
        };
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    public void MarkAsRead(string notificationId)
    {
        var notification = _history.Notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
        }
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    public void MarkAllAsRead()
    {
        foreach (var notification in _history.Notifications)
        {
            notification.IsRead = true;
        }
    }

    /// <summary>
    /// Clears all notifications.
    /// </summary>
    public void ClearNotifications()
    {
        _history.Notifications.Clear();
    }

    /// <summary>
    /// Persists notification history to disk.
    /// </summary>
    public async Task<Result<Unit>> SaveHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(NotificationDir);
            var json = JsonSerializer.Serialize(_history, JsonOptions);
            await File.WriteAllTextAsync(HistoryFile, json, ct);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to save notification history: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads notification history from disk.
    /// </summary>
    public async Task<Result<NotificationHistory>> LoadHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(HistoryFile))
            {
                _history = new NotificationHistory();
                return Result<NotificationHistory>.Success(_history);
            }

            var json = await File.ReadAllTextAsync(HistoryFile, ct);
            var loaded = JsonSerializer.Deserialize<NotificationHistory>(json, JsonOptions);
            if (loaded != null)
            {
                _history = loaded;
            }

            return Result<NotificationHistory>.Success(_history);
        }
        catch (Exception ex)
        {
            return Result<NotificationHistory>.Failure($"Failed to load notification history: {ex.Message}");
        }
    }

    private void CheckBlueprintMilestones(
        PlayerProgress previous,
        PlayerProgress current,
        List<AppNotification> notifications)
    {
        var milestones = new[] { 10, 25, 50, 100 };

        foreach (var milestone in milestones)
        {
            if (previous.BlueprintsUnlocked < milestone && current.BlueprintsUnlocked >= milestone)
            {
                notifications.Add(CreateNotification(
                    NotificationType.ProgressMilestone,
                    NotificationSeverity.Success,
                    "Blueprint Milestone",
                    $"Unlocked {milestone} blueprints!"));
            }
        }

        // Check for "all blueprints unlocked"
        if (previous.BlueprintsUnlocked < previous.BlueprintsTotal &&
            current.BlueprintsUnlocked >= current.BlueprintsTotal)
        {
            notifications.Add(CreateNotification(
                NotificationType.ProgressMilestone,
                NotificationSeverity.Success,
                "All Blueprints Unlocked!",
                "You've discovered every blueprint in the game!"));
        }
    }

    private void CheckPlaytimeMilestones(
        PlayerProgress previous,
        PlayerProgress current,
        List<AppNotification> notifications)
    {
        var milestones = new[] { 1, 5, 10, 25, 50, 100 };

        foreach (var milestone in milestones)
        {
            if (previous.TotalPlayTime.TotalHours < milestone && current.TotalPlayTime.TotalHours >= milestone)
            {
                notifications.Add(CreateNotification(
                    NotificationType.ProgressMilestone,
                    NotificationSeverity.Info,
                    "Playtime Milestone",
                    $"Reached {milestone} hours of playtime!"));
            }
        }
    }

    private static AppNotification CreateNotification(
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string message,
        string? actionLabel = null,
        string? actionTarget = null)
    {
        return new AppNotification
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            Timestamp = DateTime.UtcNow,
            IsRead = false,
            ActionLabel = actionLabel,
            ActionTarget = actionTarget
        };
    }
}
