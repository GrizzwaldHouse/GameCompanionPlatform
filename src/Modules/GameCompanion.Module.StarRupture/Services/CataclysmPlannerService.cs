namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Provides comprehensive cataclysm survival planning.
/// </summary>
public sealed class CataclysmPlannerService
{
    private readonly CataclysmTimerService _timerService;

    // Defense building types
    private static readonly HashSet<string> DefenseBuildings = new(StringComparer.OrdinalIgnoreCase)
    {
        "turret", "wall", "barrier", "shield", "bunker", "sentry"
    };

    public CataclysmPlannerService(CataclysmTimerService timerService)
    {
        _timerService = timerService;
    }

    /// <summary>
    /// Generates a comprehensive survival plan.
    /// </summary>
    public Result<CataclysmPlan> GeneratePlan(StarRuptureSave save)
    {
        try
        {
            var currentState = _timerService.AnalyzeWave(save.EnviroWave, save.PlayTime);
            var tasks = GenerateSurvivalTasks(save, currentState);
            var resources = AnalyzeResourceRequirements(save);
            var defenses = GenerateDefenseRecommendations(save);
            var readiness = CalculateReadiness(save, defenses, resources);

            var waveNumber = ParseWaveNumber(currentState.CurrentWave);
            var nextMilestone = GetNextMilestone(waveNumber);

            return Result<CataclysmPlan>.Success(new CataclysmPlan
            {
                CurrentState = currentState,
                Tasks = tasks,
                RequiredResources = resources,
                DefenseRecommendations = defenses,
                Readiness = readiness,
                NextMilestone = nextMilestone,
                TimeToNextWave = currentState.EstimatedTimeRemaining
            });
        }
        catch (Exception ex)
        {
            return Result<CataclysmPlan>.Failure($"Failed to generate plan: {ex.Message}");
        }
    }

    private static List<SurvivalTask> GenerateSurvivalTasks(StarRuptureSave save, CataclysmState state)
    {
        var tasks = new List<SurvivalTask>();

        // Check power stability
        if (save.Spatial?.ElectricityNetwork.Nodes.Count > 0)
        {
            tasks.Add(new SurvivalTask
            {
                Name = "Ensure Power Stability",
                Description = "Verify all generators are running and power grid is stable",
                Priority = TaskPriority.High,
                IsCompleted = true, // Simplified check
                Category = "Power",
                EstimatedTime = TimeSpan.FromMinutes(5)
            });
        }

        // Check defenses
        var defenseCount = save.Spatial?.Entities
            .Count(e => e.IsBuilding && DefenseBuildings.Any(d => e.EntityType.Contains(d, StringComparison.OrdinalIgnoreCase))) ?? 0;

        tasks.Add(new SurvivalTask
        {
            Name = "Build Defense Structures",
            Description = defenseCount < 5 ? "Build more turrets and defensive structures" : "Defense structures in place",
            Priority = defenseCount < 5 ? TaskPriority.Critical : TaskPriority.Low,
            IsCompleted = defenseCount >= 5,
            Category = "Defense",
            EstimatedTime = TimeSpan.FromMinutes(10)
        });

        // Check logistics
        var activeRequests = save.Spatial?.Logistics.Requests.Count(r => !r.IsAborted) ?? 0;
        tasks.Add(new SurvivalTask
        {
            Name = "Clear Logistics Queue",
            Description = activeRequests > 50 ? "Too many pending logistics requests" : "Logistics running smoothly",
            Priority = activeRequests > 50 ? TaskPriority.High : TaskPriority.Low,
            IsCompleted = activeRequests <= 50,
            Category = "Logistics"
        });

        // Add time-based tasks
        if (state.Urgency == CataclysmUrgency.Critical)
        {
            tasks.Add(new SurvivalTask
            {
                Name = "Prepare for Imminent Wave",
                Description = "Cataclysm wave incoming! Final preparations needed.",
                Priority = TaskPriority.Critical,
                IsCompleted = false,
                Category = "Emergency"
            });
        }

        return tasks.OrderByDescending(t => t.Priority).ToList();
    }

    private static List<ResourceRequirement> AnalyzeResourceRequirements(StarRuptureSave save)
    {
        var requirements = new List<ResourceRequirement>();

        // Standard cataclysm preparation resources
        var resourceTypes = new[] { "iron_plate", "copper_plate", "steel_plate", "concrete", "ammo" };
        var requiredAmounts = new[] { 500, 300, 200, 200, 100 };

        for (int i = 0; i < resourceTypes.Length; i++)
        {
            requirements.Add(new ResourceRequirement
            {
                ResourceType = resourceTypes[i],
                RequiredAmount = requiredAmounts[i],
                CurrentAmount = 0 // Would need inventory tracking to determine actual amounts
            });
        }

        return requirements;
    }

    private static List<DefenseRecommendation> GenerateDefenseRecommendations(StarRuptureSave save)
    {
        var recommendations = new List<DefenseRecommendation>();

        if (save.Spatial == null)
            return recommendations;

        // Count existing defenses
        var turretCount = save.Spatial.Entities
            .Count(e => e.EntityType.Contains("turret", StringComparison.OrdinalIgnoreCase));

        // Calculate base center
        var buildings = save.Spatial.Entities.Where(e => e.IsBuilding).ToList();
        if (buildings.Count == 0)
            return recommendations;

        var centerX = buildings.Average(b => b.Position.X);
        var centerY = buildings.Average(b => b.Position.Y);

        // Recommend perimeter defense
        if (turretCount < 8)
        {
            recommendations.Add(new DefenseRecommendation
            {
                StructureType = "Turret",
                RecommendedCount = 8,
                CurrentCount = turretCount,
                SuggestedPosition = new WorldPosition { X = centerX + 2000, Y = centerY, Z = 0 },
                Reason = "Perimeter defense needed for cataclysm waves"
            });
        }

        return recommendations;
    }

    private static ReadinessScore CalculateReadiness(
        StarRuptureSave save,
        List<DefenseRecommendation> defenses,
        List<ResourceRequirement> resources)
    {
        // Defense score
        var defenseScore = defenses.Count == 0 ? 100.0 :
            defenses.Average(d => Math.Min(100, (double)d.CurrentCount / d.RecommendedCount * 100));

        // Resource score
        var resourceScore = resources.Count == 0 ? 100.0 :
            resources.Average(r => r.FulfillmentPercent);

        // Power score (simplified)
        var powerScore = 75.0; // Would need actual power grid analysis

        var overallScore = (defenseScore * 0.4 + resourceScore * 0.3 + powerScore * 0.3);

        var level = overallScore switch
        {
            >= 90 => ReadinessLevel.Fortified,
            >= 75 => ReadinessLevel.WellPrepared,
            >= 50 => ReadinessLevel.Adequate,
            >= 25 => ReadinessLevel.Minimal,
            _ => ReadinessLevel.Unprepared
        };

        return new ReadinessScore
        {
            OverallScore = overallScore,
            DefenseScore = defenseScore,
            ResourceScore = resourceScore,
            PowerScore = powerScore,
            Assessment = GetReadinessAssessment(level),
            Level = level
        };
    }

    private static string GetReadinessAssessment(ReadinessLevel level)
    {
        return level switch
        {
            ReadinessLevel.Fortified => "Excellent! Your base is well fortified for the cataclysm.",
            ReadinessLevel.WellPrepared => "Good preparation. Minor improvements recommended.",
            ReadinessLevel.Adequate => "Basic preparation in place. Consider improvements.",
            ReadinessLevel.Minimal => "Warning: Minimal preparation. Prioritize defenses.",
            ReadinessLevel.Unprepared => "Critical: Unprepared for cataclysm. Immediate action required!",
            _ => "Unknown readiness state"
        };
    }

    private static int ParseWaveNumber(string wave)
    {
        if (string.IsNullOrEmpty(wave)) return 0;
        var parts = wave.Split(' ', '_', '-');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var num))
                return num;
        }
        return 0;
    }

    private static string GetNextMilestone(int currentWave)
    {
        return currentWave switch
        {
            < 3 => "Wave 3: First major cataclysm event",
            < 5 => "Wave 5: Increased enemy intensity",
            < 10 => "Wave 10: Boss encounter",
            < 15 => "Wave 15: Advanced enemies",
            _ => $"Wave {((currentWave / 5) + 1) * 5}: Next milestone"
        };
    }
}
