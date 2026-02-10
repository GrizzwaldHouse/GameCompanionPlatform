namespace GameCompanion.Engine.Entitlements.Tests;

using System.Security.Cryptography;
using FluentAssertions;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;
using GameCompanion.Engine.Entitlements.Services;

public class AdminTokenServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AdminTokenService _service;
    private readonly IEntitlementService _entitlementService;
    private readonly AdminCapabilityProvider _adminProvider;
    private readonly LocalAuditLogger _auditLogger;

    public AdminTokenServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"arcadia_admin_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var signingKey = new byte[32];
        RandomNumberGenerator.Fill(signingKey);
        var encryptionKey = new byte[32];
        RandomNumberGenerator.Fill(encryptionKey);

        var validator = new CapabilityValidator(signingKey);
        var issuer = new CapabilityIssuer(validator);
        var store = new LocalCapabilityStore(
            Path.Combine(_tempDir, "capabilities.dat"),
            encryptionKey);

        _entitlementService = new EntitlementService(validator, issuer, store);
        _auditLogger = new LocalAuditLogger(Path.Combine(_tempDir, "audit.log"));

        var tamperDetector = new TamperDetector(
            Path.Combine(_tempDir, "integrity.dat"),
            _auditLogger);

        _service = new AdminTokenService(
            Path.Combine(_tempDir, "admin.token"),
            encryptionKey,
            _auditLogger,
            tamperDetector);

        _adminProvider = new AdminCapabilityProvider(
            _entitlementService, _auditLogger, isProduction: true, _service);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* cleanup best-effort */ }
    }

    // --- Token Generation ---

    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        var token = _service.GenerateToken("star_rupture", TimeSpan.FromHours(8), AdminActivationMethod.TokenFile);

        token.Should().NotBeNull();
        token.Id.Should().StartWith("adm-");
        token.Scope.Should().Be("star_rupture");
        token.IsExpired.Should().BeFalse();
        token.Signature.Should().NotBeNullOrEmpty();
        token.Nonce.Should().NotBeNullOrEmpty();
        token.Method.Should().Be(AdminActivationMethod.TokenFile);
    }

    [Fact]
    public void GenerateToken_EnforcesMaxLifetime()
    {
        var token = _service.GenerateToken("*", TimeSpan.FromDays(365), AdminActivationMethod.TokenFile);

        // Max lifetime is 30 days
        var lifetime = token.ExpiresAt - token.IssuedAt;
        lifetime.Should().BeLessThanOrEqualTo(TimeSpan.FromDays(30).Add(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void GenerateToken_ProducesUniqueTokens()
    {
        var tokens = Enumerable.Range(0, 10)
            .Select(_ => _service.GenerateToken("*", TimeSpan.FromHours(1), AdminActivationMethod.TokenFile))
            .ToList();

        tokens.Select(t => t.Id).Distinct().Should().HaveCount(10);
        tokens.Select(t => t.Nonce).Distinct().Should().HaveCount(10);
    }

    // --- Token Validation ---

    [Fact]
    public void ValidateToken_AcceptsValidToken()
    {
        var token = _service.GenerateToken("star_rupture", TimeSpan.FromHours(8), AdminActivationMethod.TokenFile);

        var result = _service.ValidateToken(token);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(token.Id);
    }

    [Fact]
    public void ValidateToken_RejectsTamperedSignature()
    {
        var token = _service.GenerateToken("star_rupture", TimeSpan.FromHours(8), AdminActivationMethod.TokenFile);

        // Tamper with the signature
        var tampered = new AdminToken
        {
            Id = token.Id,
            Scope = token.Scope,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt,
            Nonce = token.Nonce,
            Method = token.Method,
            Signature = "0000000000000000000000000000000000000000000000000000000000000000"
        };

        var result = _service.ValidateToken(tampered);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("signature");
    }

    [Fact]
    public void ValidateToken_RejectsTamperedScope()
    {
        var token = _service.GenerateToken("star_rupture", TimeSpan.FromHours(8), AdminActivationMethod.TokenFile);

        // Change the scope but keep original signature
        var tampered = new AdminToken
        {
            Id = token.Id,
            Scope = "*",  // Changed from star_rupture
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt,
            Nonce = token.Nonce,
            Method = token.Method,
            Signature = token.Signature
        };

        var result = _service.ValidateToken(tampered);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_RejectsExpiredToken()
    {
        var token = _service.GenerateToken("star_rupture", TimeSpan.FromHours(8), AdminActivationMethod.TokenFile);

        // Create a token that's already expired
        var expired = new AdminToken
        {
            Id = token.Id,
            Scope = token.Scope,
            IssuedAt = DateTimeOffset.UtcNow.AddHours(-10),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-2),
            Nonce = token.Nonce,
            Method = token.Method,
            Signature = token.Signature  // Invalid sig but expiry checked first
        };

        var result = _service.ValidateToken(expired);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired");
    }

    // --- Token Persistence ---

    [Fact]
    public async Task SaveAndLoad_RoundTripsCorrectly()
    {
        var token = _service.GenerateToken("star_rupture", TimeSpan.FromHours(8), AdminActivationMethod.TokenFile);

        var saveResult = await _service.SaveTokenAsync(token);
        saveResult.IsSuccess.Should().BeTrue();

        var loadResult = await _service.LoadAndValidateTokenAsync();
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value!.Id.Should().Be(token.Id);
        loadResult.Value.Scope.Should().Be(token.Scope);
        loadResult.Value.Method.Should().Be(AdminActivationMethod.TokenFile);
    }

    [Fact]
    public async Task LoadToken_ReturnsFailureWhenNoFile()
    {
        var result = await _service.LoadAndValidateTokenAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No admin token");
    }

    [Fact]
    public async Task RevokeToken_DeletesFile()
    {
        var token = _service.GenerateToken("*", TimeSpan.FromHours(1), AdminActivationMethod.TokenFile);
        await _service.SaveTokenAsync(token);

        var revokeResult = await _service.RevokeTokenAsync();
        revokeResult.IsSuccess.Should().BeTrue();

        var loadResult = await _service.LoadAndValidateTokenAsync();
        loadResult.IsFailure.Should().BeTrue();
    }

    // --- Break-Glass ---

    [Fact]
    public void BreakGlassChallenge_IsConsistentWithinDay()
    {
        var challenge1 = _service.GenerateBreakGlassChallenge();
        var challenge2 = _service.GenerateBreakGlassChallenge();

        // Same day = same challenge
        challenge1.Should().Be(challenge2);
        challenge1.Should().HaveLength(8); // 4 bytes = 8 hex chars
    }

    [Fact]
    public void BreakGlassResponse_RejectsWrongResponse()
    {
        var challenge = _service.GenerateBreakGlassChallenge();

        var result = _service.ValidateBreakGlassResponse(challenge, "DEADBEEF", "star_rupture");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid break-glass response");
    }

    [Fact]
    public void BreakGlassResponse_RejectsEmptyResponse()
    {
        var challenge = _service.GenerateBreakGlassChallenge();

        var result = _service.ValidateBreakGlassResponse(challenge, "", "star_rupture");

        // Empty string won't match the expected response
        result.IsFailure.Should().BeTrue();
    }

    // --- AdminCapabilityProvider Integration ---

    [Fact]
    public async Task Provider_InjectsCapabilitiesFromToken()
    {
        // Activate via token
        var result = await _adminProvider.ActivateWithTokenAsync("star_rupture", TimeSpan.FromHours(4));
        result.IsSuccess.Should().BeTrue();

        // Verify admin capabilities exist
        var hasAdmin = await _adminProvider.HasAdminOverrideAsync("star_rupture");
        hasAdmin.Should().BeTrue();

        // Verify paid capabilities were also injected
        var saveModify = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");
        saveModify.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Provider_TokenTakesPriorityOverEnvVars()
    {
        // Even in production mode (env vars disabled), token path works
        var result = await _adminProvider.ActivateWithTokenAsync("star_rupture", TimeSpan.FromHours(1));
        result.IsSuccess.Should().BeTrue();

        // Provider should find the token on next startup check
        var injectResult = await _adminProvider.TryInjectAdminCapabilitiesAsync();
        injectResult.IsSuccess.Should().BeTrue();
        injectResult.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Provider_ProductionMode_RejectsEnvVarsButAcceptsToken()
    {
        // Production mode: env vars won't work (set isProduction=true in constructor)
        // But tokens should work
        var tokenResult = await _adminProvider.ActivateWithTokenAsync("*", TimeSpan.FromHours(1));
        tokenResult.IsSuccess.Should().BeTrue();

        var hasAdmin = await _adminProvider.HasAdminOverrideAsync("star_rupture");
        hasAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task Provider_RevokeRemovesAccess()
    {
        await _adminProvider.ActivateWithTokenAsync("star_rupture", TimeSpan.FromHours(4));
        var hasAdmin = await _adminProvider.HasAdminOverrideAsync("star_rupture");
        hasAdmin.Should().BeTrue();

        await _adminProvider.RevokeAdminAsync("star_rupture");
        var hasAdminAfter = await _adminProvider.HasAdminOverrideAsync("star_rupture");
        hasAdminAfter.Should().BeFalse();
    }

    [Fact]
    public async Task Provider_AllPaidCapabilitiesGranted()
    {
        await _adminProvider.ActivateWithTokenAsync("star_rupture", TimeSpan.FromHours(4));

        foreach (var action in CapabilityActions.GetAllPaidActions())
        {
            var result = await _entitlementService.CheckEntitlementAsync(action, "star_rupture");
            result.IsSuccess.Should().BeTrue($"Expected capability '{action}' to be active");
        }
    }

    // --- Diagnostics ---

    [Fact]
    public async Task Diagnostics_ReflectsActiveToken()
    {
        await _adminProvider.ActivateWithTokenAsync("star_rupture", TimeSpan.FromHours(4));

        var diag = await _service.GetDiagnosticsAsync();

        diag.HasValidToken.Should().BeTrue();
        diag.TokenScope.Should().Be("star_rupture");
        diag.ActivationMethod.Should().Be(AdminActivationMethod.TokenFile);
        diag.MachineFingerprint.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Diagnostics_ShowsNoTokenWhenRevoked()
    {
        await _adminProvider.ActivateWithTokenAsync("*", TimeSpan.FromHours(1));
        await _service.RevokeTokenAsync();

        var diag = await _service.GetDiagnosticsAsync();

        diag.HasValidToken.Should().BeFalse();
    }

    // --- Audit Trail ---

    [Fact]
    public async Task AuditLog_RecordsTokenSave()
    {
        var token = _service.GenerateToken("*", TimeSpan.FromHours(1), AdminActivationMethod.TokenFile);
        await _service.SaveTokenAsync(token);

        var log = await _auditLogger.ReadAllAsync();
        log.IsSuccess.Should().BeTrue();
        log.Value!.Should().Contain(e => e.Action == "admin.token.save");
    }

    [Fact]
    public async Task AuditLog_RecordsRevocation()
    {
        var token = _service.GenerateToken("*", TimeSpan.FromHours(1), AdminActivationMethod.TokenFile);
        await _service.SaveTokenAsync(token);
        await _service.RevokeTokenAsync();

        var log = await _auditLogger.ReadAllAsync();
        log.IsSuccess.Should().BeTrue();
        log.Value!.Should().Contain(e => e.Action == "admin.token.revoke");
    }

    [Fact]
    public async Task AuditLog_RecordsCapabilityInjection()
    {
        await _adminProvider.ActivateWithTokenAsync("star_rupture", TimeSpan.FromHours(1));

        var log = await _auditLogger.ReadAllAsync();
        log.IsSuccess.Should().BeTrue();
        log.Value!.Should().Contain(e => e.Action == "admin.inject");
    }

    // --- Security: Non-Escalation ---

    [Fact]
    public async Task PaidUsers_CannotEscalateToAdmin()
    {
        // Grant only save.modify (a paid capability)
        await _entitlementService.GrantCapabilityAsync(CapabilityActions.SaveModify, "star_rupture");

        // Should NOT have admin access
        var hasAdmin = await _adminProvider.HasAdminOverrideAsync("star_rupture");
        hasAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task AdminCapabilities_AreSeparateNamespace()
    {
        // Grant all paid capabilities
        foreach (var action in CapabilityActions.GetAllPaidActions())
        {
            await _entitlementService.GrantCapabilityAsync(action, "star_rupture");
        }

        // Admin override should still be false
        var hasAdmin = await _adminProvider.HasAdminOverrideAsync("star_rupture");
        hasAdmin.Should().BeFalse();
    }
}
