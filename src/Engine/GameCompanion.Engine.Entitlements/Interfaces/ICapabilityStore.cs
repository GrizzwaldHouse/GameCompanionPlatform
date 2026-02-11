namespace GameCompanion.Engine.Entitlements.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;

/// <summary>
/// Persistent storage for capability tokens. Implementations handle
/// serialization, encryption-at-rest, and revocation tracking.
/// </summary>
public interface ICapabilityStore
{
    /// <summary>
    /// Stores a capability token.
    /// </summary>
    Task<Result<Unit>> StoreAsync(Capability capability, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all stored capabilities for a given action and game scope.
    /// Returns empty list if no matching capabilities exist.
    /// </summary>
    Task<Result<IReadOnlyList<Capability>>> GetCapabilitiesAsync(
        string action,
        string gameScope,
        CancellationToken ct = default);

    /// <summary>
    /// Revokes a capability by its ID. Revoked capabilities will fail validation.
    /// </summary>
    Task<Result<Unit>> RevokeAsync(string capabilityId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a capability ID has been revoked.
    /// </summary>
    Task<Result<bool>> IsRevokedAsync(string capabilityId, CancellationToken ct = default);

    /// <summary>
    /// Removes all expired and revoked capabilities from storage.
    /// </summary>
    Task<Result<int>> PurgeExpiredAsync(CancellationToken ct = default);
}
