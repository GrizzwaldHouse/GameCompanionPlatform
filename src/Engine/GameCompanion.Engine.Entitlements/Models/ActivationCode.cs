namespace GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Represents a parsed activation code with its embedded feature grant information.
/// Codes use the format ARCA-XXXX-XXXX-XXXX-XXXX where X is alphanumeric.
/// </summary>
public sealed class ActivationCode
{
    /// <summary>
    /// The raw activation code string (e.g., "ARCA-A1B2-C3D4-E5F6-7890").
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Which feature bundle this code unlocks.
    /// </summary>
    public required ActivationBundle Bundle { get; init; }

    /// <summary>
    /// Random nonce making each code unique (prevents reuse tracking by pattern).
    /// </summary>
    public required byte[] Nonce { get; init; }

    /// <summary>
    /// Truncated HMAC tag for code authenticity verification.
    /// </summary>
    public required byte[] Tag { get; init; }
}

/// <summary>
/// Predefined feature bundles that activation codes can unlock.
/// Each bundle maps to a set of capability actions.
/// </summary>
public enum ActivationBundle : byte
{
    /// <summary>
    /// Pro bundle: Save Modify + Save Inspect + Backup Manager + Themes.
    /// </summary>
    Pro = 0,

    /// <summary>
    /// Save Modifier only.
    /// </summary>
    SaveModifier = 1,

    /// <summary>
    /// Save Inspector (read-only deep analytics).
    /// </summary>
    SaveInspector = 2,

    /// <summary>
    /// Backup Manager (scheduled backups, restore).
    /// </summary>
    BackupManager = 3,

    /// <summary>
    /// Theme Customizer (additional UI themes).
    /// </summary>
    ThemeCustomizer = 4,

    /// <summary>
    /// Efficiency Optimizer (production chain analysis).
    /// </summary>
    Optimizer = 5,

    /// <summary>
    /// Milestones & Alerts (custom progress notifications).
    /// </summary>
    Milestones = 6,

    /// <summary>
    /// Export Pro (PDF, image, JSON export).
    /// </summary>
    ExportPro = 7
}
