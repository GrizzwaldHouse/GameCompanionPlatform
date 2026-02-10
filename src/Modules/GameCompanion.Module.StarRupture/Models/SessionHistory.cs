namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// History of tracked gameplay sessions for a save.
/// </summary>
public sealed class SessionHistory
{
    public required string SessionName { get; init; }
    public required IReadOnlyList<SessionSnapshot> Snapshots { get; init; }
    public required TimeSpan TotalTrackedTime { get; init; }
}

/// <summary>
/// A snapshot of progress at a point in time.
/// </summary>
public sealed class SessionSnapshot
{
    public required DateTime Timestamp { get; init; }
    public required TimeSpan PlayTimeAtSnapshot { get; init; }
    public required ProgressionPhase Phase { get; init; }
    public required double OverallProgress { get; init; }
    public required int BlueprintsUnlocked { get; init; }
    public required int DataPoints { get; init; }
    public string PlayTimeDisplay => $"{(int)PlayTimeAtSnapshot.TotalHours}h {PlayTimeAtSnapshot.Minutes}m";
    public string ProgressDisplay => $"{OverallProgress * 100:F1}%";
}

/// <summary>
/// Event args for save file changes detected by FileSystemWatcher.
/// </summary>
public sealed class SaveChangedEventArgs : EventArgs
{
    public required string SessionName { get; init; }
    public required string SaveFilePath { get; init; }
    public required DateTime ChangeTime { get; init; }
}
