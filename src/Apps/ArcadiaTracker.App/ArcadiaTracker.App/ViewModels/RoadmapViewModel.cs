namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the roadmap/recommendations view.
/// </summary>
public sealed partial class RoadmapViewModel : ObservableObject
{
    [ObservableProperty]
    private PlayerProgress? _progress;

    [ObservableProperty]
    private ObservableCollection<Recommendation> _recommendations = [];

    [ObservableProperty]
    private string _currentPhaseAdvice = string.Empty;

    public void UpdateProgress(PlayerProgress progress)
    {
        Progress = progress;
        GenerateRecommendations(progress);
    }

    private void GenerateRecommendations(PlayerProgress progress)
    {
        var recs = new List<Recommendation>();

        // Phase-specific advice
        CurrentPhaseAdvice = progress.CurrentPhase switch
        {
            ProgressionPhase.EarlyGame => "Focus on establishing your first base and getting power running. Explore nearby areas for blueprints.",
            ProgressionPhase.MidGame => "Time to expand! Hunt for blueprints, build up corporation reputation, and start automating production.",
            ProgressionPhase.EndGame => "You're in the endgame now. Focus on unlocking remaining blueprints and maxing out corporations.",
            ProgressionPhase.Mastery => "You've mastered StarRupture! Consider starting a new game with different strategies.",
            _ => "Keep exploring and building!"
        };

        // Blueprint recommendations
        var blueprintPercent = progress.BlueprintProgress * 100;
        if (blueprintPercent < 50)
        {
            recs.Add(new Recommendation
            {
                Priority = RecommendationPriority.High,
                Title = "Hunt for Blueprints",
                Description = $"You have {progress.BlueprintsUnlocked}/{progress.BlueprintsTotal} blueprints ({blueprintPercent:F0}%). Explore POIs and ship items to corporations to unlock more.",
                Category = "Blueprints",
                Icon = "📘"
            });
        }

        // Map unlock recommendation
        if (!progress.MapUnlocked)
        {
            recs.Add(new Recommendation
            {
                Priority = RecommendationPriority.High,
                Title = "Unlock the Map",
                Description = "Reach Moon Energy Corporation Level 3 to unlock the interactive map. Ship Calcium Ore to Moon Energy.",
                Category = "Exploration",
                Icon = "🗺️"
            });
        }

        // Corporation recommendations
        if (progress.HighestCorporationLevel < 5)
        {
            recs.Add(new Recommendation
            {
                Priority = RecommendationPriority.Medium,
                Title = "Level Up Corporations",
                Description = $"Your highest corporation is {progress.HighestCorporationName} at Level {progress.HighestCorporationLevel}. Ship more items to level up and unlock rewards.",
                Category = "Corporations",
                Icon = "🏢"
            });
        }

        // Data points recommendation
        if (progress.DataPointsEarned < 10000)
        {
            recs.Add(new Recommendation
            {
                Priority = RecommendationPriority.Medium,
                Title = "Earn More Data Points",
                Description = $"You have {progress.DataPointsEarned:N0} data points. Ship items to corporations to earn more for upgrades.",
                Category = "Resources",
                Icon = "📊"
            });
        }

        // Automation recommendation (mid/late game)
        if (progress.CurrentPhase >= ProgressionPhase.MidGame && progress.BlueprintsUnlocked < 100)
        {
            recs.Add(new Recommendation
            {
                Priority = RecommendationPriority.Low,
                Title = "Automate Production",
                Description = "Set up conveyor systems and automated crafting to increase efficiency. Focus on high-value items for corporations.",
                Category = "Automation",
                Icon = "⚙️"
            });
        }

        // Badge recommendations
        var unearnedBadges = GameCompanion.Module.StarRupture.Progression.Badges.AllBadges.Count - progress.EarnedBadges.Count;
        if (unearnedBadges > 0)
        {
            recs.Add(new Recommendation
            {
                Priority = RecommendationPriority.Low,
                Title = "Earn More Badges",
                Description = $"You have {unearnedBadges} badges left to earn. Check the dashboard for available achievements.",
                Category = "Achievements",
                Icon = "🏆"
            });
        }

        Recommendations = new ObservableCollection<Recommendation>(
            recs.OrderBy(r => r.Priority));
    }
}

/// <summary>
/// A roadmap recommendation.
/// </summary>
public sealed class Recommendation
{
    public required RecommendationPriority Priority { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Icon { get; init; }
}

/// <summary>
/// Priority level for recommendations.
/// </summary>
public enum RecommendationPriority
{
    High = 0,
    Medium = 1,
    Low = 2
}
