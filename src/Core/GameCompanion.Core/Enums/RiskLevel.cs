namespace GameCompanion.Core.Enums;

/// <summary>
/// Classification of save file fields by risk level for editing.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// LOW: Cosmetic values, UI preferences.
    /// Rules: Instant edit allowed, backup optional.
    /// </summary>
    Low = 0,

    /// <summary>
    /// MEDIUM: Inventory quantities, skill levels, progress flags.
    /// Rules: Mandatory backup before edit, validation required, warning shown.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// HIGH: World state, quest completion flags, spawn data.
    /// Rules: Locked by default, advanced mode required, multiple backups, explicit confirmation.
    /// </summary>
    High = 2,

    /// <summary>
    /// CRITICAL: Save headers, checksum values, encryption keys.
    /// Rules: Read-only, no editing allowed, explain why to user.
    /// </summary>
    Critical = 3
}
