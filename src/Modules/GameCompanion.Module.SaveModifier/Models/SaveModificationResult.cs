namespace GameCompanion.Module.SaveModifier.Models;

/// <summary>
/// Result of applying modifications to a save file.
/// </summary>
public sealed class SaveModificationResult
{
    /// <summary>
    /// The save file path that was modified.
    /// </summary>
    public required string SavePath { get; init; }

    /// <summary>
    /// The backup ID created before modification for rollback.
    /// </summary>
    public required string BackupId { get; init; }

    /// <summary>
    /// Number of fields successfully modified.
    /// </summary>
    public required int ModifiedFieldCount { get; init; }

    /// <summary>
    /// Timestamp of the modification.
    /// </summary>
    public required DateTimeOffset ModifiedAt { get; init; }
}
