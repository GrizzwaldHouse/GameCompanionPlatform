namespace GameCompanion.Engine.Entitlements.Tests;

using FluentAssertions;
using GameCompanion.Engine.Entitlements.Models;
using GameCompanion.Engine.Entitlements.Services;

public class ConsentServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalConsentService _service;

    public ConsentServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"consent_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _service = new LocalConsentService(Path.Combine(_tempDir, "consent.json"));
    }

    [Fact]
    public async Task HasConsent_WithNoRecords_ReturnsFalse()
    {
        var result = await _service.HasConsentAsync("star_rupture", 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task RecordAndCheck_ReturnsTrue()
    {
        var record = new ConsentRecord
        {
            GameScope = "star_rupture",
            ConsentVersion = 1,
            AcceptedAt = DateTimeOffset.UtcNow,
            ConsentTextHash = LocalConsentService.ComputeConsentHash("star_rupture")
        };

        await _service.RecordConsentAsync(record);

        var result = await _service.HasConsentAsync("star_rupture", 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task HasConsent_WrongGameScope_ReturnsFalse()
    {
        var record = new ConsentRecord
        {
            GameScope = "minecraft",
            ConsentVersion = 1,
            AcceptedAt = DateTimeOffset.UtcNow,
            ConsentTextHash = LocalConsentService.ComputeConsentHash("minecraft")
        };

        await _service.RecordConsentAsync(record);

        var result = await _service.HasConsentAsync("star_rupture", 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void GetConsentInfo_ReturnsTrustFirstCopy()
    {
        var info = _service.GetConsentInfo("star_rupture");

        info.Title.Should().Be("Before You Continue");
        info.AcceptButtonText.Should().Be("Continue");
        info.DeclineButtonText.Should().Be("Cancel");
        info.Body.Should().Contain("single-player");
        info.Body.Should().Contain("backup");
        info.Body.Should().NotContain("WARRANTY");
        info.Body.Should().NotContain("IMPORTANT NOTICE");
    }

    [Fact]
    public void GetConsentInfo_UsesNonScaryLanguage()
    {
        var info = _service.GetConsentInfo("star_rupture");

        // Should not contain fear-inducing language
        info.Body.Should().NotContain("legal");
        info.Body.Should().NotContain("liable");
        info.Body.Should().NotContain("terminate");
        info.Body.Should().NotContain("penalty");
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
