namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Represents a discovered StarRupture save session (folder with saves).
/// </summary>
public sealed class SaveSession
{
    public required string SessionName { get; init; }
    public required string SessionPath { get; init; }
    public required SaveLocation Location { get; init; }
    public required IReadOnlyList<SaveSlot> Slots { get; init; }
    public DateTime LastModified { get; init; }
    public long TotalSizeBytes { get; init; }
}

/// <summary>
/// Represents a single save slot within a session.
/// </summary>
public sealed class SaveSlot
{
    public required string SlotName { get; init; }
    public required string SaveFilePath { get; init; }
    public required string MetadataFilePath { get; init; }
    public required DateTime LastModified { get; init; }
    public required long SizeBytes { get; init; }
    public bool IsAutoSave { get; init; }
}

/// <summary>
/// Save file location type.
/// </summary>
public enum SaveLocation
{
    LocalAppData,
    SteamCloud
}
