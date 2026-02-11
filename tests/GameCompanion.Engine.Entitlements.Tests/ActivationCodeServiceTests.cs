namespace GameCompanion.Engine.Entitlements.Tests;

using System.Security.Cryptography;
using FluentAssertions;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;
using GameCompanion.Engine.Entitlements.Services;

public class ActivationCodeServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ActivationCodeService _service;
    private readonly IEntitlementService _entitlementService;

    public ActivationCodeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"arcadia_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Set up real entitlement infrastructure for integration testing
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

        var auditLogger = new LocalAuditLogger(Path.Combine(_tempDir, "audit.log"));

        _service = new ActivationCodeService(
            _entitlementService,
            auditLogger,
            Path.Combine(_tempDir, "redeemed.json"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* cleanup best-effort */ }
    }

    [Fact]
    public void GenerateCode_ReturnsValidFormat()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);

        code.Should().StartWith("ARCA-");
        code.Should().MatchRegex(@"^ARCA-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}$");
    }

    [Fact]
    public void GenerateCode_ProducesUniqueCodes()
    {
        var code1 = _service.GenerateCode(ActivationBundle.Pro);
        var code2 = _service.GenerateCode(ActivationBundle.Pro);

        code1.Should().NotBe(code2);
    }

    [Theory]
    [InlineData(ActivationBundle.Pro)]
    [InlineData(ActivationBundle.SaveModifier)]
    [InlineData(ActivationBundle.SaveInspector)]
    [InlineData(ActivationBundle.BackupManager)]
    [InlineData(ActivationBundle.ThemeCustomizer)]
    [InlineData(ActivationBundle.Optimizer)]
    [InlineData(ActivationBundle.Milestones)]
    [InlineData(ActivationBundle.ExportPro)]
    public void Validate_AcceptsGeneratedCode(ActivationBundle bundle)
    {
        var code = _service.GenerateCode(bundle);
        var result = _service.Validate(code);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bundle.Should().Be(bundle);
    }

    [Fact]
    public void Validate_RejectsEmptyCode()
    {
        var result = _service.Validate("");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Validate_RejectsGarbageCode()
    {
        var result = _service.Validate("ARCA-ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsTamperedCode()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);

        // Flip one character in the payload portion
        var chars = code.ToCharArray();
        var idx = 5; // First char after "ARCA-"
        chars[idx] = chars[idx] == '0' ? '1' : '0';
        var tampered = new string(chars);

        var result = _service.Validate(tampered);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsCodeWithoutDashes()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);
        var noDashes = code.Replace("-", "");

        var result = _service.Validate(noDashes);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_IsCaseInsensitive()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);
        var lower = code.ToLowerInvariant();

        var result = _service.Validate(lower);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RedeemAsync_GrantsCapabilities()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);

        var result = await _service.RedeemAsync(code, "star_rupture");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(CapabilityActions.SaveModify);
        result.Value.Should().Contain(CapabilityActions.SaveInspect);
        result.Value.Should().Contain(CapabilityActions.BackupManage);
        result.Value.Should().Contain(CapabilityActions.UiThemes);
    }

    [Fact]
    public async Task RedeemAsync_CapabilitiesAreVerifiable()
    {
        var code = _service.GenerateCode(ActivationBundle.SaveInspector);
        await _service.RedeemAsync(code, "star_rupture");

        var check = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveInspect, "star_rupture");

        check.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RedeemAsync_PreventsDoubleRedemption()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);

        var first = await _service.RedeemAsync(code, "star_rupture");
        var second = await _service.RedeemAsync(code, "star_rupture");

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Should().Contain("already been activated");
    }

    [Fact]
    public async Task IsRedeemedAsync_ReturnsFalseForNewCode()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);

        var redeemed = await _service.IsRedeemedAsync(code);

        redeemed.Should().BeFalse();
    }

    [Fact]
    public async Task IsRedeemedAsync_ReturnsTrueAfterRedemption()
    {
        var code = _service.GenerateCode(ActivationBundle.Pro);
        await _service.RedeemAsync(code, "star_rupture");

        var redeemed = await _service.IsRedeemedAsync(code);

        redeemed.Should().BeTrue();
    }

    [Fact]
    public void GetBundleActions_ProBundleIncludesAllExpectedActions()
    {
        var actions = ActivationCodeService.GetBundleActions(ActivationBundle.Pro);

        actions.Should().Contain(CapabilityActions.SaveModify);
        actions.Should().Contain(CapabilityActions.SaveInspect);
        actions.Should().Contain(CapabilityActions.BackupManage);
        actions.Should().Contain(CapabilityActions.UiThemes);
    }

    [Fact]
    public void GetBundleActions_SingleFeatureBundlesReturnOneAction()
    {
        ActivationCodeService.GetBundleActions(ActivationBundle.SaveModifier)
            .Should().Equal([CapabilityActions.SaveModify]);
        ActivationCodeService.GetBundleActions(ActivationBundle.SaveInspector)
            .Should().Equal([CapabilityActions.SaveInspect]);
        ActivationCodeService.GetBundleActions(ActivationBundle.BackupManager)
            .Should().Equal([CapabilityActions.BackupManage]);
    }

    [Fact]
    public async Task RedeemAsync_SingleFeatureDoesNotGrantOtherCapabilities()
    {
        var code = _service.GenerateCode(ActivationBundle.SaveInspector);
        await _service.RedeemAsync(code, "star_rupture");

        // Should have SaveInspect
        var inspectCheck = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveInspect, "star_rupture");
        inspectCheck.IsSuccess.Should().BeTrue();

        // Should NOT have SaveModify
        var modifyCheck = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");
        modifyCheck.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RedeemAsync_RespectsGameScope()
    {
        var code = _service.GenerateCode(ActivationBundle.SaveModifier);
        await _service.RedeemAsync(code, "star_rupture");

        // Should work for star_rupture
        var scopedCheck = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "star_rupture");
        scopedCheck.IsSuccess.Should().BeTrue();

        // Should NOT work for a different game
        var otherCheck = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveModify, "other_game");
        otherCheck.IsFailure.Should().BeTrue();
    }
}
