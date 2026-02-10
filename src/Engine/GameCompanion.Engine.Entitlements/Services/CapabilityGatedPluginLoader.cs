namespace GameCompanion.Engine.Entitlements.Services;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Interfaces;

/// <summary>
/// Loads plugin modules only when the required capability is present and valid.
/// This is the non-discoverability enforcement point: if no capability exists,
/// the plugin code is never loaded, its types never resolved, and its UI
/// elements never registered. No error messages, no hints, no dead code paths.
/// </summary>
public sealed class CapabilityGatedPluginLoader
{
    private readonly IEntitlementService _entitlementService;

    public CapabilityGatedPluginLoader(IEntitlementService entitlementService)
    {
        _entitlementService = entitlementService;
    }

    /// <summary>
    /// Attempts to load a plugin by executing the factory only if the required
    /// capability is present and valid. Returns null silently if not entitled —
    /// callers must not log, display errors, or otherwise reveal the feature's existence.
    /// </summary>
    public async Task<T?> TryLoadAsync<T>(
        string requiredAction,
        string gameScope,
        Func<T> factory,
        CancellationToken ct = default) where T : class
    {
        var result = await _entitlementService.CheckEntitlementAsync(requiredAction, gameScope, ct);
        if (result.IsFailure)
            return null;

        return factory();
    }

    /// <summary>
    /// Attempts to load a plugin asynchronously by executing the async factory
    /// only if the required capability is present and valid.
    /// </summary>
    public async Task<T?> TryLoadAsync<T>(
        string requiredAction,
        string gameScope,
        Func<Task<T>> asyncFactory,
        CancellationToken ct = default) where T : class
    {
        var result = await _entitlementService.CheckEntitlementAsync(requiredAction, gameScope, ct);
        if (result.IsFailure)
            return null;

        return await asyncFactory();
    }

    /// <summary>
    /// Checks whether a capability exists for a given action and game scope
    /// without loading anything. Used for conditional registration decisions.
    /// </summary>
    public async Task<bool> HasCapabilityAsync(
        string requiredAction,
        string gameScope,
        CancellationToken ct = default)
    {
        var result = await _entitlementService.CheckEntitlementAsync(requiredAction, gameScope, ct);
        return result.IsSuccess;
    }
}
