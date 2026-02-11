namespace GameCompanion.Engine.Entitlements.Tests;

using FluentAssertions;
using GameCompanion.Engine.Entitlements.Services;

public class TamperDetectorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TamperDetector _detector;

    public TamperDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"tamper_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var auditLogger = new LocalAuditLogger(Path.Combine(_tempDir, "audit.log"));
        _detector = new TamperDetector(Path.Combine(_tempDir, "integrity.dat"), auditLogger);
    }

    [Fact]
    public async Task VerifyIntegrity_NoFile_ReturnsTrue()
    {
        var result = await _detector.VerifyIntegrityAsync(
            Path.Combine(_tempDir, "nonexistent.dat"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAndVerify_UntamperedFile_ReturnsTrue()
    {
        var filePath = Path.Combine(_tempDir, "data.dat");
        await File.WriteAllTextAsync(filePath, "original content");

        await _detector.UpdateChecksumAsync(filePath);

        var result = await _detector.VerifyIntegrityAsync(filePath);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_TamperedFile_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "data.dat");
        await File.WriteAllTextAsync(filePath, "original content");

        await _detector.UpdateChecksumAsync(filePath);

        // Tamper with the file
        await File.WriteAllTextAsync(filePath, "tampered content");

        var result = await _detector.VerifyIntegrityAsync(filePath);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse("tampered file should be detected");
    }

    [Fact]
    public async Task Verify_FileWithNoChecksum_ReturnsTrue()
    {
        var filePath = Path.Combine(_tempDir, "untracked.dat");
        await File.WriteAllTextAsync(filePath, "some content");

        // No checksum was recorded — first run scenario
        var result = await _detector.VerifyIntegrityAsync(filePath);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue("untracked files should not trigger false positives");
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
