namespace GameCompanion.Engine.Entitlements.Tests;

using FluentAssertions;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Services;

public class PluginLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly EntitlementService _service;
    private readonly CapabilityGatedPluginLoader _loader;

    public PluginLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"plugin_tests_{Guid.NewGuid():N}");
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
        _loader = new CapabilityGatedPluginLoader(_service);
    }

    [Fact]
    public async Task TryLoad_WithoutCapability_ReturnsNull()
    {
        var factoryCalled = false;
        var result = await _loader.TryLoadAsync<object>(
            CapabilityActions.SaveModify,
            "star_rupture",
            () =>
            {
                factoryCalled = true;
                return new object();
            });

        result.Should().BeNull();
        factoryCalled.Should().BeFalse("factory must not be called without capability");
    }

    [Fact]
    public async Task TryLoad_WithCapability_ReturnsInstance()
    {
        await _service.GrantCapabilityAsync(CapabilityActions.SaveModify, "star_rupture");

        var result = await _loader.TryLoadAsync<string>(
            CapabilityActions.SaveModify,
            "star_rupture",
            () => "loaded");

        result.Should().Be("loaded");
    }

    [Fact]
    public async Task HasCapability_WithoutGrant_ReturnsFalse()
    {
        var has = await _loader.HasCapabilityAsync(
            CapabilityActions.SaveModify, "star_rupture");

        has.Should().BeFalse();
    }

    [Fact]
    public async Task HasCapability_WithGrant_ReturnsTrue()
    {
        await _service.GrantCapabilityAsync(CapabilityActions.SaveModify, "star_rupture");

        var has = await _loader.HasCapabilityAsync(
            CapabilityActions.SaveModify, "star_rupture");

        has.Should().BeTrue();
    }

    [Fact]
    public async Task TryLoad_AfterRevocation_ReturnsNull()
    {
        var grant = await _service.GrantCapabilityAsync(
            CapabilityActions.SaveModify, "star_rupture");

        await _service.RevokeCapabilityAsync(grant.Value!.Id);

        var result = await _loader.TryLoadAsync<object>(
            CapabilityActions.SaveModify,
            "star_rupture",
            () => new object());

        result.Should().BeNull("revoked capability must not allow loading");
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
