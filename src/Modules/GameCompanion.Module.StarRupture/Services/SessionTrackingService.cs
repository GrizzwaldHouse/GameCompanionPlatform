namespace GameCompanion.Module.StarRupture.Services;

using System.IO;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Tracks gameplay sessions by watching save file changes and persisting snapshots.
/// </summary>
public sealed class SessionTrackingService : IDisposable
{
    private static readonly string SessionDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker", "sessions");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly System.Timers.Timer _debounceTimer;
    private string? _pendingPath;

    public event EventHandler<SaveChangedEventArgs>? SaveChanged;

    public SessionTrackingService()
    {
        _debounceTimer = new System.Timers.Timer(1500) { AutoReset = false };
        _debounceTimer.Elapsed += OnDebounceElapsed;
    }

    /// <summary>
    /// Starts watching the specified save directories for changes.
    /// </summary>
    public void StartWatching(IReadOnlyList<string> saveDirectories)
    {
        StopWatching();
        foreach (var dir in saveDirectories)
        {
            if (!Directory.Exists(dir)) continue;
            var watcher = new FileSystemWatcher(dir, "*.sav")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            watcher.Changed += OnSaveFileChanged;
            watcher.Created += OnSaveFileChanged;
            _watchers.Add(watcher);
        }
    }

    /// <summary>
    /// Stops watching all save directories.
    /// </summary>
    public void StopWatching()
    {
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
    }

    /// <summary>
    /// Retrieves session history for the specified session name.
    /// </summary>
    public async Task<Result<SessionHistory>> GetHistoryAsync(string sessionName, CancellationToken ct = default)
    {
        try
        {
            var path = GetHistoryFilePath(sessionName);
            if (!File.Exists(path))
            {
                return Result<SessionHistory>.Success(new SessionHistory
                {
                    SessionName = sessionName,
                    Snapshots = [],
                    TotalTrackedTime = TimeSpan.Zero
                });
            }

            var json = await File.ReadAllTextAsync(path, ct);
            var data = JsonSerializer.Deserialize<SessionHistoryData>(json, JsonOptions);
            if (data == null)
                return Result<SessionHistory>.Failure("Failed to deserialize session history.");

            var snapshots = data.Snapshots.Select(s => new SessionSnapshot
            {
                Timestamp = s.Timestamp,
                PlayTimeAtSnapshot = TimeSpan.FromSeconds(s.PlayTimeSeconds),
                Phase = Enum.TryParse<ProgressionPhase>(s.Phase, out var p) ? p : ProgressionPhase.EarlyGame,
                OverallProgress = s.OverallProgress,
                BlueprintsUnlocked = s.BlueprintsUnlocked,
                DataPoints = s.DataPoints
            }).ToList();

            var totalTime = snapshots.Count >= 2
                ? snapshots.Last().PlayTimeAtSnapshot - snapshots.First().PlayTimeAtSnapshot
                : TimeSpan.Zero;

            return Result<SessionHistory>.Success(new SessionHistory
            {
                SessionName = sessionName,
                Snapshots = snapshots,
                TotalTrackedTime = totalTime
            });
        }
        catch (Exception ex)
        {
            return Result<SessionHistory>.Failure($"Error reading session history: {ex.Message}");
        }
    }

    /// <summary>
    /// Records a progress snapshot for the specified session.
    /// </summary>
    public async Task<Result<Unit>> RecordSnapshotAsync(string sessionName, PlayerProgress progress, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(SessionDir);
            var path = GetHistoryFilePath(sessionName);

            SessionHistoryData data;
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path, ct);
                data = JsonSerializer.Deserialize<SessionHistoryData>(json, JsonOptions) ?? new SessionHistoryData();
            }
            else
            {
                data = new SessionHistoryData();
            }

            data.Snapshots.Add(new SnapshotData
            {
                Timestamp = DateTime.UtcNow,
                PlayTimeSeconds = progress.TotalPlayTime.TotalSeconds,
                Phase = progress.CurrentPhase.ToString(),
                OverallProgress = progress.OverallProgress,
                BlueprintsUnlocked = progress.BlueprintsUnlocked,
                DataPoints = progress.DataPointsEarned
            });

            var output = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(path, output, ct);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Error recording snapshot: {ex.Message}");
        }
    }

    private void OnSaveFileChanged(object sender, FileSystemEventArgs e)
    {
        _pendingPath = e.FullPath;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_pendingPath == null) return;
        var path = _pendingPath;
        _pendingPath = null;

        var sessionName = Path.GetFileName(Path.GetDirectoryName(path)) ?? "Unknown";
        SaveChanged?.Invoke(this, new SaveChangedEventArgs
        {
            SessionName = sessionName,
            SaveFilePath = path,
            ChangeTime = DateTime.UtcNow
        });
    }

    private static string GetHistoryFilePath(string sessionName)
    {
        var safeName = string.Join("_", sessionName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(SessionDir, $"{safeName}_history.json");
    }

    public void Dispose()
    {
        StopWatching();
        _debounceTimer.Dispose();
    }

    // Internal serialization models
    private sealed class SessionHistoryData
    {
        public List<SnapshotData> Snapshots { get; set; } = [];
    }

    private sealed class SnapshotData
    {
        public DateTime Timestamp { get; set; }
        public double PlayTimeSeconds { get; set; }
        public string Phase { get; set; } = "";
        public double OverallProgress { get; set; }
        public int BlueprintsUnlocked { get; set; }
        public int DataPoints { get; set; }
    }
}
