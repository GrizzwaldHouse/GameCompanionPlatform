namespace GameCompanion.Engine.Entitlements.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Service for creating, validating, and managing admin tokens.
/// Provides release-safe admin access without compile-time flags.
/// </summary>
public interface IAdminTokenService
{
    /// <summary>
    /// Generates a signed admin token for the given scope and lifetime.
    /// </summary>
    AdminToken GenerateToken(string scope, TimeSpan lifetime, AdminActivationMethod method);

    /// <summary>
    /// Validates an admin token's signature, expiry, and integrity.
    /// </summary>
    Result<AdminToken> ValidateToken(AdminToken token);

    /// <summary>
    /// Persists an admin token to the token file.
    /// </summary>
    Task<Result<Unit>> SaveTokenAsync(AdminToken token, CancellationToken ct = default);

    /// <summary>
    /// Loads and validates the current admin token from disk.
    /// Returns failure if no token exists or the token is invalid/expired.
    /// </summary>
    Task<Result<AdminToken>> LoadAndValidateTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Revokes the current admin token (deletes the token file).
    /// </summary>
    Task<Result<Unit>> RevokeTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Generates a break-glass recovery challenge for the current machine.
    /// The challenge changes daily and requires the admin passphrase to solve.
    /// </summary>
    string GenerateBreakGlassChallenge();

    /// <summary>
    /// Validates a break-glass response and issues a short-lived admin token if correct.
    /// </summary>
    Result<AdminToken> ValidateBreakGlassResponse(string challenge, string response, string scope);

    /// <summary>
    /// Gets diagnostic information about the current admin state.
    /// </summary>
    Task<AdminDiagnostics> GetDiagnosticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Diagnostic information about the admin subsystem.
/// </summary>
public sealed class AdminDiagnostics
{
    public bool HasValidToken { get; init; }
    public string? TokenScope { get; init; }
    public DateTimeOffset? TokenExpiresAt { get; init; }
    public AdminActivationMethod? ActivationMethod { get; init; }
    public int ActiveCapabilityCount { get; init; }
    public int TotalAuditEntries { get; init; }
    public DateTimeOffset? LastAdminAction { get; init; }
    public bool StoreIntegrityOk { get; init; }
    public long StoreSizeBytes { get; init; }
    public string MachineFingerprint { get; init; } = "";
}
