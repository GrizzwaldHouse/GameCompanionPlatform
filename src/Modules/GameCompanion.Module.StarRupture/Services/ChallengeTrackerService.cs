namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Tracks challenge mode progress.
/// </summary>
public sealed class ChallengeTrackerService
{
    // Built-in challenges
    private static readonly List<Challenge> BuiltInChallenges =
    [
        new Challenge
        {
            Id = "first_factory",
            Name = "First Factory",
            Description = "Build your first automated production line",
            Category = ChallengeCategory.Production,
            Difficulty = ChallengeDifficulty.Easy,
            Objectives =
            [
                new ChallengeObjective { Description = "Build 5 smelters", Target = 5, Current = 0 },
                new ChallengeObjective { Description = "Build 3 constructors", Target = 3, Current = 0 }
            ],
            Rewards =
            [
                new ChallengeReward { Type = "Experience", Description = "Gain experience", Amount = 100 }
            ]
        },
        new Challenge
        {
            Id = "survivor",
            Name = "Survivor",
            Description = "Survive 5 cataclysm waves",
            Category = ChallengeCategory.Survival,
            Difficulty = ChallengeDifficulty.Medium,
            Objectives =
            [
                new ChallengeObjective { Description = "Survive cataclysm waves", Target = 5, Current = 0 }
            ],
            Rewards =
            [
                new ChallengeReward { Type = "Achievement", Description = "Unlock achievement", Amount = 1 }
            ]
        },
        new Challenge
        {
            Id = "efficiency_master",
            Name = "Efficiency Master",
            Description = "Achieve 95% factory efficiency",
            Category = ChallengeCategory.Efficiency,
            Difficulty = ChallengeDifficulty.Hard,
            Objectives =
            [
                new ChallengeObjective { Description = "Reach 95% efficiency", Target = 95, Current = 0 }
            ],
            Rewards =
            [
                new ChallengeReward { Type = "Title", Description = "Earn 'Efficiency Master' title", Amount = 1 }
            ]
        },
        new Challenge
        {
            Id = "speed_demon",
            Name = "Speed Demon",
            Description = "Complete early game in under 2 hours",
            Category = ChallengeCategory.Speed,
            Difficulty = ChallengeDifficulty.Expert,
            Objectives =
            [
                new ChallengeObjective { Description = "Complete early game milestones", Target = 100, Current = 0 }
            ],
            Rewards =
            [
                new ChallengeReward { Type = "Achievement", Description = "Speed Demon achievement", Amount = 1 }
            ]
        }
    ];

    /// <summary>
    /// Gets challenge tracker with current progress.
    /// </summary>
    public Result<ChallengeTracker> GetChallengeTracker(StarRuptureSave save)
    {
        try
        {
            var challenges = UpdateChallengeProgress(save);
            var completed = challenges.Count(c => c.IsCompleted);
            var inProgress = challenges
                .Where(c => !c.IsCompleted)
                .Select(c => new ChallengeProgress
                {
                    Challenge = c,
                    OverallProgress = c.Objectives.Average(o => o.Progress),
                    ObjectiveProgress = c.Objectives.ToList(),
                    TimeSpent = save.PlayTime
                })
                .Where(p => p.OverallProgress > 0)
                .ToList();

            return Result<ChallengeTracker>.Success(new ChallengeTracker
            {
                Challenges = challenges,
                CompletedCount = completed,
                TotalCount = challenges.Count,
                CompletionPercent = challenges.Count > 0 ? (double)completed / challenges.Count * 100 : 0,
                InProgress = inProgress
            });
        }
        catch (Exception ex)
        {
            return Result<ChallengeTracker>.Failure($"Failed to get challenges: {ex.Message}");
        }
    }

    private static List<Challenge> UpdateChallengeProgress(StarRuptureSave save)
    {
        var challenges = new List<Challenge>();

        foreach (var template in BuiltInChallenges)
        {
            var updatedObjectives = template.Objectives.Select(o =>
            {
                var current = GetObjectiveProgress(o.Description, save);
                return new ChallengeObjective
                {
                    Description = o.Description,
                    Target = o.Target,
                    Current = current
                };
            }).ToList();

            challenges.Add(new Challenge
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Difficulty = template.Difficulty,
                Objectives = updatedObjectives,
                Rewards = template.Rewards,
                IsCompleted = updatedObjectives.All(o => o.IsCompleted),
                CompletedAt = updatedObjectives.All(o => o.IsCompleted) ? DateTime.Now : null
            });
        }

        return challenges;
    }

    private static int GetObjectiveProgress(string description, StarRuptureSave save)
    {
        var desc = description.ToLowerInvariant();

        if (save.Spatial == null) return 0;

        if (desc.Contains("smelter"))
            return save.Spatial.Entities.Count(e => e.EntityType.Contains("smelter", StringComparison.OrdinalIgnoreCase));

        if (desc.Contains("constructor"))
            return save.Spatial.Entities.Count(e => e.EntityType.Contains("constructor", StringComparison.OrdinalIgnoreCase));

        if (desc.Contains("wave"))
        {
            var waveParts = save.EnviroWave.Wave.Split(' ', '_', '-');
            foreach (var part in waveParts)
            {
                if (int.TryParse(part, out var waveNum))
                    return waveNum;
            }
        }

        if (desc.Contains("efficiency"))
        {
            var total = save.Spatial.Entities.Count(e => e.IsBuilding);
            var operational = save.Spatial.Entities.Count(e => e.IsBuilding && !e.IsDisabled && !e.HasMalfunction);
            return total > 0 ? (int)((double)operational / total * 100) : 0;
        }

        return 0;
    }
}
