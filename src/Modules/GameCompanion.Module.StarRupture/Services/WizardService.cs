namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Provides What's Next wizard recommendations.
/// </summary>
public sealed class WizardService
{
    /// <summary>
    /// Generates recommendations based on current game state.
    /// </summary>
    public Result<WizardRecommendations> GetRecommendations(StarRuptureSave save)
    {
        try
        {
            var phase = DetermineGamePhase(save);
            var suggestions = GenerateSuggestions(save, phase);
            var goals = GetAvailableGoals(save);
            var primaryGoal = goals.FirstOrDefault(g => g.IsActive) ?? goals.FirstOrDefault();

            return Result<WizardRecommendations>.Success(new WizardRecommendations
            {
                Suggestions = suggestions,
                CurrentPhase = phase,
                ProgressPercent = CalculateOverallProgress(save),
                PrimaryGoal = primaryGoal,
                AvailableGoals = goals
            });
        }
        catch (Exception ex)
        {
            return Result<WizardRecommendations>.Failure($"Failed to get recommendations: {ex.Message}");
        }
    }

    private static string DetermineGamePhase(StarRuptureSave save)
    {
        if (!save.GameState.TutorialCompleted)
            return "Tutorial";

        var unlockPercent = save.Crafting.TotalRecipeCount > 0
            ? (double)save.Crafting.UnlockedRecipeCount / save.Crafting.TotalRecipeCount * 100
            : 0;

        if (unlockPercent < 25)
            return "Early Game";
        if (unlockPercent < 50)
            return "Mid Game";
        if (unlockPercent < 75)
            return "Late Game";

        return "End Game";
    }

    private static List<WizardSuggestion> GenerateSuggestions(StarRuptureSave save, string phase)
    {
        var suggestions = new List<WizardSuggestion>();

        if (phase == "Tutorial")
        {
            suggestions.Add(new WizardSuggestion
            {
                Title = "Complete the Tutorial",
                Description = "Follow the in-game tutorial to learn the basics",
                Priority = SuggestionPriority.Urgent,
                Category = SuggestionCategory.Production,
                Steps = ["Follow tutorial prompts", "Build your first machines", "Learn crafting"],
                Reasoning = "The tutorial teaches essential game mechanics"
            });
        }
        else if (phase == "Early Game")
        {
            suggestions.Add(new WizardSuggestion
            {
                Title = "Automate Basic Resources",
                Description = "Set up automated iron and copper production",
                Priority = SuggestionPriority.High,
                Category = SuggestionCategory.Production,
                Steps = ["Build miners on ore deposits", "Connect to smelters", "Set up storage"],
                Reasoning = "Automation frees you to focus on expansion",
                Benefits = ["Passive resource generation", "Foundation for advanced recipes"]
            });

            suggestions.Add(new WizardSuggestion
            {
                Title = "Prepare for Cataclysm",
                Description = "Build defenses before the first wave",
                Priority = SuggestionPriority.High,
                Category = SuggestionCategory.Defense,
                Steps = ["Research turrets", "Build defensive perimeter", "Stockpile ammo"],
                Reasoning = "The first cataclysm wave can destroy unprepared bases"
            });
        }
        else if (phase == "Mid Game")
        {
            suggestions.Add(new WizardSuggestion
            {
                Title = "Expand Power Generation",
                Description = "Your factory needs more power to grow",
                Priority = SuggestionPriority.Medium,
                Category = SuggestionCategory.Power,
                Steps = ["Build more generators", "Set up fuel production", "Create power grid"],
                Reasoning = "Power limitations will bottleneck production"
            });

            suggestions.Add(new WizardSuggestion
            {
                Title = "Unlock Advanced Research",
                Description = "Research higher tier machines",
                Priority = SuggestionPriority.Medium,
                Category = SuggestionCategory.Research,
                Steps = ["Gather data points", "Unlock Tier 2 machines", "Upgrade production lines"],
                Reasoning = "Advanced machines are more efficient"
            });
        }
        else
        {
            suggestions.Add(new WizardSuggestion
            {
                Title = "Optimize Factory Efficiency",
                Description = "Fine-tune your production for maximum output",
                Priority = SuggestionPriority.Medium,
                Category = SuggestionCategory.Optimization,
                Steps = ["Analyze bottlenecks", "Balance production ratios", "Reduce waste"],
                Reasoning = "Efficiency matters more at scale"
            });
        }

        // Add suggestions based on current state
        if (save.Spatial != null)
        {
            var disabledCount = save.Spatial.Entities.Count(e => e.IsDisabled);
            if (disabledCount > 5)
            {
                suggestions.Add(new WizardSuggestion
                {
                    Title = "Enable Disabled Buildings",
                    Description = $"You have {disabledCount} disabled buildings",
                    Priority = SuggestionPriority.Medium,
                    Category = SuggestionCategory.Optimization,
                    Steps = ["Check power supply", "Verify input resources", "Enable buildings"],
                    Reasoning = "Disabled buildings reduce efficiency"
                });
            }

            var malfunctionCount = save.Spatial.Entities.Count(e => e.HasMalfunction);
            if (malfunctionCount > 0)
            {
                suggestions.Add(new WizardSuggestion
                {
                    Title = "Repair Malfunctioning Machines",
                    Description = $"You have {malfunctionCount} machines with malfunctions",
                    Priority = SuggestionPriority.High,
                    Category = SuggestionCategory.Optimization,
                    Steps = ["Locate malfunctioning machines", "Repair or rebuild", "Check for causes"],
                    Reasoning = "Malfunctions halt production"
                });
            }
        }

        return suggestions.OrderByDescending(s => s.Priority).Take(5).ToList();
    }

    private static List<WizardGoal> GetAvailableGoals(StarRuptureSave save)
    {
        var goals = new List<WizardGoal>
        {
            new WizardGoal
            {
                Id = "complete_tutorial",
                Name = "Complete Tutorial",
                Description = "Learn the basics of the game",
                Type = GoalType.Tutorial,
                Progress = save.GameState.TutorialCompleted ? 100 : 50,
                Milestones =
                [
                    new GoalMilestone { Name = "Start Tutorial", Description = "Begin the tutorial", IsCompleted = true, Order = 1 },
                    new GoalMilestone { Name = "Build First Machine", Description = "Place your first machine", IsCompleted = save.GameState.TutorialCompleted, Order = 2 },
                    new GoalMilestone { Name = "Complete Tutorial", Description = "Finish all tutorial steps", IsCompleted = save.GameState.TutorialCompleted, Order = 3 }
                ],
                IsActive = !save.GameState.TutorialCompleted
            },
            new WizardGoal
            {
                Id = "survive_wave_5",
                Name = "Survive Wave 5",
                Description = "Build defenses to survive the first 5 waves",
                Type = GoalType.MainQuest,
                Progress = GetWaveProgress(save, 5),
                Milestones =
                [
                    new GoalMilestone { Name = "Build Turrets", Description = "Place defensive turrets", IsCompleted = HasTurrets(save), Order = 1 },
                    new GoalMilestone { Name = "Survive Wave 1", Description = "Complete wave 1", IsCompleted = GetCurrentWave(save) >= 1, Order = 2 },
                    new GoalMilestone { Name = "Survive Wave 5", Description = "Complete wave 5", IsCompleted = GetCurrentWave(save) >= 5, Order = 3 }
                ],
                IsActive = save.GameState.TutorialCompleted && GetCurrentWave(save) < 5
            }
        };

        return goals;
    }

    private static double CalculateOverallProgress(StarRuptureSave save)
    {
        var tutorialWeight = save.GameState.TutorialCompleted ? 10 : 0;
        var researchWeight = save.Crafting.TotalRecipeCount > 0
            ? (double)save.Crafting.UnlockedRecipeCount / save.Crafting.TotalRecipeCount * 50
            : 0;
        var waveWeight = Math.Min(40, GetCurrentWave(save) * 4);

        return tutorialWeight + researchWeight + waveWeight;
    }

    private static int GetCurrentWave(StarRuptureSave save)
    {
        var parts = save.EnviroWave.Wave.Split(' ', '_', '-');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var wave))
                return wave;
        }
        return 0;
    }

    private static double GetWaveProgress(StarRuptureSave save, int targetWave)
    {
        var current = GetCurrentWave(save);
        return Math.Min(100, (double)current / targetWave * 100);
    }

    private static bool HasTurrets(StarRuptureSave save)
    {
        return save.Spatial?.Entities.Any(e => e.EntityType.Contains("turret", StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}
