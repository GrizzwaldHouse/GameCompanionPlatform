namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Calculates cataclysm timer state from EnviroWave data.
/// </summary>
public sealed class CataclysmTimerService
{
    // Known wave durations in approximate minutes (based on community data)
    // These are estimates - actual values vary by game settings
    private static readonly Dictionary<string, double> WaveDurationMinutes = new()
    {
        ["Wave 1"] = 20.0,
        ["Wave 2"] = 25.0,
        ["Wave 3"] = 30.0,
        ["Wave 4"] = 35.0,
        ["Wave 5"] = 40.0
    };

    private const double DefaultWaveDurationMinutes = 30.0;

    /// <summary>
    /// Analyzes EnviroWave data to produce cataclysm timer state.
    /// </summary>
    public CataclysmState AnalyzeWave(EnviroWaveData waveData, TimeSpan totalPlayTime)
    {
        var waveName = string.IsNullOrEmpty(waveData.Wave) ? "Unknown" : waveData.Wave;
        var stageName = string.IsNullOrEmpty(waveData.Stage) ? "Unknown" : waveData.Stage;
        var progress = Math.Clamp(waveData.Progress, 0.0, 1.0);

        // Estimate remaining time based on wave duration and current progress
        var durationMinutes = WaveDurationMinutes.GetValueOrDefault(waveName, DefaultWaveDurationMinutes);
        var remainingFraction = 1.0 - progress;
        var remainingMinutes = durationMinutes * remainingFraction;
        var remaining = TimeSpan.FromMinutes(Math.Max(0, remainingMinutes));

        var urgency = remaining.TotalMinutes switch
        {
            > 15 => CataclysmUrgency.Safe,
            > 5 => CataclysmUrgency.Caution,
            > 1 => CataclysmUrgency.Warning,
            _ => CataclysmUrgency.Critical
        };

        return new CataclysmState
        {
            CurrentWave = waveName,
            CurrentStage = stageName,
            StageProgress = progress,
            EstimatedTimeRemaining = remaining,
            Urgency = urgency
        };
    }
}
