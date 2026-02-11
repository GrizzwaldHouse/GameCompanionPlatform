namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;

/// <summary>
/// Service for creating and managing save file snapshots for time-lapse replay.
/// </summary>
public sealed class SnapshotService
{
    private static readonly string SnapshotsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker",
        "Snapshots");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public SnapshotService()
    {
        Directory.CreateDirectory(SnapshotsFolder);
    }

    /// <summary>
    /// Creates a snapshot of the current save file with metadata.
    /// </summary>
    public async Task<Result<SnapshotMetadata>> CreateSnapshotAsync(
        string savePath,
        string sessionName,
        TimeSpan playTime,
        string currentPhase,
        double progressPercent,
        CancellationToken ct = default)
    {
        try
        {
            var sessionFolder = Path.Combine(SnapshotsFolder, SanitizeFileName(sessionName));
            Directory.CreateDirectory(sessionFolder);

            var timestamp = DateTime.UtcNow;
            var snapshotId = $"{timestamp:yyyyMMdd_HHmmss}";
            var snapshotSavePath = Path.Combine(sessionFolder, $"{snapshotId}.sav");
            var metadataPath = Path.Combine(sessionFolder, $"{snapshotId}.json");

            // Copy save file
            await using (var source = File.OpenRead(savePath))
            await using (var dest = File.Create(snapshotSavePath))
            {
                await source.CopyToAsync(dest, ct);
            }

            // Create metadata
            var metadata = new SnapshotMetadata
            {
                Id = snapshotId,
                SessionName = sessionName,
                Timestamp = timestamp,
                PlayTime = playTime,
                Phase = currentPhase,
                ProgressPercent = progressPercent,
                SaveFilePath = snapshotSavePath
            };

            // Save metadata
            var json = JsonSerializer.Serialize(metadata, JsonOptions);
            await File.WriteAllTextAsync(metadataPath, json, ct);

            return Result<SnapshotMetadata>.Success(metadata);
        }
        catch (Exception ex)
        {
            return Result<SnapshotMetadata>.Failure($"Failed to create snapshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all snapshots for a session, ordered by timestamp.
    /// </summary>
    public async Task<Result<IReadOnlyList<SnapshotMetadata>>> GetSnapshotsAsync(
        string sessionName,
        CancellationToken ct = default)
    {
        try
        {
            var sessionFolder = Path.Combine(SnapshotsFolder, SanitizeFileName(sessionName));
            if (!Directory.Exists(sessionFolder))
            {
                return Result<IReadOnlyList<SnapshotMetadata>>.Success(Array.Empty<SnapshotMetadata>());
            }

            var snapshots = new List<SnapshotMetadata>();
            var metadataFiles = Directory.GetFiles(sessionFolder, "*.json");

            foreach (var file in metadataFiles)
            {
                ct.ThrowIfCancellationRequested();
                var json = await File.ReadAllTextAsync(file, ct);
                var metadata = JsonSerializer.Deserialize<SnapshotMetadata>(json, JsonOptions);
                if (metadata != null)
                {
                    snapshots.Add(metadata);
                }
            }

            var ordered = snapshots.OrderBy(s => s.Timestamp).ToList();
            return Result<IReadOnlyList<SnapshotMetadata>>.Success(ordered);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<SnapshotMetadata>>.Failure($"Failed to load snapshots: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up old snapshots based on retention policy.
    /// </summary>
    public async Task<Result<int>> CleanupSnapshotsAsync(
        string sessionName,
        int maxSnapshots = 100,
        CancellationToken ct = default)
    {
        try
        {
            var result = await GetSnapshotsAsync(sessionName, ct);
            if (result.IsFailure) return Result<int>.Failure(result.Error!);

            var snapshots = result.Value!.ToList();
            if (snapshots.Count <= maxSnapshots)
            {
                return Result<int>.Success(0);
            }

            var toDelete = snapshots.Take(snapshots.Count - maxSnapshots).ToList();
            foreach (var snapshot in toDelete)
            {
                ct.ThrowIfCancellationRequested();
                if (File.Exists(snapshot.SaveFilePath))
                    File.Delete(snapshot.SaveFilePath);

                var metadataPath = Path.ChangeExtension(snapshot.SaveFilePath, ".json");
                if (File.Exists(metadataPath))
                    File.Delete(metadataPath);
            }

            return Result<int>.Success(toDelete.Count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to cleanup snapshots: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all session names that have snapshots.
    /// </summary>
    public Result<IReadOnlyList<string>> GetSessionsWithSnapshots()
    {
        try
        {
            if (!Directory.Exists(SnapshotsFolder))
            {
                return Result<IReadOnlyList<string>>.Success(Array.Empty<string>());
            }

            var sessions = Directory.GetDirectories(SnapshotsFolder)
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToList();

            return Result<IReadOnlyList<string>>.Success(sessions);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"Failed to get sessions: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}

/// <summary>
/// Metadata for a save file snapshot.
/// </summary>
public sealed record SnapshotMetadata
{
    public required string Id { get; init; }
    public required string SessionName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required TimeSpan PlayTime { get; init; }
    public required string Phase { get; init; }
    public required double ProgressPercent { get; init; }
    public required string SaveFilePath { get; init; }
}
