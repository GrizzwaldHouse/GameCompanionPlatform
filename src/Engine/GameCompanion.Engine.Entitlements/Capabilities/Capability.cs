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
/// </summary>
public static class CapabilityActions
{
    public const string SaveModify = "save.modify";
    public const string SaveInspect = "save.inspect";
    public const string AdminSaveOverride = "admin.save.override";
    public const string AdminCapabilityIssue = "admin.capability.issue";
}
