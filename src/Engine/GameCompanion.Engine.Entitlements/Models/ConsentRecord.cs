namespace GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Records that a user has acknowledged and accepted the legal consent
/// screen for save modification. Stored locally (never transmitted).
/// </summary>
public sealed class ConsentRecord
{
    /// <summary>
    /// The game scope for which consent was given.
    /// </summary>
    public required string GameScope { get; init; }

    /// <summary>
    /// Version of the consent text that was accepted.
    /// If the consent text changes, users must re-consent.
    /// </summary>
    public required int ConsentVersion { get; init; }

    /// <summary>
    /// When consent was recorded.
    /// </summary>
    public required DateTimeOffset AcceptedAt { get; init; }

    /// <summary>
    /// SHA-256 hash of the consent text that was displayed.
    /// Proves which exact text was agreed to.
    /// </summary>
    public required string ConsentTextHash { get; init; }
}
