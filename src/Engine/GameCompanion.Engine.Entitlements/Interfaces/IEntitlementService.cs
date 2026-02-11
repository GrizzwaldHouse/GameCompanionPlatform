namespace GameCompanion.Engine.Entitlements.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;

/// <summary>
/// High-level entitlement service that combines capability validation, storage, and revocation.
/// This is the primary interface consumers use to check if an action is authorized.
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Checks whether the user has a valid, non-expired, non-revoked capability
    /// for the given action and game scope. Returns the validated capability on success.
    /// </summary>
    Task<Result<Capability>> CheckEntitlementAsync(
        string action,
        string gameScope,
        CancellationToken ct = default);

    /// <summary>
    /// Grants a capability for the given action and game scope.
    /// The capability is signed, stored, and returned.
    /// </summary>
    Task<Result<Capability>> GrantCapabilityAsync(
        string action,
        string gameScope,
        TimeSpan? lifetime = null,
        CancellationToken ct = default);

    /// <summary>
    /// Revokes a specific capability by ID.
    /// </summary>
    Task<Result<Unit>> RevokeCapabilityAsync(string capabilityId, CancellationToken ct = default);
}
