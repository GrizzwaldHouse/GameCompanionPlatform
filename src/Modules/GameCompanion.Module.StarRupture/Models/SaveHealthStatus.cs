namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Health assessment of a save file.
/// </summary>
public sealed class SaveHealthStatus
{
    public required string SavePath { get; init; }
    public required SaveHealthLevel Level { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
    public required long FileSizeBytes { get; init; }
    public required DateTime LastModified { get; init; }
    public required int BackupCount { get; init; }
    public DateTime? LastBackupTime { get; init; }
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}

/// <summary>
/// Save file health level.
/// </summary>
public enum SaveHealthLevel
{
    Healthy,    // Parses cleanly, all data present
    Warning,    // Parses but missing optional sections
    Corrupted   // Failed to parse or critical data missing
}

/// <summary>
/// Info about a backup file.
/// </summary>
public sealed class BackupInfo
{
    public required string BackupId { get; init; }
    public required string OriginalPath { get; init; }
    public required string BackupPath { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long SizeBytes { get; init; }
    public string? Description { get; init; }
}
