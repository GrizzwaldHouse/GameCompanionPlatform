namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Analyzes production chains to identify bottlenecks.
/// </summary>
public sealed class BottleneckAnalyzerService
{
    // Known production building types
    private static readonly HashSet<string> ProductionBuildings = new(StringComparer.OrdinalIgnoreCase)
    {
        "smelter", "furnace", "assembler", "manufacturer", "refinery",
        "constructor", "foundry", "packager", "blender"
    };

    /// <summary>
    /// Analyzes the save for production bottlenecks.
    /// </summary>
    public Result<BottleneckAnalysis> AnalyzeBottlenecks(StarRuptureSave save)
    {
        try
        {
            if (save.Spatial == null)
            {
                return Result<BottleneckAnalysis>.Success(new BottleneckAnalysis
                {
                    Bottlenecks = [],
                    Chains = [],
                    TotalMachines = 0,
                    BottleneckCount = 0
                });
            }

            var productionEntities = save.Spatial.Entities
                .Where(e => e.IsBuilding && IsProductionBuilding(e.EntityType))
                .ToList();

            var bottlenecks = new List<BottleneckInfo>();
            var chains = new List<ProductionChain>();

            // Analyze each production entity
            foreach (var entity in productionEntities)
            {
                var bottleneck = AnalyzeEntity(entity, save.Spatial);
                if (bottleneck != null)
                {
                    bottlenecks.Add(bottleneck);
                }
            }

            // Group entities into chains by proximity
            var entityChains = GroupIntoChains(productionEntities, save.Spatial);
            chains.AddRange(entityChains);

            return Result<BottleneckAnalysis>.Success(new BottleneckAnalysis
            {
                Bottlenecks = bottlenecks.OrderByDescending(b => b.Severity).ToList(),
                Chains = chains,
                TotalMachines = productionEntities.Count,
                BottleneckCount = bottlenecks.Count
            });
        }
        catch (Exception ex)
        {
            return Result<BottleneckAnalysis>.Failure($"Failed to analyze bottlenecks: {ex.Message}");
        }
    }

    private static bool IsProductionBuilding(string entityType)
    {
        return ProductionBuildings.Any(b => entityType.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    private static BottleneckInfo? AnalyzeEntity(PlacedEntity entity, SpatialData spatial)
    {
        // Check for disabled or malfunctioning machines
        if (entity.IsDisabled)
        {
            return new BottleneckInfo
            {
                EntityId = entity.PersistentId,
                EntityType = entity.EntityType,
                Position = entity.Position,
                Severity = BottleneckSeverity.High,
                Reason = "Machine is disabled",
                Recommendation = "Enable or repair the machine",
                ThroughputRatio = 0
            };
        }

        if (entity.HasMalfunction)
        {
            return new BottleneckInfo
            {
                EntityId = entity.PersistentId,
                EntityType = entity.EntityType,
                Position = entity.Position,
                Severity = BottleneckSeverity.Critical,
                Reason = "Machine has malfunction",
                Recommendation = "Repair the machine immediately",
                ThroughputRatio = 0
            };
        }

        // Check if machine is isolated (no nearby machines)
        var nearby = spatial.Entities
            .Where(e => e.PersistentId != entity.PersistentId && IsProductionBuilding(e.EntityType))
            .Where(e => Distance(e.Position, entity.Position) < 2000) // Within 20 meters
            .ToList();

        if (nearby.Count == 0)
        {
            return new BottleneckInfo
            {
                EntityId = entity.PersistentId,
                EntityType = entity.EntityType,
                Position = entity.Position,
                Severity = BottleneckSeverity.Low,
                Reason = "Machine is isolated from production chain",
                Recommendation = "Connect to other machines or relocate",
                ThroughputRatio = 0.5
            };
        }

        return null;
    }

    private static List<ProductionChain> GroupIntoChains(List<PlacedEntity> entities, SpatialData spatial)
    {
        var chains = new List<ProductionChain>();

        // Group by entity type to find chains
        var byType = entities.GroupBy(e => ExtractBaseType(e.EntityType));

        foreach (var group in byType)
        {
            var nodes = group.Select(e => new ChainNode
            {
                EntityId = e.PersistentId,
                EntityType = e.EntityType,
                Position = e.Position,
                IsBottleneck = e.IsDisabled || e.HasMalfunction,
                InputsFrom = [],
                OutputsTo = []
            }).ToList();

            chains.Add(new ProductionChain
            {
                OutputItem = group.Key,
                Nodes = nodes,
                TheoreticalThroughput = nodes.Count * 60, // Placeholder
                ActualThroughput = nodes.Count(n => !n.IsBottleneck) * 60
            });
        }

        return chains;
    }

    private static string ExtractBaseType(string entityType)
    {
        // Extract base type from full path like "/Game/Buildings/Smelter_T1"
        var parts = entityType.Split('/');
        var name = parts.LastOrDefault() ?? entityType;
        return name.Split('_').FirstOrDefault() ?? name;
    }

    private static double Distance(WorldPosition a, WorldPosition b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
