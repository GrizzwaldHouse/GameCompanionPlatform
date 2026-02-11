namespace GameCompanion.Engine.Entitlements.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Append-only local audit logger for capability-related actions.
/// Records are stored as newline-delimited JSON for easy parsing.
/// No telemetry — purely local accountability.
/// </summary>
public sealed class LocalAuditLogger
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LocalAuditLogger(string logPath)
    {
        _logPath = logPath;
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    }

    /// <summary>
    /// Appends an audit entry to the log.
    /// </summary>
    public async Task<Result<Unit>> LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(_logPath, json + Environment.NewLine, ct);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to write audit log: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Reads all audit entries from the log.
    /// </summary>
    public async Task<Result<IReadOnlyList<AuditEntry>>> ReadAllAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_logPath))
                return Result<IReadOnlyList<AuditEntry>>.Success([]);

            var lines = await File.ReadAllLinesAsync(_logPath, ct);
            var entries = new List<AuditEntry>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var entry = JsonSerializer.Deserialize<AuditEntry>(line);
                if (entry != null)
                    entries.Add(entry);
            }

            return Result<IReadOnlyList<AuditEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<AuditEntry>>.Failure($"Failed to read audit log: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }
}
