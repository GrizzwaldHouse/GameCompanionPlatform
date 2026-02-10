namespace GameCompanion.Engine.SaveSafety.Interfaces;

using GameCompanion.Core.Models;

/// <summary>
/// Service for managing save file backups.
/// Game modules provide their own implementations based on save locations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a backup of a save.
    /// </summary>
    Task<Result<BackupInfo>> CreateBackupAsync(
        string saveId,
        string? description = null,
        BackupSource source = BackupSource.Manual,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all backups for a save.
    /// </summary>
    Task<Result<IReadOnlyList<BackupInfo>>> GetBackupsAsync(
        string saveId,
        CancellationToken ct = default);

    /// <summary>
    /// Restores a save from a backup.
    /// </summary>
    Task<Result<Unit>> RestoreBackupAsync(
        string backupId,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    Task<Result<Unit>> DeleteBackupAsync(
        string backupId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets backup statistics.
    /// </summary>
    Task<Result<BackupStats>> GetStatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Information about a backup.
/// </summary>
public sealed class BackupInfo
{
    public required string Id { get; init; }
    public required string SaveId { get; init; }
    public required string SaveName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? Description { get; init; }
    public required long SizeBytes { get; init; }
    public required string BackupPath { get; init; }
    public required BackupSource Source { get; init; }
}

/// <summary>
/// Source/reason for a backup.
/// </summary>
public enum BackupSource
{
    Manual,
    AutoScheduled,
    PreRestore,
    PreEdit,
    PreSync
}

/// <summary>
/// Statistics about backups.
/// </summary>
public sealed class BackupStats
{
    public int TotalBackups { get; set; }
    public long TotalSizeBytes { get; set; }
    public int SavesWithBackups { get; set; }
    public DateTime? OldestBackup { get; set; }
    public DateTime? NewestBackup { get; set; }
}
