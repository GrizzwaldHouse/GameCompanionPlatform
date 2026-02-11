namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Provides optimal research path recommendations.
/// </summary>
public sealed class ResearchPathService
{
    private readonly ResearchTreeService _treeService;

    // Priority research by goal type
    private static readonly Dictionary<ResearchGoalType, string[]> GoalPriorities = new()
    {
        { ResearchGoalType.Automation, ["conveyor", "splitter", "merger", "arm", "robot"] },
        { ResearchGoalType.Defense, ["turret", "wall", "shield", "ammo"] },
        { ResearchGoalType.Production, ["smelter", "assembler", "constructor", "foundry"] },
        { ResearchGoalType.PowerGeneration, ["generator", "solar", "battery", "coal"] },
        { ResearchGoalType.Logistics, ["drone", "storage", "container", "truck"] }
    };

    public ResearchPathService(ResearchTreeService treeService)
    {
        _treeService = treeService;
    }

    /// <summary>
    /// Generates optimal research path for a goal.
    /// </summary>
    public async Task<Result<ResearchPath>> GeneratePathAsync(
        CraftingData crafting,
        ResearchGoalType goalType = ResearchGoalType.Automation)
    {
        try
        {
            var treeResult = await _treeService.BuildTreeAsync(crafting);
            if (!treeResult.IsSuccess)
                return Result<ResearchPath>.Failure(treeResult.Error ?? "Failed to build research tree");

            var tree = treeResult.Value!;
            var allNodes = tree.Categories.SelectMany(c => c.Nodes).ToList();

            // Get priority keywords for goal
            var priorities = GoalPriorities.GetValueOrDefault(goalType, []);

            // Find high priority unlocks
            var highPriority = allNodes
                .Where(n => n.Status == ResearchNodeStatus.Locked)
                .Where(n => priorities.Any(p => n.Name.Contains(p, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Find currently available (unlocked) research
            var available = allNodes
                .Where(n => n.Status == ResearchNodeStatus.Unlocked)
                .ToList();

            // Generate recommended path
            var path = GenerateRecommendedPath(allNodes, priorities, goalType);

            // Create current goal
            var goal = new ResearchGoal
            {
                Name = GetGoalName(goalType),
                Description = GetGoalDescription(goalType),
                Type = goalType,
                RequiredResearch = highPriority.Select(n => n.Name).ToList(),
                CompletedSteps = path.Count(s => s.IsUnlocked),
                TotalSteps = path.Count
            };

            return Result<ResearchPath>.Success(new ResearchPath
            {
                RecommendedPath = path,
                HighPriorityUnlocks = highPriority,
                CurrentlyAvailable = available,
                CurrentGoal = goal,
                EstimatedDataPointsNeeded = path.Where(p => !p.IsUnlocked).Sum(p => p.DataPointsCost),
                CompletionPercent = tree.UnlockPercent
            });
        }
        catch (Exception ex)
        {
            return Result<ResearchPath>.Failure($"Failed to generate research path: {ex.Message}");
        }
    }

    private static List<ResearchStep> GenerateRecommendedPath(
        List<ResearchNode> allNodes,
        string[] priorities,
        ResearchGoalType goalType)
    {
        var steps = new List<ResearchStep>();
        var order = 1;

        // First, add priority nodes
        foreach (var priority in priorities)
        {
            var matchingNodes = allNodes
                .Where(n => n.Name.Contains(priority, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.Status == ResearchNodeStatus.Unlocked ? 0 : 1);

            foreach (var node in matchingNodes)
            {
                if (steps.Any(s => s.Node.Id == node.Id))
                    continue;

                steps.Add(new ResearchStep
                {
                    Order = order++,
                    Node = node,
                    Reason = $"Priority for {goalType}: {priority}",
                    UnlocksAbilities = GetUnlockedAbilities(node, goalType),
                    Prerequisites = [], // Would need dependency tracking
                    IsUnlocked = node.Status == ResearchNodeStatus.Unlocked,
                    DataPointsCost = EstimateDataPointCost(node)
                });
            }
        }

        return steps.Take(20).ToList(); // Limit to top 20 recommendations
    }

    private static List<string> GetUnlockedAbilities(ResearchNode node, ResearchGoalType goalType)
    {
        var abilities = new List<string>();

        if (node.Name.Contains("conveyor", StringComparison.OrdinalIgnoreCase))
            abilities.Add("Item transport automation");
        if (node.Name.Contains("smelter", StringComparison.OrdinalIgnoreCase))
            abilities.Add("Metal processing");
        if (node.Name.Contains("turret", StringComparison.OrdinalIgnoreCase))
            abilities.Add("Automated defense");
        if (node.Name.Contains("generator", StringComparison.OrdinalIgnoreCase))
            abilities.Add("Power generation");
        if (node.Name.Contains("drone", StringComparison.OrdinalIgnoreCase))
            abilities.Add("Aerial logistics");

        if (abilities.Count == 0)
            abilities.Add($"Unlocks {node.Name}");

        return abilities;
    }

    private static int EstimateDataPointCost(ResearchNode node)
    {
        // Simplified cost estimation based on node characteristics
        if (node.Name.Contains("mk2", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Contains("tier2", StringComparison.OrdinalIgnoreCase))
            return 100;
        if (node.Name.Contains("mk3", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Contains("tier3", StringComparison.OrdinalIgnoreCase))
            return 250;
        if (node.Name.Contains("advanced", StringComparison.OrdinalIgnoreCase))
            return 150;

        return 50;
    }

    private static string GetGoalName(ResearchGoalType goalType)
    {
        return goalType switch
        {
            ResearchGoalType.Automation => "Factory Automation",
            ResearchGoalType.Defense => "Base Defense",
            ResearchGoalType.Production => "Production Efficiency",
            ResearchGoalType.PowerGeneration => "Power Infrastructure",
            ResearchGoalType.Logistics => "Logistics Network",
            ResearchGoalType.Exploration => "World Exploration",
            _ => "Custom Goal"
        };
    }

    private static string GetGoalDescription(ResearchGoalType goalType)
    {
        return goalType switch
        {
            ResearchGoalType.Automation => "Unlock technologies for automated item transport and processing",
            ResearchGoalType.Defense => "Research defensive structures and weapons for cataclysm survival",
            ResearchGoalType.Production => "Improve production capabilities with advanced machines",
            ResearchGoalType.PowerGeneration => "Expand power generation and distribution capacity",
            ResearchGoalType.Logistics => "Enhance logistics with drones, storage, and transport",
            ResearchGoalType.Exploration => "Unlock vehicles and tools for world exploration",
            _ => "Custom research path based on your needs"
        };
    }
}
