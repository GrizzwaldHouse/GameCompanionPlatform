namespace GameCompanion.Engine.Entitlements.Tests;

using FluentAssertions;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Services;

public class EntitlementServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly EntitlementService _service;

    public EntitlementServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"entitlement_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var signingKey = new byte[32];
        for (var i = 0; i < 32; i++)
            signingKey[i] = (byte)(i + 1);

        var encryptionKey = new byte[32];
        for (var i = 0; i < 32; i++)
            encryptionKey[i] = (byte)(i + 100);

        var validator = new CapabilityValidator(signingKey);
        var issuer = new CapabilityIssuer(validator);
        var store = new LocalCapabilityStore(
            Path.Combine(_tempDir, "caps.dat"),
            encryptionKey);

        _service = new EntitlementService(validator, issuer, store);
    }

    [Fact]
    public async Task CheckEntitlement_WithNoCapabilities_ReturnsFailure()
    {
        var result = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GrantAndCheck_WithValidCapability_ReturnsSuccess()
    {
        await _service.GrantCapabilityAsync(
            CapabilityActions.SaveModify, "star_rupture");

        var result = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Action.Should().Be(CapabilityActions.SaveModify);
    }

    [Fact]
    public async Task GrantAndRevoke_PreventsAccess()
    {
        var grantResult = await _service.GrantCapabilityAsync(
            CapabilityActions.SaveModify, "star_rupture");

        grantResult.IsSuccess.Should().BeTrue();

        await _service.RevokeCapabilityAsync(grantResult.Value!.Id);

        var checkResult = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");

        checkResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CheckEntitlement_WrongAction_ReturnsFailure()
    {
        await _service.GrantCapabilityAsync(
            CapabilityActions.SaveInspect, "star_rupture");

        var result = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CheckEntitlement_WrongGameScope_ReturnsFailure()
    {
        await _service.GrantCapabilityAsync(
            CapabilityActions.SaveModify, "minecraft");

        var result = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CheckEntitlement_WildcardScope_MatchesAnyGame()
    {
        await _service.GrantCapabilityAsync(
            CapabilityActions.SaveModify, "*");

        var result = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GrantCapability_WithLifetime_StoresExpiry()
    {
        var result = await _service.GrantCapabilityAsync(
            CapabilityActions.SaveModify, "star_rupture", TimeSpan.FromHours(24));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiresAt.Should().NotBeNull();
        result.Value!.ExpiresAt!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AdminCapability_SeparateFromPaid()
    {
        // Grant admin override
        await _service.GrantCapabilityAsync(
            CapabilityActions.AdminSaveOverride, "star_rupture");

        // Paid save.modify should still fail
        var paidCheck = await _service.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");
        paidCheck.IsFailure.Should().BeTrue();

        // Admin override should succeed
        var adminCheck = await _service.CheckEntitlementAsync(
            CapabilityActions.AdminSaveOverride, "star_rupture");
        adminCheck.IsSuccess.Should().BeTrue();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Cleanup best-effort
        }
    }
}
