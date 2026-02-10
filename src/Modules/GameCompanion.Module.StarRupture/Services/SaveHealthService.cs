namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for analyzing save file health and managing backups.
/// </summary>
public sealed class SaveHealthService
{
    private static readonly string BackupDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "backups");

    private readonly SaveParserService _parser;

    public SaveHealthService(SaveParserService parser)
    {
        _parser = parser;
    }

    /// <summary>
    /// Analyzes the health of a save file by attempting to parse it and checking for issues.
    /// </summary>
    public async Task<Result<SaveHealthStatus>> AnalyzeHealthAsync(
        string savePath,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(savePath))
                return Result<SaveHealthStatus>.Failure($"Save file not found: {savePath}");

            var fileInfo = new FileInfo(savePath);
            var issues = new List<string>();
            var level = SaveHealthLevel.Healthy;

            // Try to parse the save
            var parseResult = await _parser.ParseSaveAsync(savePath, ct);

            if (parseResult.IsFailure)
            {
                level = SaveHealthLevel.Corrupted;
                issues.Add($"Parse failed: {parseResult.Error}");
            }
            else if (parseResult.Value != null)
            {
                var save = parseResult.Value;

                // Check for missing optional sections
                if (save.Spatial == null)
                {
                    level = SaveHealthLevel.Warning;
                    issues.Add("Missing spatial data (map/building data unavailable)");
                }
                if (save.Corporations.Corporations.Count == 0)
                {
                    level = SaveHealthLevel.Warning;
                    issues.Add("No corporation data found");
                }
                if (save.Crafting.TotalRecipeCount == 0)
                {
                    level = SaveHealthLevel.Warning;
                    issues.Add("No crafting recipe data found");
                }
                if (fileInfo.Length < 100)
                {
                    level = SaveHealthLevel.Warning;
                    issues.Add("Save file appears unusually small");
                }
            }

            // Count existing backups
            var backups = await GetBackupsInternalAsync(savePath);

            return Result<SaveHealthStatus>.Success(new SaveHealthStatus
            {
                SavePath = savePath,
                Level = level,
                Issues = issues,
                FileSizeBytes = fileInfo.Length,
                LastModified = fileInfo.LastWriteTimeUtc,
                BackupCount = backups.Count,
                LastBackupTime = backups.FirstOrDefault()?.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return Result<SaveHealthStatus>.Failure($"Error analyzing save: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a backup of the save file with optional description.
    /// </summary>
    public async Task<Result<BackupInfo>> CreateBackupAsync(
        string savePath,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(savePath))
                return Result<BackupInfo>.Failure($"Save file not found: {savePath}");

            Directory.CreateDirectory(BackupDir);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(savePath);
            var backupName = $"{fileName}_{timestamp}.sav.bak";
            var backupPath = Path.Combine(BackupDir, backupName);

            await Task.Run(() => File.Copy(savePath, backupPath, overwrite: true), ct);

            var backupInfo = new BackupInfo
            {
                BackupId = backupName,
                OriginalPath = savePath,
                BackupPath = backupPath,
                CreatedAt = DateTime.UtcNow,
                SizeBytes = new FileInfo(backupPath).Length,
                Description = description
            };

            // Save metadata
            await SaveBackupMetadataAsync(backupInfo, ct);

            return Result<BackupInfo>.Success(backupInfo);
        }
        catch (Exception ex)
        {
            return Result<BackupInfo>.Failure($"Error creating backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores a backup by copying it back to the original save location.
    /// </summary>
    public async Task<Result<Unit>> RestoreBackupAsync(
        string backupId,
        CancellationToken ct = default)
    {
        try
        {
            var metaPath = Path.Combine(BackupDir, $"{backupId}.meta.json");
            if (!File.Exists(metaPath))
                return Result<Unit>.Failure($"Backup metadata not found: {backupId}");

            var json = await File.ReadAllTextAsync(metaPath, ct);
            var meta = JsonSerializer.Deserialize<BackupMetadata>(json);
            if (meta == null)
                return Result<Unit>.Failure("Failed to read backup metadata.");

            if (!File.Exists(meta.BackupPath))
                return Result<Unit>.Failure($"Backup file not found: {meta.BackupPath}");

            await Task.Run(() => File.Copy(meta.BackupPath, meta.OriginalPath, overwrite: true), ct);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Error restoring backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all backups for a session, ordered by creation date (newest first).
    /// </summary>
    public async Task<Result<IReadOnlyList<BackupInfo>>> GetBackupsAsync(
        string sessionName,
        CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(BackupDir);
            var allBackups = await GetAllBackupsAsync(ct);
            var filtered = allBackups
                .Where(b => b.OriginalPath.Contains(sessionName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
            return Result<IReadOnlyList<BackupInfo>>.Success(filtered);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<BackupInfo>>.Failure($"Error listing backups: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<BackupInfo>> GetBackupsInternalAsync(string savePath)
    {
        try
        {
            var allBackups = await GetAllBackupsAsync(CancellationToken.None);
            return allBackups
                .Where(b => b.OriginalPath.Equals(savePath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
        }
        catch
        {
            return Array.Empty<BackupInfo>();
        }
    }

    private async Task<List<BackupInfo>> GetAllBackupsAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(BackupDir);
        var metaFiles = Directory.GetFiles(BackupDir, "*.meta.json");
        var backups = new List<BackupInfo>();

        foreach (var metaFile in metaFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(metaFile, ct);
                var meta = JsonSerializer.Deserialize<BackupMetadata>(json);
                if (meta != null)
                {
                    backups.Add(new BackupInfo
                    {
                        BackupId = meta.BackupId,
                        OriginalPath = meta.OriginalPath,
                        BackupPath = meta.BackupPath,
                        CreatedAt = meta.CreatedAt,
                        SizeBytes = File.Exists(meta.BackupPath) ? new FileInfo(meta.BackupPath).Length : 0,
                        Description = meta.Description
                    });
                }
            }
            catch { /* skip corrupted metadata */ }
        }

        return backups;
    }

    private static async Task SaveBackupMetadataAsync(BackupInfo info, CancellationToken ct)
    {
        var meta = new BackupMetadata
        {
            BackupId = info.BackupId,
            OriginalPath = info.OriginalPath,
            BackupPath = info.BackupPath,
            CreatedAt = info.CreatedAt,
            Description = info.Description
        };

        var metaPath = Path.Combine(BackupDir, $"{info.BackupId}.meta.json");
        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metaPath, json, ct);
    }

    /// <summary>
    /// Internal metadata structure for persisting backup information.
    /// </summary>
    private sealed class BackupMetadata
    {
        public string BackupId { get; set; } = "";
        public string OriginalPath { get; set; } = "";
        public string BackupPath { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
    }
}
