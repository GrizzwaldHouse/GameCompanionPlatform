namespace GameCompanion.Engine.Entitlements.Capabilities;

/// <summary>
/// An opaque, signed capability token granting permission to perform a specific action
/// scoped to a game and action type. Capabilities are non-guessable and validated
/// at execution time via HMAC signature verification.
/// </summary>
public sealed class Capability
{
    /// <summary>
    /// Unique identifier for this capability instance.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The action this capability grants (e.g., "save.modify", "save.inspect", "admin.save.override").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// The game scope this capability applies to (e.g., "star_rupture"). Use "*" for all games.
    /// </summary>
    public required string GameScope { get; init; }

    /// <summary>
    /// When this capability was issued.
    /// </summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// When this capability expires. Null means no expiry.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// HMAC-SHA256 signature computed over the canonical capability payload.
    /// Used to detect tampering and verify authenticity.
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// Whether the capability has expired based on current time.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;

    /// <summary>
    /// Constructs the canonical string representation used for signature computation.
    /// Format: "{Id}|{Action}|{GameScope}|{IssuedAt:O}|{ExpiresAt:O or NONE}"
    /// </summary>
    internal string ToCanonicalString()
    {
        var expiry = ExpiresAt?.ToString("O") ?? "NONE";
        return $"{Id}|{Action}|{GameScope}|{IssuedAt:O}|{expiry}";
    }
}

/// <summary>
/// Well-known capability action constants. These define the permission space.
/// Actions are grouped by domain: save operations, analytics, UI, export, and admin.
/// </summary>
public static class CapabilityActions
{
    // Save operations
    public const string SaveModify = "save.modify";
    public const string SaveInspect = "save.inspect";
    public const string BackupManage = "save.backup.manage";

    // Analytics & insights
    public const string AnalyticsOptimizer = "analytics.optimizer";
    public const string AnalyticsCompare = "analytics.compare";
    public const string AnalyticsReplay = "analytics.replay";
    public const string AlertsMilestones = "alerts.milestones";

    // UI & customization
    public const string UiThemes = "ui.themes";

    // Export
    public const string ExportPro = "export.pro";

    // Bundles
    public const string ProBundle = "bundle.pro";

    // Admin
    public const string AdminSaveOverride = "admin.save.override";
    public const string AdminCapabilityIssue = "admin.capability.issue";

    /// <summary>
    /// Returns the set of capabilities included in the Pro bundle.
    /// </summary>
    public static IReadOnlyList<string> GetProBundleActions() =>
    [
        SaveModify,
        SaveInspect,
        BackupManage,
        UiThemes
    ];

    /// <summary>
    /// All known paid (non-admin) capability actions.
    /// </summary>
    public static IReadOnlyList<string> GetAllPaidActions() =>
    [
        SaveModify,
        SaveInspect,
        BackupManage,
        AnalyticsOptimizer,
        AnalyticsCompare,
        AnalyticsReplay,
        AlertsMilestones,
        UiThemes,
        ExportPro
    ];
}
