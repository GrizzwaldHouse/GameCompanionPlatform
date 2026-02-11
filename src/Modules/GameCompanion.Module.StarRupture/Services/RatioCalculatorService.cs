namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Calculates machine requirements for target production rates.
/// </summary>
public sealed class RatioCalculatorService
{
    // Known recipes: item -> (machine type, rate per machine per minute, inputs)
    private static readonly Dictionary<string, RecipeInfo> Recipes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "iron_plate", new RecipeInfo("Smelter", 30, [("iron_ore", 30)]) },
        { "copper_plate", new RecipeInfo("Smelter", 30, [("copper_ore", 30)]) },
        { "steel_plate", new RecipeInfo("Foundry", 15, [("iron_ore", 45)]) },
        { "iron_rod", new RecipeInfo("Constructor", 15, [("iron_plate", 15)]) },
        { "screw", new RecipeInfo("Constructor", 40, [("iron_rod", 10)]) },
        { "wire", new RecipeInfo("Constructor", 30, [("copper_plate", 15)]) },
        { "cable", new RecipeInfo("Constructor", 15, [("wire", 30)]) },
        { "concrete", new RecipeInfo("Constructor", 15, [("limestone", 45)]) },
        { "reinforced_plate", new RecipeInfo("Assembler", 5, [("iron_plate", 30), ("screw", 60)]) },
        { "rotor", new RecipeInfo("Assembler", 4, [("iron_rod", 20), ("screw", 100)]) },
        { "modular_frame", new RecipeInfo("Assembler", 2, [("reinforced_plate", 3), ("iron_rod", 12)]) }
    };

    /// <summary>
    /// Calculates required machines for a target production rate.
    /// </summary>
    public Result<RatioCalculation> CalculateRatio(
        string targetItem,
        double targetRate,
        StarRuptureSave? save = null)
    {
        try
        {
            var requirements = new List<MachineRequirement>();
            CalculateRequirements(targetItem, targetRate, requirements, new HashSet<string>());

            // Compare with current build if save provided
            var comparisons = new List<ComparisonDelta>();
            if (save?.Spatial != null)
            {
                var currentMachines = CountMachinesByType(save.Spatial.Entities);

                foreach (var req in requirements)
                {
                    var currentCount = currentMachines.GetValueOrDefault(req.MachineType, 0);
                    comparisons.Add(new ComparisonDelta
                    {
                        MachineType = req.MachineType,
                        CurrentCount = currentCount,
                        RequiredCount = req.RequiredCount,
                        Status = currentCount >= req.RequiredCount
                            ? (currentCount > req.RequiredCount ? DeltaStatus.Excess : DeltaStatus.Sufficient)
                            : DeltaStatus.NeedMore
                    });
                }
            }

            var canAchieve = comparisons.Count == 0 || comparisons.All(c => c.Status != DeltaStatus.NeedMore);

            return Result<RatioCalculation>.Success(new RatioCalculation
            {
                TargetItem = targetItem,
                TargetRate = targetRate,
                Requirements = requirements,
                CurrentVsRequired = comparisons,
                CanAchieveTarget = canAchieve,
                BottleneckReason = canAchieve ? null : GetBottleneckReason(comparisons)
            });
        }
        catch (Exception ex)
        {
            return Result<RatioCalculation>.Failure($"Failed to calculate ratio: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets available items that can be calculated.
    /// </summary>
    public IReadOnlyList<string> GetAvailableItems()
    {
        return Recipes.Keys.ToList();
    }

    private void CalculateRequirements(
        string item,
        double rate,
        List<MachineRequirement> requirements,
        HashSet<string> visited)
    {
        if (!Recipes.TryGetValue(item, out var recipe) || visited.Contains(item))
            return;

        visited.Add(item);

        var machinesNeeded = (int)Math.Ceiling(rate / recipe.RatePerMachine);

        var inputs = recipe.Inputs.Select(i => new InputRequirement
        {
            ItemType = i.Item,
            RequiredRate = (i.Rate / recipe.RatePerMachine) * machinesNeeded
        }).ToList();

        requirements.Add(new MachineRequirement
        {
            MachineType = recipe.MachineType,
            ProducesItem = item,
            RequiredCount = machinesNeeded,
            ProductionRate = recipe.RatePerMachine,
            Inputs = inputs
        });

        // Recursively calculate input requirements
        foreach (var input in recipe.Inputs)
        {
            var inputRate = (input.Rate / recipe.RatePerMachine) * machinesNeeded;
            CalculateRequirements(input.Item, inputRate, requirements, visited);
        }
    }

    private static Dictionary<string, int> CountMachinesByType(IReadOnlyList<PlacedEntity> entities)
    {
        return entities
            .Where(e => e.IsBuilding)
            .GroupBy(e => ExtractMachineType(e.EntityType))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static string ExtractMachineType(string entityType)
    {
        var parts = entityType.Split('/');
        var name = parts.LastOrDefault() ?? entityType;
        return name.Split('_').FirstOrDefault() ?? name;
    }

    private static string GetBottleneckReason(List<ComparisonDelta> comparisons)
    {
        var needed = comparisons.Where(c => c.Status == DeltaStatus.NeedMore).ToList();
        if (needed.Count == 0) return null!;

        var most = needed.OrderByDescending(n => n.Delta).First();
        return $"Need {most.Delta} more {most.MachineType}(s)";
    }

    private record RecipeInfo(string MachineType, double RatePerMachine, List<(string Item, double Rate)> Inputs);
}
