namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;
using Microsoft.Win32;

/// <summary>
/// Discovers StarRupture save files in both Steam and LocalAppData locations.
/// </summary>
public sealed class SaveDiscoveryService
{
    private const string SteamAppId = "1631270";
    private const string LocalAppDataPath = @"StarRupture\Saved\SaveGames";

    /// <summary>
    /// Discovers all StarRupture save sessions across all known locations.
    /// </summary>
    public async Task<Result<IReadOnlyList<SaveSession>>> DiscoverSessionsAsync(CancellationToken ct = default)
    {
        var sessions = new List<SaveSession>();

        // Check LocalAppData
        var localPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LocalAppDataPath);

        if (Directory.Exists(localPath))
        {
            var localSessions = await ScanLocationAsync(localPath, SaveLocation.LocalAppData, ct);
            sessions.AddRange(localSessions);
        }

        // Check Steam userdata
        var steamPaths = GetSteamUserDataPaths();
        foreach (var steamPath in steamPaths)
        {
            var steamSessions = await ScanLocationAsync(steamPath, SaveLocation.SteamCloud, ct);
            sessions.AddRange(steamSessions);
        }

        // Sort by last modified (newest first)
        sessions.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));

        return Result<IReadOnlyList<SaveSession>>.Success(sessions);
    }

    /// <summary>
    /// Gets the newest save slot across all sessions.
    /// </summary>
    public async Task<Result<SaveSlot?>> GetNewestSaveAsync(CancellationToken ct = default)
    {
        var sessionsResult = await DiscoverSessionsAsync(ct);
        if (sessionsResult.IsFailure)
            return Result<SaveSlot?>.Failure(sessionsResult.Error!);

        var allSlots = sessionsResult.Value!
            .SelectMany(s => s.Slots)
            .OrderByDescending(s => s.LastModified)
            .ToList();

        return Result<SaveSlot?>.Success(allSlots.FirstOrDefault());
    }

    private async Task<IReadOnlyList<SaveSession>> ScanLocationAsync(
        string basePath,
        SaveLocation location,
        CancellationToken ct)
    {
        var sessions = new List<SaveSession>();

        if (!Directory.Exists(basePath))
            return sessions;

        var sessionDirs = Directory.GetDirectories(basePath);
        foreach (var sessionDir in sessionDirs)
        {
            ct.ThrowIfCancellationRequested();

            var sessionName = Path.GetFileName(sessionDir);
            var slots = ScanSessionSlots(sessionDir);

            if (slots.Count > 0)
            {
                sessions.Add(new SaveSession
                {
                    SessionName = sessionName,
                    SessionPath = sessionDir,
                    Location = location,
                    Slots = slots,
                    LastModified = slots.Max(s => s.LastModified),
                    TotalSizeBytes = slots.Sum(s => s.SizeBytes)
                });
            }
        }

        return sessions;
    }

    private IReadOnlyList<SaveSlot> ScanSessionSlots(string sessionPath)
    {
        var slots = new List<SaveSlot>();
        var savFiles = Directory.GetFiles(sessionPath, "*.sav");

        foreach (var savFile in savFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(savFile);
            var metFile = Path.ChangeExtension(savFile, ".met");

            if (!File.Exists(metFile))
                continue; // Both files required

            var fileInfo = new FileInfo(savFile);
            var isAutoSave = fileName.StartsWith("AutoSave", StringComparison.OrdinalIgnoreCase);

            slots.Add(new SaveSlot
            {
                SlotName = fileName,
                SaveFilePath = savFile,
                MetadataFilePath = metFile,
                LastModified = fileInfo.LastWriteTime,
                SizeBytes = fileInfo.Length,
                IsAutoSave = isAutoSave
            });
        }

        // Sort by last modified (newest first)
        slots.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
        return slots;
    }

    private IReadOnlyList<string> GetSteamUserDataPaths()
    {
        var paths = new List<string>();

        // Try to find Steam installation path from registry
        var steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath))
        {
            // Fallback to common locations
            var commonPaths = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                @"D:\Steam",
                @"E:\Steam"
            };

            steamPath = commonPaths.FirstOrDefault(Directory.Exists);
        }

        if (string.IsNullOrEmpty(steamPath))
            return paths;

        var userDataPath = Path.Combine(steamPath, "userdata");
        if (!Directory.Exists(userDataPath))
            return paths;

        // Scan all user IDs
        foreach (var userDir in Directory.GetDirectories(userDataPath))
        {
            var savePath = Path.Combine(userDir, SteamAppId, "remote", "Saved", "SaveGames");
            if (Directory.Exists(savePath))
            {
                paths.Add(savePath);
            }
        }

        return paths;
    }

    private string? GetSteamInstallPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            return key?.GetValue("SteamPath") as string;
        }
        catch
        {
            return null;
        }
    }
}
