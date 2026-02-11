namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for tracking personal best records across all sessions.
/// </summary>
public sealed class RecordsService
{
    private static readonly string RecordsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker",
        "records.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private PersonalRecords? _cachedRecords;

    public RecordsService()
    {
        var folder = Path.GetDirectoryName(RecordsPath);
        if (folder != null) Directory.CreateDirectory(folder);
    }

    /// <summary>
    /// Loads personal records from storage.
    /// </summary>
    public async Task<Result<PersonalRecords>> LoadRecordsAsync(CancellationToken ct = default)
    {
        try
        {
            if (_cachedRecords != null)
                return Result<PersonalRecords>.Success(_cachedRecords);

            if (!File.Exists(RecordsPath))
            {
                _cachedRecords = new PersonalRecords();
                return Result<PersonalRecords>.Success(_cachedRecords);
            }

            var json = await File.ReadAllTextAsync(RecordsPath, ct);
            _cachedRecords = JsonSerializer.Deserialize<PersonalRecords>(json, JsonOptions) ?? new PersonalRecords();
            return Result<PersonalRecords>.Success(_cachedRecords);
        }
        catch (Exception ex)
        {
            return Result<PersonalRecords>.Failure($"Failed to load records: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates records with progress from current save, returns true if any records were broken.
    /// </summary>
    public async Task<Result<RecordUpdateResult>> UpdateRecordsAsync(
        PlayerProgress progress,
        string sessionName,
        CancellationToken ct = default)
    {
        try
        {
            var loadResult = await LoadRecordsAsync(ct);
            if (loadResult.IsFailure)
                return Result<RecordUpdateResult>.Failure(loadResult.Error!);

            var records = loadResult.Value!;
            var brokenRecords = new List<string>();
            var timestamp = DateTime.UtcNow;

            // Fastest to each phase
            if (progress.CurrentPhase >= ProgressionPhase.MidGame)
            {
                var current = progress.TotalPlayTime;
                if (records.FastestToMidGame == null || current < records.FastestToMidGame.Value.Time)
                {
                    records.FastestToMidGame = new RecordEntry
                    {
                        Time = current,
                        SessionName = sessionName,
                        AchievedAt = timestamp
                    };
                    brokenRecords.Add("Fastest to Mid-Game");
                }
            }

            if (progress.CurrentPhase >= ProgressionPhase.EndGame)
            {
                var current = progress.TotalPlayTime;
                if (records.FastestToEndGame == null || current < records.FastestToEndGame.Value.Time)
                {
                    records.FastestToEndGame = new RecordEntry
                    {
                        Time = current,
                        SessionName = sessionName,
                        AchievedAt = timestamp
                    };
                    brokenRecords.Add("Fastest to End-Game");
                }
            }

            // Highest data points
            if (progress.DataPointsEarned > (records.HighestDataPoints?.Value ?? 0))
            {
                records.HighestDataPoints = new RecordEntry<int>
                {
                    Value = progress.DataPointsEarned,
                    SessionName = sessionName,
                    AchievedAt = timestamp
                };
                brokenRecords.Add("Highest Data Points");
            }

            // Highest wave survived (parse from string)
            var waveNumber = ParseWaveNumber(progress.CurrentWave);
            if (waveNumber > (records.HighestWaveSurvived?.Value ?? 0))
            {
                records.HighestWaveSurvived = new RecordEntry<int>
                {
                    Value = waveNumber,
                    SessionName = sessionName,
                    AchievedAt = timestamp
                };
                brokenRecords.Add("Highest Wave Survived");
            }

            // Longest session
            if (progress.TotalPlayTime > (records.LongestSession?.Time ?? TimeSpan.Zero))
            {
                records.LongestSession = new RecordEntry
                {
                    Time = progress.TotalPlayTime,
                    SessionName = sessionName,
                    AchievedAt = timestamp
                };
                brokenRecords.Add("Longest Session");
            }

            // Save if any records broken
            if (brokenRecords.Count > 0)
            {
                var json = JsonSerializer.Serialize(records, JsonOptions);
                await File.WriteAllTextAsync(RecordsPath, json, ct);
                _cachedRecords = records;
            }

            return Result<RecordUpdateResult>.Success(new RecordUpdateResult
            {
                BrokenRecords = brokenRecords,
                CurrentRecords = records
            });
        }
        catch (Exception ex)
        {
            return Result<RecordUpdateResult>.Failure($"Failed to update records: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all records.
    /// </summary>
    public async Task<Result<Unit>> ClearRecordsAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(RecordsPath))
                File.Delete(RecordsPath);

            _cachedRecords = new PersonalRecords();
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to clear records: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to parse a wave number from the wave string.
    /// </summary>
    private static int ParseWaveNumber(string wave)
    {
        if (string.IsNullOrEmpty(wave)) return 0;

        // Try to extract number from wave string like "Wave 5" or just "5"
        var digits = new string(wave.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var result) ? result : 0;
    }
}

/// <summary>
/// Personal best records across all sessions.
/// </summary>
public sealed class PersonalRecords
{
    // Speed records
    public RecordEntry? FastestToMidGame { get; set; }
    public RecordEntry? FastestToEndGame { get; set; }
    public RecordEntry? FastestToMastery { get; set; }

    // Achievement records
    public RecordEntry<int>? HighestDataPoints { get; set; }
    public RecordEntry<int>? HighestWaveSurvived { get; set; }
    public RecordEntry<int>? MostBasesBuilt { get; set; }
    public RecordEntry<int>? MostResearchCompleted { get; set; }

    // Endurance records
    public RecordEntry? LongestSession { get; set; }
    public RecordEntry<int>? LongestSurvivalStreak { get; set; }
}

/// <summary>
/// A record entry with time value.
/// </summary>
public struct RecordEntry
{
    public TimeSpan Time { get; set; }
    public string SessionName { get; set; }
    public DateTime AchievedAt { get; set; }
}

/// <summary>
/// A record entry with typed value.
/// </summary>
public struct RecordEntry<T>
{
    public T Value { get; set; }
    public string SessionName { get; set; }
    public DateTime AchievedAt { get; set; }
}

/// <summary>
/// Result of updating records.
/// </summary>
public sealed record RecordUpdateResult
{
    public required IReadOnlyList<string> BrokenRecords { get; init; }
    public required PersonalRecords CurrentRecords { get; init; }
}
