namespace GameCompanion.Engine.Entitlements.Services;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Provides admin/developer capabilities through explicit, auditable mechanisms.
///
/// Two activation paths:
/// 1. DEBUG + Environment Variables: Development convenience only, disabled in release builds.
/// 2. Admin Token: Signed, encrypted, time-bound token file. Works in ALL builds.
///
/// Admin access is:
/// - Separate from paid entitlements (uses distinct capability actions)
/// - Explicitly activated via one of two paths
/// - Time-bound (8h for DEBUG, configurable for tokens, 4h for break-glass)
/// - Fully audited
/// - Revocable by deleting the token file
///
/// Admin capabilities NEVER piggyback on paid entitlements.
/// </summary>
public sealed class AdminCapabilityProvider
{
    /// <summary>
    /// Environment variable that must be set to enable admin capability injection.
    /// Only checked in development (non-production) environments.
    /// </summary>
    private const string AdminEnvVar = "ARCADIA_ADMIN_ENABLED";

    /// <summary>
    /// Environment variable for the admin scope (which game, or "*" for all).
    /// </summary>
    private const string AdminScopeEnvVar = "ARCADIA_ADMIN_SCOPE";

    private readonly IEntitlementService _entitlementService;
    private readonly LocalAuditLogger _auditLogger;
    private readonly IAdminTokenService? _adminTokenService;
    private readonly bool _isProduction;

    /// <summary>
    /// Cached token from the most recent successful validation.
    /// </summary>
    private AdminToken? _currentToken;

    public AdminCapabilityProvider(
        IEntitlementService entitlementService,
        LocalAuditLogger auditLogger,
        bool isProduction = true,
        IAdminTokenService? adminTokenService = null)
    {
        _entitlementService = entitlementService;
        _auditLogger = auditLogger;
        _isProduction = isProduction;
        _adminTokenService = adminTokenService;
    }

    /// <summary>
    /// Attempts to inject admin capabilities via available activation paths.
    ///
    /// Path priority:
    /// 1. Admin token file (works in ALL builds, including release)
    /// 2. Environment variables (DEBUG builds only)
    ///
    /// If both paths are available, the token file takes precedence.
    /// </summary>
    public async Task<Result<bool>> TryInjectAdminCapabilitiesAsync(CancellationToken ct = default)
    {
        // Path 1: Try admin token (works in all builds)
        if (_adminTokenService != null)
        {
            var tokenResult = await _adminTokenService.LoadAndValidateTokenAsync(ct);
            if (tokenResult.IsSuccess)
            {
                _currentToken = tokenResult.Value!;
                var remainingLifetime = _currentToken.ExpiresAt - DateTimeOffset.UtcNow;
                return await InjectCapabilitiesAsync(
                    _currentToken.Scope, remainingLifetime, _currentToken.Method, ct);
            }
        }

        // Path 2: Environment variables (DEBUG only)
        if (_isProduction)
            return Result<bool>.Success(false);

        var adminEnabled = Environment.GetEnvironmentVariable(AdminEnvVar);
        if (!string.Equals(adminEnabled, "true", StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Success(false);

        var scope = Environment.GetEnvironmentVariable(AdminScopeEnvVar) ?? "*";
        return await InjectCapabilitiesAsync(
            scope, TimeSpan.FromHours(8), AdminActivationMethod.DebugEnvironment, ct);
    }

    /// <summary>
    /// Activates admin via a new token and injects capabilities immediately.
    /// Used by the Admin Panel self-activation flow.
    /// </summary>
    public async Task<Result<bool>> ActivateWithTokenAsync(
        string scope, TimeSpan lifetime, CancellationToken ct = default)
    {
        if (_adminTokenService == null)
            return Result<bool>.Failure("Admin token service not available.");

        var token = _adminTokenService.GenerateToken(scope, lifetime, AdminActivationMethod.TokenFile);
        var saveResult = await _adminTokenService.SaveTokenAsync(token, ct);
        if (saveResult.IsFailure)
            return Result<bool>.Failure(saveResult.Error!);

        _currentToken = token;
        var remainingLifetime = token.ExpiresAt - DateTimeOffset.UtcNow;
        return await InjectCapabilitiesAsync(
            scope, remainingLifetime, AdminActivationMethod.TokenFile, ct);
    }

    /// <summary>
    /// Activates admin via the break-glass emergency mechanism.
    /// </summary>
    public async Task<Result<bool>> ActivateBreakGlassAsync(
        string challenge, string response, string scope, CancellationToken ct = default)
    {
        if (_adminTokenService == null)
            return Result<bool>.Failure("Admin token service not available.");

        var validateResult = _adminTokenService.ValidateBreakGlassResponse(challenge, response, scope);
        if (validateResult.IsFailure)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Action = "admin.breakglass.failed",
                CapabilityId = "",
                GameScope = scope,
                Detail = $"Break-glass authentication failed for challenge '{challenge}'.",
                Outcome = AuditOutcome.Denied
            }, ct);
            return Result<bool>.Failure(validateResult.Error!);
        }

        var token = validateResult.Value!;
        var saveResult = await _adminTokenService.SaveTokenAsync(token, ct);
        if (saveResult.IsFailure)
            return Result<bool>.Failure(saveResult.Error!);

        _currentToken = token;
        var remainingLifetime = token.ExpiresAt - DateTimeOffset.UtcNow;
        return await InjectCapabilitiesAsync(
            scope, remainingLifetime, AdminActivationMethod.BreakGlass, ct);
    }

    /// <summary>
    /// Revokes admin access: deletes the token and revokes all admin capabilities.
    /// </summary>
    public async Task<Result<Unit>> RevokeAdminAsync(string gameScope = "*", CancellationToken ct = default)
    {
        if (_adminTokenService != null)
            await _adminTokenService.RevokeTokenAsync(ct);

        // Revoke admin capabilities by finding active ones and revoking by ID
        var adminActions = new[] { CapabilityActions.AdminSaveOverride, CapabilityActions.AdminCapabilityIssue };
        foreach (var action in adminActions)
        {
            var check = await _entitlementService.CheckEntitlementAsync(action, gameScope, ct);
            if (check.IsSuccess)
                await _entitlementService.RevokeCapabilityAsync(check.Value!.Id, ct);
        }

        _currentToken = null;

        await _auditLogger.LogAsync(new AuditEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = "admin.revoke",
            CapabilityId = "",
            GameScope = gameScope,
            Detail = "Admin access revoked. Token deleted and admin capabilities revoked.",
            Outcome = AuditOutcome.Success
        }, ct);

        return Result<Unit>.Success(Unit.Value);
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

    /// <summary>
    /// Gets the current admin token (if any). Null if not activated.
    /// </summary>
    public AdminToken? CurrentToken => _currentToken;

    /// <summary>
    /// Core capability injection logic shared by all activation paths.
    /// </summary>
    private async Task<Result<bool>> InjectCapabilitiesAsync(
        string scope, TimeSpan lifetime, AdminActivationMethod method, CancellationToken ct)
    {
        // Grant admin-specific capabilities
        var overrideResult = await _entitlementService.GrantCapabilityAsync(
            CapabilityActions.AdminSaveOverride, scope, lifetime, ct);

        if (overrideResult.IsFailure)
            return Result<bool>.Failure(overrideResult.Error!);

        var issueResult = await _entitlementService.GrantCapabilityAsync(
            CapabilityActions.AdminCapabilityIssue, scope, lifetime, ct);

        if (issueResult.IsFailure)
            return Result<bool>.Failure(issueResult.Error!);

        // Grant all paid capabilities for admin testing and management
        foreach (var action in CapabilityActions.GetAllPaidActions())
        {
            await _entitlementService.GrantCapabilityAsync(action, scope, lifetime, ct);
        }

        var capCount = CapabilityActions.GetAllPaidActions().Count + 2;
        await _auditLogger.LogAsync(new AuditEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = "admin.inject",
            CapabilityId = CapabilityActions.AdminCapabilityIssue,
            GameScope = scope,
            Detail = $"Admin capabilities injected via {method}. Granted {capCount} capabilities for scope '{scope}'. Lifetime={lifetime}.",
            Outcome = AuditOutcome.Success
        }, ct);

        return Result<bool>.Success(true);
    }
}
