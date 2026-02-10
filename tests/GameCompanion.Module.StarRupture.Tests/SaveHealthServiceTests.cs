using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class SaveHealthServiceTests
{
    private readonly SaveHealthService _service;

    public SaveHealthServiceTests()
    {
        var parser = new SaveParserService();
        _service = new SaveHealthService(parser);
    }

    [Fact]
    public async Task AnalyzeHealth_WithNonExistentFile_ShouldReturnFailure()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sav");

        var result = await _service.AnalyzeHealthAsync(nonExistentPath);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void AnalyzeHealth_ReturnsCorrectFileSizeFormat()
    {
        // Test the FileSizeDisplay computed property on SaveHealthStatus model

        // Test bytes
        var statusBytes = new SaveHealthStatus
        {
            SavePath = "test.sav",
            Level = SaveHealthLevel.Healthy,
            Issues = [],
            FileSizeBytes = 512,
            LastModified = DateTime.UtcNow,
            BackupCount = 0,
            LastBackupTime = null
        };
        statusBytes.FileSizeDisplay.Should().Be("512 B");

        // Test kilobytes
        var statusKB = new SaveHealthStatus
        {
            SavePath = "test.sav",
            Level = SaveHealthLevel.Healthy,
            Issues = [],
            FileSizeBytes = 2048,
            LastModified = DateTime.UtcNow,
            BackupCount = 0,
            LastBackupTime = null
        };
        statusKB.FileSizeDisplay.Should().Contain("KB");
        statusKB.FileSizeDisplay.Should().Contain("2.0");

        // Test megabytes
        var statusMB = new SaveHealthStatus
        {
            SavePath = "test.sav",
            Level = SaveHealthLevel.Healthy,
            Issues = [],
            FileSizeBytes = 5242880, // 5 MB
            LastModified = DateTime.UtcNow,
            BackupCount = 0,
            LastBackupTime = null
        };
        statusMB.FileSizeDisplay.Should().Contain("MB");
        statusMB.FileSizeDisplay.Should().Contain("5.0");
    }
}
