namespace GameCompanion.Engine.Entitlements.Services;

using System.Security.Cryptography;
using System.Text;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Lightweight tamper detection for the capability store and audit log.
/// Detects:
/// - Store file modification outside the application
/// - Checksum mismatches on capability data
/// - Unexpected capability injection
///
/// Response strategy: silently disable the feature. No punishment, no phone-home.
/// </summary>
public sealed class TamperDetector
{
    private readonly string _checksumPath;
    private readonly LocalAuditLogger _auditLogger;

    public TamperDetector(string checksumPath, LocalAuditLogger auditLogger)
    {
        _checksumPath = checksumPath;
        _auditLogger = auditLogger;
        Directory.CreateDirectory(Path.GetDirectoryName(checksumPath)!);
    }

    /// <summary>
    /// Computes and stores a checksum for the given file.
    /// Called after legitimate writes to the capability store.
    /// </summary>
    public async Task<Result<Unit>> UpdateChecksumAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result<Unit>.Success(Unit.Value);

            var hash = await ComputeFileHashAsync(filePath, ct);
            var checksums = await LoadChecksumsAsync(ct);
            checksums[filePath] = hash;
            await SaveChecksumsAsync(checksums, ct);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to update checksum: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that a file has not been tampered with since the last checksum update.
    /// Returns true if the file is intact, false if tampering is detected.
    /// </summary>
    public async Task<Result<bool>> VerifyIntegrityAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result<bool>.Success(true); // No file = no tampering

            var checksums = await LoadChecksumsAsync(ct);
            if (!checksums.TryGetValue(filePath, out var expectedHash))
                return Result<bool>.Success(true); // No stored checksum = first run

            var actualHash = await ComputeFileHashAsync(filePath, ct);
            var isIntact = string.Equals(expectedHash, actualHash, StringComparison.Ordinal);

            if (!isIntact)
            {
                await _auditLogger.LogAsync(new AuditEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Action = "tamper_detection",
                    CapabilityId = "system",
                    GameScope = "*",
                    Detail = $"Integrity check failed for: {Path.GetFileName(filePath)}",
                    Outcome = AuditOutcome.TamperDetected
                }, ct);
            }

            return Result<bool>.Success(isIntact);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Integrity check failed: {ex.Message}");
        }
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<Dictionary<string, string>> LoadChecksumsAsync(CancellationToken ct)
    {
        if (!File.Exists(_checksumPath))
            return new Dictionary<string, string>();

        var lines = await File.ReadAllLinesAsync(_checksumPath, ct);
        var checksums = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOf('|');
            if (separatorIndex > 0)
            {
                var path = line[..separatorIndex];
                var hash = line[(separatorIndex + 1)..];
                checksums[path] = hash;
            }
        }

        return checksums;
    }

    private async Task SaveChecksumsAsync(Dictionary<string, string> checksums, CancellationToken ct)
    {
        var lines = checksums.Select(kv => $"{kv.Key}|{kv.Value}");
        await File.WriteAllLinesAsync(_checksumPath, lines, ct);
    }
}
