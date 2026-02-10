namespace GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// A signed, time-bound admin token that works in release builds.
/// Unlike DEBUG-only env var injection, admin tokens are:
/// - Cryptographically signed (HMAC-SHA256)
/// - Time-bound with explicit expiry
/// - Scope-limited (per game or global)
/// - Stored as a file that can be rotated or revoked
/// - Validated without compile-time flags
/// </summary>
public sealed class AdminToken
{
    /// <summary>
    /// Unique identifier for this token instance.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The game scope this token grants admin access to. Use "*" for all games.
    /// </summary>
    public required string Scope { get; init; }

    /// <summary>
    /// When this token was issued.
    /// </summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// When this token expires. Admin tokens always expire.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Random nonce to ensure each token is unique even for the same scope/time.
    /// </summary>
    public required string Nonce { get; init; }

    /// <summary>
    /// HMAC-SHA256 signature over the canonical token payload.
    /// Computed with the admin signing key (derived separately from capability keys).
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// Whether the token has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Activation method that created this token.
    /// </summary>
    public required AdminActivationMethod Method { get; init; }

    /// <summary>
    /// Canonical string for HMAC signature computation.
    /// </summary>
    internal string ToCanonicalString()
    {
        return $"{Id}|{Scope}|{IssuedAt:O}|{ExpiresAt:O}|{Nonce}|{Method}";
    }
}

/// <summary>
/// How an admin token was activated. Recorded in audit logs.
/// </summary>
public enum AdminActivationMethod
{
    /// <summary>DEBUG-mode environment variable injection (dev only).</summary>
    DebugEnvironment = 0,

    /// <summary>Signed admin token file placed in entitlements directory.</summary>
    TokenFile = 1,

    /// <summary>Break-glass emergency recovery mechanism.</summary>
    BreakGlass = 2
}
