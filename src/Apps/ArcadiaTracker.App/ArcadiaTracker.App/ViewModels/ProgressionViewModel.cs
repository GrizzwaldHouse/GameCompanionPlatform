namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for detailed progression tracking view.
/// </summary>
public sealed partial class ProgressionViewModel : ObservableObject
{
    [ObservableProperty]
    private PlayerProgress? _progress;

    [ObservableProperty]
    private ObservableCollection<PhaseProgress> _phases = [];

    public void UpdateProgress(PlayerProgress progress)
    {
        Progress = progress;

        // Calculate phase-specific progress
        var phases = new List<PhaseProgress>
        {
            new PhaseProgress
            {
                Name = "Early Game",
                Description = "First base, basic resources, power setup",
                IsCompleted = progress.CurrentPhase != ProgressionPhase.EarlyGame,
                IsCurrent = progress.CurrentPhase == ProgressionPhase.EarlyGame,
                Progress = progress.CurrentPhase == ProgressionPhase.EarlyGame ? CalculateEarlyProgress(progress) : (progress.CurrentPhase > ProgressionPhase.EarlyGame ? 100 : 0)
            },
            new PhaseProgress
            {
                Name = "Mid Game",
                Description = "Factory automation, blueprint collection, corporation rep",
                IsCompleted = progress.CurrentPhase > ProgressionPhase.MidGame,
                IsCurrent = progress.CurrentPhase == ProgressionPhase.MidGame,
                Progress = progress.CurrentPhase == ProgressionPhase.MidGame ? CalculateMidProgress(progress) : (progress.CurrentPhase > ProgressionPhase.MidGame ? 100 : 0)
            },
            new PhaseProgress
            {
                Name = "End Game",
                Description = "Expansion, defense, advanced production",
                IsCompleted = progress.CurrentPhase > ProgressionPhase.EndGame,
                IsCurrent = progress.CurrentPhase == ProgressionPhase.EndGame,
                Progress = progress.CurrentPhase == ProgressionPhase.EndGame ? CalculateEndProgress(progress) : (progress.CurrentPhase > ProgressionPhase.EndGame ? 100 : 0)
            },
            new PhaseProgress
            {
                Name = "Mastery",
                Description = "All blueprints unlocked, corporations maxed",
                IsCompleted = progress.CurrentPhase == ProgressionPhase.Mastery,
                IsCurrent = progress.CurrentPhase == ProgressionPhase.Mastery,
                Progress = progress.CurrentPhase == ProgressionPhase.Mastery ? 100 : 0
            }
        };

        Phases = new ObservableCollection<PhaseProgress>(phases);
    }

    private static double CalculateEarlyProgress(PlayerProgress progress)
    {
        // Early game: < 5 hours, < 30 blueprints
        var timeProgress = Math.Min(100, (progress.TotalPlayTime.TotalHours / 5) * 100);
        var blueprintProgress = Math.Min(100, (progress.BlueprintsUnlocked / 30.0) * 100);
        return (timeProgress + blueprintProgress) / 2;
    }

    private static double CalculateMidProgress(PlayerProgress progress)
    {
        // Mid game: 5-20 hours, 30-80 blueprints, some corporation rep
        var timeProgress = Math.Min(100, ((progress.TotalPlayTime.TotalHours - 5) / 15) * 100);
        var blueprintProgress = Math.Min(100, ((progress.BlueprintsUnlocked - 30) / 50.0) * 100);
        var dataProgress = Math.Min(100, (progress.DataPointsEarned / 15000.0) * 100);
        return (timeProgress + blueprintProgress + dataProgress) / 3;
    }

    private static double CalculateEndProgress(PlayerProgress progress)
    {
        // End game: > 20 hours, 80+ blueprints, high data points
        var blueprintProgress = Math.Min(100, ((progress.BlueprintsUnlocked - 80) / 100.0) * 100);
        var dataProgress = Math.Min(100, ((progress.DataPointsEarned - 15000) / 35000.0) * 100);
        return (blueprintProgress + dataProgress) / 2;
    }
}

/// <summary>
/// Represents a game phase with progress tracking.
/// </summary>
public sealed class PhaseProgress
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsCurrent { get; init; }
    public double Progress { get; init; }
}
