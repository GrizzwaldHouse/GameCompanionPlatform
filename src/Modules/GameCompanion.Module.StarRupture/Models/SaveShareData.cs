namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Metadata for an exported .arcadia save package.
/// </summary>
public sealed class SavePackageInfo
{
    public required string PackageId { get; init; }
    public required string SessionName { get; init; }
    public required DateTime ExportedAt { get; init; }
    public required string ExportedBy { get; init; }
    public required TimeSpan PlayTime { get; init; }
    public required string CurrentPhase { get; init; }
    public required double OverallProgress { get; init; }
    public required int BlueprintsUnlocked { get; init; }
    public required int BlueprintsTotal { get; init; }
    public required long SaveFileSizeBytes { get; init; }
    public string? Description { get; init; }

    public string FileSizeDisplay => SaveFileSizeBytes switch
    {
        < 1024 => $"{SaveFileSizeBytes} B",
        < 1024 * 1024 => $"{SaveFileSizeBytes / 1024.0:F1} KB",
        _ => $"{SaveFileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}

/// <summary>
/// Result of importing a .arcadia save package.
/// </summary>
public sealed class ImportResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public SavePackageInfo? PackageInfo { get; init; }
    public string? RestoredToPath { get; init; }
}
