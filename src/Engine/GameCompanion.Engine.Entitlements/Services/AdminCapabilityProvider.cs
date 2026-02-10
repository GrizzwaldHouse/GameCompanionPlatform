namespace GameCompanion.Engine.Entitlements.Services;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Provides admin/developer capabilities through explicit, auditable mechanisms.
/// Admin access is:
/// - Separate from paid entitlements (uses distinct capability actions)
/// - Disabled by default in production
/// - Only enabled via environment-gated configuration or secure local dev certificates
/// - Fully audited
///
/// Admin capabilities NEVER piggyback on paid entitlements.
/// </summary>
public sealed class AdminCapabilityProvider
{
    /// <summary>
    /// Environment variable that must be set to enable admin capability injection.
    /// Only checked in development environments.
    /// </summary>
    private const string AdminEnvVar = "ARCADIA_ADMIN_ENABLED";

    /// <summary>
    /// Environment variable for the admin scope (which game, or "*" for all).
    /// </summary>
    private const string AdminScopeEnvVar = "ARCADIA_ADMIN_SCOPE";

    private readonly IEntitlementService _entitlementService;
    private readonly LocalAuditLogger _auditLogger;
    private readonly bool _isProduction;

    public AdminCapabilityProvider(
        IEntitlementService entitlementService,
        LocalAuditLogger auditLogger,
        bool isProduction = true)
    {
        _entitlementService = entitlementService;
        _auditLogger = auditLogger;
        _isProduction = isProduction;
    }

    /// <summary>
    /// Attempts to inject admin capabilities if the environment is configured for it.
    /// Returns false if admin access is not enabled or not allowed.
    /// </summary>
    public async Task<Result<bool>> TryInjectAdminCapabilitiesAsync(CancellationToken ct = default)
    {
        // Never inject admin caps in production
        if (_isProduction)
            return Result<bool>.Success(false);

        var adminEnabled = Environment.GetEnvironmentVariable(AdminEnvVar);
        if (!string.Equals(adminEnabled, "true", StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Success(false);

        var scope = Environment.GetEnvironmentVariable(AdminScopeEnvVar) ?? "*";

        // Grant admin save override capability
        var overrideResult = await _entitlementService.GrantCapabilityAsync(
            CapabilityActions.AdminSaveOverride, scope, TimeSpan.FromHours(8), ct);

        if (overrideResult.IsFailure)
            return Result<bool>.Failure(overrideResult.Error!);

        // Grant admin capability issuance capability
        var issueResult = await _entitlementService.GrantCapabilityAsync(
            CapabilityActions.AdminCapabilityIssue, scope, TimeSpan.FromHours(8), ct);

        if (issueResult.IsFailure)
            return Result<bool>.Failure(issueResult.Error!);

        // Also grant standard save modify/inspect for testing
        await _entitlementService.GrantCapabilityAsync(
            CapabilityActions.SaveModify, scope, TimeSpan.FromHours(8), ct);
        await _entitlementService.GrantCapabilityAsync(
            CapabilityActions.SaveInspect, scope, TimeSpan.FromHours(8), ct);

        // Audit the injection
        await _auditLogger.LogAsync(new AuditEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = "admin.inject",
            CapabilityId = CapabilityActions.AdminCapabilityIssue,
            GameScope = scope,
            Detail = "Admin capabilities injected via environment configuration.",
            Outcome = AuditOutcome.Success
        }, ct);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Checks if admin override is active for a given game scope.
    /// </summary>
    public async Task<bool> HasAdminOverrideAsync(string gameScope, CancellationToken ct = default)
    {
        var result = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.AdminSaveOverride, gameScope, ct);
        return result.IsSuccess;
    }
}
