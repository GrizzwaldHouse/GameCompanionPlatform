namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Analyzes power grid health and predicts brownouts.
/// </summary>
public sealed class PowerGridAnalyzerService
{
    // Known generator types
    private static readonly HashSet<string> GeneratorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "generator", "solar", "wind", "nuclear", "geothermal", "coal", "fuel"
    };

    // Estimated power values per building type (placeholder values)
    private static readonly Dictionary<string, double> PowerEstimates = new(StringComparer.OrdinalIgnoreCase)
    {
        { "generator", 100 },
        { "solar", 50 },
        { "smelter", -30 },
        { "assembler", -25 },
        { "constructor", -20 },
        { "manufacturer", -50 },
        { "refinery", -40 }
    };

    /// <summary>
    /// Analyzes the power grid from save data.
    /// </summary>
    public Result<PowerGridAnalysis> AnalyzePowerGrid(StarRuptureSave save)
    {
        try
        {
            if (save.Spatial == null)
            {
                return Result<PowerGridAnalysis>.Success(new PowerGridAnalysis
                {
                    Networks = [],
                    TotalGeneration = 0,
                    TotalConsumption = 0,
                    OverallStatus = GridStatus.Disconnected,
                    Warnings = [],
                    PlacementSuggestions = []
                });
            }

            var networks = new List<GridNetwork>();
            var warnings = new List<PowerWarning>();
            var suggestions = new List<GeneratorPlacement>();

            // Group entities by subgraph
            var nodesBySubgraph = save.Spatial.ElectricityNetwork.Nodes
                .GroupBy(n => n.SubgraphId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (subgraphId, nodes) in nodesBySubgraph)
            {
                var powerNodes = new List<PowerNode>();
                double generation = 0;
                double consumption = 0;

                foreach (var node in nodes)
                {
                    var entity = save.Spatial.Entities
                        .FirstOrDefault(e => e.PersistentId == node.EntityId);

                    if (entity == null) continue;

                    var isGenerator = IsGenerator(entity.EntityType);
                    var powerValue = EstimatePower(entity.EntityType);

                    powerNodes.Add(new PowerNode
                    {
                        EntityId = node.EntityId,
                        EntityType = entity.EntityType,
                        Position = entity.Position,
                        IsGenerator = isGenerator,
                        PowerValue = powerValue
                    });

                    if (powerValue > 0)
                        generation += powerValue;
                    else
                        consumption += Math.Abs(powerValue);
                }

                var status = DetermineStatus(generation, consumption);
                var isBrownoutRisk = status == GridStatus.Strained || status == GridStatus.Brownout;

                networks.Add(new GridNetwork
                {
                    NetworkId = subgraphId,
                    Nodes = powerNodes,
                    Generation = generation,
                    Consumption = consumption,
                    Status = status,
                    IsBrownoutRisk = isBrownoutRisk
                });

                // Generate warnings
                if (isBrownoutRisk)
                {
                    warnings.Add(new PowerWarning
                    {
                        NetworkId = subgraphId,
                        Severity = status == GridStatus.Brownout ? WarningSeverity.Critical : WarningSeverity.Warning,
                        Message = status == GridStatus.Brownout
                            ? $"Network {subgraphId} is experiencing brownout!"
                            : $"Network {subgraphId} is at risk of brownout",
                        Suggestion = "Add more generators to this network"
                    });

                    // Suggest generator placement
                    var centerPos = CalculateNetworkCenter(powerNodes);
                    suggestions.Add(new GeneratorPlacement
                    {
                        TargetNetworkId = subgraphId,
                        SuggestedPosition = centerPos,
                        RecommendedGeneratorType = "Generator",
                        PowerDeficit = consumption - generation
                    });
                }
            }

            var totalGen = networks.Sum(n => n.Generation);
            var totalCon = networks.Sum(n => n.Consumption);

            return Result<PowerGridAnalysis>.Success(new PowerGridAnalysis
            {
                Networks = networks,
                TotalGeneration = totalGen,
                TotalConsumption = totalCon,
                OverallStatus = DetermineStatus(totalGen, totalCon),
                Warnings = warnings,
                PlacementSuggestions = suggestions
            });
        }
        catch (Exception ex)
        {
            return Result<PowerGridAnalysis>.Failure($"Failed to analyze power grid: {ex.Message}");
        }
    }

    private static bool IsGenerator(string entityType)
    {
        return GeneratorTypes.Any(g => entityType.Contains(g, StringComparison.OrdinalIgnoreCase));
    }

    private static double EstimatePower(string entityType)
    {
        foreach (var (key, value) in PowerEstimates)
        {
            if (entityType.Contains(key, StringComparison.OrdinalIgnoreCase))
                return value;
        }
        return -10; // Default consumption
    }

    private static GridStatus DetermineStatus(double generation, double consumption)
    {
        if (generation == 0) return GridStatus.Disconnected;

        var ratio = consumption / generation;
        return ratio switch
        {
            < 0.7 => GridStatus.Healthy,
            < 0.9 => GridStatus.Stable,
            < 1.0 => GridStatus.Strained,
            _ => GridStatus.Brownout
        };
    }

    private static WorldPosition CalculateNetworkCenter(List<PowerNode> nodes)
    {
        if (nodes.Count == 0)
            return new WorldPosition { X = 0, Y = 0, Z = 0 };

        var avgX = nodes.Average(n => n.Position.X);
        var avgY = nodes.Average(n => n.Position.Y);
        var avgZ = nodes.Average(n => n.Position.Z);

        return new WorldPosition { X = avgX, Y = avgY, Z = avgZ };
    }
}
