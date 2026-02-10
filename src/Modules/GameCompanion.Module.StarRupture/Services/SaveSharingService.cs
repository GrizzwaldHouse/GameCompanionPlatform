namespace GameCompanion.Module.StarRupture.Services;

using System.IO;
using System.IO.Compression;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for exporting and importing save files as .arcadia packages.
/// </summary>
public sealed class SaveSharingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SaveParserService _saveParser;
    private readonly ProgressionAnalyzerService _progressionAnalyzer;

    public SaveSharingService(SaveParserService saveParser, ProgressionAnalyzerService progressionAnalyzer)
    {
        _saveParser = saveParser;
        _progressionAnalyzer = progressionAnalyzer;
    }

    /// <summary>
    /// Exports a save file to a .arcadia package (zip archive with metadata).
    /// </summary>
    public async Task<Result<SavePackageInfo>> ExportSaveAsync(
        string savePath,
        string outputPath,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(savePath))
                return Result<SavePackageInfo>.Failure($"Save file not found: {savePath}");

            // Parse save to extract metadata
            var parseResult = await _saveParser.ParseSaveAsync(savePath, ct);
            if (parseResult.IsFailure)
                return Result<SavePackageInfo>.Failure($"Failed to parse save: {parseResult.Error}");

            var save = parseResult.Value!;

            // Analyze progression
            var progressResult = await _progressionAnalyzer.AnalyzeAsync(savePath, ct);
            if (progressResult.IsFailure)
                return Result<SavePackageInfo>.Failure($"Failed to analyze progression: {progressResult.Error}");

            var progress = progressResult.Value!;

            // Create package metadata
            var packageInfo = new SavePackageInfo
            {
                PackageId = Guid.NewGuid().ToString(),
                SessionName = save.SessionName,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = Environment.UserName,
                PlayTime = save.PlayTime,
                CurrentPhase = progress.CurrentPhase.ToString(),
                OverallProgress = progress.OverallProgress,
                BlueprintsUnlocked = progress.BlueprintsUnlocked,
                BlueprintsTotal = progress.BlueprintsTotal,
                SaveFileSizeBytes = new FileInfo(savePath).Length,
                Description = description
            };

            // Create .arcadia package (zip file)
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using (var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                // Add save file
                archive.CreateEntryFromFile(savePath, "save.sav", CompressionLevel.Optimal);

                // Add metadata.json
                var metadataJson = JsonSerializer.Serialize(packageInfo, JsonOptions);
                var metadataEntry = archive.CreateEntry("metadata.json", CompressionLevel.Optimal);
                using (var metadataStream = metadataEntry.Open())
                using (var writer = new StreamWriter(metadataStream))
                {
                    await writer.WriteAsync(metadataJson);
                }
            }

            return Result<SavePackageInfo>.Success(packageInfo);
        }
        catch (Exception ex)
        {
            return Result<SavePackageInfo>.Failure($"Failed to export save: {ex.Message}");
        }
    }

    /// <summary>
    /// Inspects a .arcadia package and returns metadata without extracting.
    /// </summary>
    public async Task<Result<SavePackageInfo>> InspectPackageAsync(
        string packagePath,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(packagePath))
                return Result<SavePackageInfo>.Failure($"Package not found: {packagePath}");

            using var archive = ZipFile.OpenRead(packagePath);

            // Find metadata.json entry
            var metadataEntry = archive.GetEntry("metadata.json");
            if (metadataEntry == null)
                return Result<SavePackageInfo>.Failure("Invalid .arcadia package: missing metadata.json");

            // Read and deserialize metadata
            using var metadataStream = metadataEntry.Open();
            using var reader = new StreamReader(metadataStream);
            var metadataJson = await reader.ReadToEndAsync();

            var packageInfo = JsonSerializer.Deserialize<SavePackageInfo>(metadataJson, JsonOptions);
            if (packageInfo == null)
                return Result<SavePackageInfo>.Failure("Failed to deserialize package metadata");

            return Result<SavePackageInfo>.Success(packageInfo);
        }
        catch (Exception ex)
        {
            return Result<SavePackageInfo>.Failure($"Failed to inspect package: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports a .arcadia package by extracting the save file to the target directory.
    /// </summary>
    public async Task<Result<ImportResult>> ImportSaveAsync(
        string packagePath,
        string targetDirectory,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                return Result<ImportResult>.Success(new ImportResult
                {
                    Success = false,
                    Message = $"Package not found: {packagePath}",
                    PackageInfo = null,
                    RestoredToPath = null
                });
            }

            // Inspect package to get metadata
            var inspectResult = await InspectPackageAsync(packagePath, ct);
            if (inspectResult.IsFailure)
            {
                return Result<ImportResult>.Success(new ImportResult
                {
                    Success = false,
                    Message = $"Failed to inspect package: {inspectResult.Error}",
                    PackageInfo = null,
                    RestoredToPath = null
                });
            }

            var packageInfo = inspectResult.Value!;

            // Create target directory if needed
            Directory.CreateDirectory(targetDirectory);

            // Extract save file
            using var archive = ZipFile.OpenRead(packagePath);
            var saveEntry = archive.GetEntry("save.sav");
            if (saveEntry == null)
            {
                return Result<ImportResult>.Success(new ImportResult
                {
                    Success = false,
                    Message = "Invalid .arcadia package: missing save.sav",
                    PackageInfo = packageInfo,
                    RestoredToPath = null
                });
            }

            // Determine output path (use session name from metadata)
            var safeName = string.Join("_", packageInfo.SessionName.Split(Path.GetInvalidFileNameChars()));
            var outputPath = Path.Combine(targetDirectory, $"{safeName}.sav");

            // Extract save file
            saveEntry.ExtractToFile(outputPath, overwrite: true);

            return Result<ImportResult>.Success(new ImportResult
            {
                Success = true,
                Message = $"Save imported successfully to {outputPath}",
                PackageInfo = packageInfo,
                RestoredToPath = outputPath
            });
        }
        catch (Exception ex)
        {
            return Result<ImportResult>.Success(new ImportResult
            {
                Success = false,
                Message = $"Failed to import save: {ex.Message}",
                PackageInfo = null,
                RestoredToPath = null
            });
        }
    }
}
