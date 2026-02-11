namespace GameCompanion.Engine.Entitlements.Services;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;

/// <summary>
/// Orchestrates capability validation, issuance, and revocation.
/// All entitlement checks flow through this service, which enforces
/// signature verification, expiry, and revocation status.
/// </summary>
public sealed class EntitlementService : IEntitlementService
{
    private readonly CapabilityValidator _validator;
    private readonly CapabilityIssuer _issuer;
    private readonly ICapabilityStore _store;

    public EntitlementService(
        CapabilityValidator validator,
        CapabilityIssuer issuer,
        ICapabilityStore store)
    {
        _validator = validator;
        _issuer = issuer;
        _store = store;
    }

    public async Task<Result<Capability>> CheckEntitlementAsync(
        string action,
        string gameScope,
        CancellationToken ct = default)
    {
        var capsResult = await _store.GetCapabilitiesAsync(action, gameScope, ct);
        if (capsResult.IsFailure)
            return Result<Capability>.Failure(capsResult.Error!);

        var capabilities = capsResult.Value!;
        if (capabilities.Count == 0)
            return Result<Capability>.Failure("No capability found.");

        // Find the first valid, non-revoked capability
        foreach (var cap in capabilities)
        {
            // Check revocation
            var revokedResult = await _store.IsRevokedAsync(cap.Id, ct);
            if (revokedResult.IsSuccess && revokedResult.Value!)
                continue;

            // Validate signature, expiry, and scope
            var validationResult = _validator.Validate(cap, action, gameScope);
            if (validationResult.IsSuccess)
                return validationResult;
        }

        return Result<Capability>.Failure("No valid capability found.");
    }

    public async Task<Result<Capability>> GrantCapabilityAsync(
        string action,
        string gameScope,
        TimeSpan? lifetime = null,
        CancellationToken ct = default)
    {
        var capability = _issuer.Issue(action, gameScope, lifetime);

        var storeResult = await _store.StoreAsync(capability, ct);
        if (storeResult.IsFailure)
            return Result<Capability>.Failure(storeResult.Error!);

        return Result<Capability>.Success(capability);
    }

    public async Task<Result<Unit>> RevokeCapabilityAsync(
        string capabilityId,
        CancellationToken ct = default)
    {
        return await _store.RevokeAsync(capabilityId, ct);
    }
}
