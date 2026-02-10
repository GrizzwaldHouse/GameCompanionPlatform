namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Transforms raw spatial save data into map-ready view models.
/// Handles base clustering, connection detection, and coordinate normalization.
/// </summary>
public sealed class MapDataService
{
    // Buildings within this distance (world units) of a base core are considered part of that base.
    private const double BaseCoreRadius = 15000.0;

    // Categories that represent player-placed machines (not world objects, not foundables).
    private static readonly HashSet<string> MachineCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Production", "Power", "Storage", "Logistics", "Defense", "Extraction", "Research"
    };

    /// <summary>
    /// Builds map-ready data from a parsed save.
    /// </summary>
    public Result<MapData> BuildMapData(StarRuptureSave save)
    {
        if (save.Spatial == null)
            return Result<MapData>.Failure("No spatial data available in this save");

        var spatial = save.Spatial;

        // Filter to only player-placed buildings
        var buildings = spatial.Entities
            .Where(e => e.IsBuilding)
            .ToList();

        // Build base clusters around base cores
        var bases = BuildBaseClusters(buildings, spatial.BaseCores, spatial.ElectricityNetwork);

        // Build connections between bases
        var connections = BuildConnections(bases, spatial.ElectricityNetwork, spatial.Logistics);

        // Calculate world bounds from all building positions
        var bounds = CalculateBounds(buildings, spatial.PlayerPosition);

        return Result<MapData>.Success(new MapData
        {
            WorldBounds = bounds,
            PlayerPosition = spatial.PlayerPosition,
            Bases = bases,
            Connections = connections,
            TotalBuildingCount = buildings.Count,
            TotalPowerGrids = spatial.ElectricityNetwork.Subgraphs.Count,
            ActiveLogisticsRequests = spatial.Logistics.Requests.Count(r => !r.IsAborted)
        });
    }

    private List<BaseCluster> BuildBaseClusters(
        List<PlacedEntity> buildings,
        IReadOnlyList<BaseCoreData> baseCores,
        ElectricityNetworkData electricity)
    {
        var clusters = new List<BaseCluster>();
        var assignedBuildings = new HashSet<int>();

        // Find base core entities and build clusters around them
        var baseCoreEntities = baseCores
            .Select(bc => new
            {
                Core = bc,
                Entity = buildings.FirstOrDefault(b => b.PersistentId == bc.EntityId)
            })
            .Where(x => x.Entity != null)
            .ToList();

        for (var i = 0; i < baseCoreEntities.Count; i++)
        {
            var coreInfo = baseCoreEntities[i];
            var coreEntity = coreInfo.Entity!;
            var core = coreInfo.Core;

            // Find all buildings within radius of this base core
            var nearbyBuildings = buildings
                .Where(b => !assignedBuildings.Contains(b.PersistentId) &&
                           Distance2D(coreEntity.Position, b.Position) <= BaseCoreRadius)
                .ToList();

            foreach (var b in nearbyBuildings)
                assignedBuildings.Add(b.PersistentId);

            // Calculate cluster center and radius
            var center = CalculateCenter(nearbyBuildings);
            var radius = nearbyBuildings.Count > 0
                ? nearbyBuildings.Max(b => Distance2D(center, b.Position))
                : BaseCoreRadius;

            // Build machine summaries (only for machine categories)
            var machines = nearbyBuildings
                .Where(b => MachineCategories.Contains(b.EntityCategory))
                .GroupBy(b => b.EntityType)
                .Select(g => new MachineSummary
                {
                    TypeName = g.Key,
                    Category = g.First().EntityCategory,
                    Count = g.Count(),
                    RunningCount = g.Count(b => !b.IsDisabled && !b.HasMalfunction)
                })
                .OrderByDescending(m => m.Count)
                .ToList();

            // Find the power grid for this base core
            var powerGridId = electricity.Nodes
                .Where(n => n.EntityId == core.EntityId)
                .Select(n => n.SubgraphId)
                .FirstOrDefault();

            clusters.Add(new BaseCluster
            {
                Id = $"base_{i + 1}",
                Name = $"Base {i + 1}",
                Center = center,
                Radius = Math.Max(radius, 1000), // Minimum visual radius
                BaseCoreEntityId = core.EntityId,
                BaseCoreLevel = core.UpgradeLevel,
                Machines = machines,
                TotalBuildingCount = nearbyBuildings.Count,
                OperationalCount = nearbyBuildings.Count(b => !b.IsDisabled && !b.HasMalfunction),
                DisabledCount = nearbyBuildings.Count(b => b.IsDisabled),
                MalfunctionCount = nearbyBuildings.Count(b => b.HasMalfunction),
                PowerGridId = powerGridId
            });
        }

        // Handle orphaned buildings (not near any base core) -- group them as "Outposts"
        var orphanBuildings = buildings
            .Where(b => !assignedBuildings.Contains(b.PersistentId) && MachineCategories.Contains(b.EntityCategory))
            .ToList();

        if (orphanBuildings.Count > 0)
        {
            // Simple distance-based clustering for orphans
            var orphanClusters = ClusterByProximity(orphanBuildings, BaseCoreRadius);
            for (var i = 0; i < orphanClusters.Count; i++)
            {
                var cluster = orphanClusters[i];
                var center = CalculateCenter(cluster);
                var radius = cluster.Count > 0
                    ? cluster.Max(b => Distance2D(center, b.Position))
                    : 1000;

                var machines = cluster
                    .GroupBy(b => b.EntityType)
                    .Select(g => new MachineSummary
                    {
                        TypeName = g.Key,
                        Category = g.First().EntityCategory,
                        Count = g.Count(),
                        RunningCount = g.Count(b => !b.IsDisabled && !b.HasMalfunction)
                    })
                    .OrderByDescending(m => m.Count)
                    .ToList();

                clusters.Add(new BaseCluster
                {
                    Id = $"outpost_{i + 1}",
                    Name = $"Outpost {i + 1}",
                    Center = center,
                    Radius = Math.Max(radius, 1000),
                    BaseCoreEntityId = 0,
                    BaseCoreLevel = 0,
                    Machines = machines,
                    TotalBuildingCount = cluster.Count,
                    OperationalCount = cluster.Count(b => !b.IsDisabled && !b.HasMalfunction),
                    DisabledCount = cluster.Count(b => b.IsDisabled),
                    MalfunctionCount = cluster.Count(b => b.HasMalfunction),
                    PowerGridId = 0
                });
            }
        }

        return clusters;
    }

    private List<MapConnection> BuildConnections(
        List<BaseCluster> bases,
        ElectricityNetworkData electricity,
        LogisticsData logistics)
    {
        var connections = new List<MapConnection>();

        // Detect connections via shared power grids
        for (var i = 0; i < bases.Count; i++)
        {
            for (var j = i + 1; j < bases.Count; j++)
            {
                var baseA = bases[i];
                var baseB = bases[j];

                // Check if they share a power grid
                if (baseA.PowerGridId > 0 && baseA.PowerGridId == baseB.PowerGridId)
                {
                    connections.Add(new MapConnection
                    {
                        FromBaseId = baseA.Id,
                        ToBaseId = baseB.Id,
                        Start = baseA.Center,
                        End = baseB.Center,
                        Type = ConnectionType.PowerGrid,
                        Status = ConnectionStatus.Active
                    });
                }

                // Check for drone logistics connections between bases
                var hasLogisticsLink = logistics.Requests.Any(r =>
                    !r.IsAborted &&
                    bases[i].Machines.Any(m => m.Category == "Logistics") &&
                    bases[j].Machines.Any(m => m.Category == "Logistics"));

                if (hasLogisticsLink)
                {
                    connections.Add(new MapConnection
                    {
                        FromBaseId = baseA.Id,
                        ToBaseId = baseB.Id,
                        Start = baseA.Center,
                        End = baseB.Center,
                        Type = ConnectionType.DroneLogistics,
                        Status = ConnectionStatus.Active
                    });
                }
            }
        }

        return connections;
    }

    private static MapBounds CalculateBounds(List<PlacedEntity> buildings, WorldPosition playerPos)
    {
        if (buildings.Count == 0)
        {
            return new MapBounds
            {
                MinX = playerPos.X - 10000,
                MinY = playerPos.Y - 10000,
                MaxX = playerPos.X + 10000,
                MaxY = playerPos.Y + 10000
            };
        }

        var allX = buildings.Select(b => b.Position.X).Append(playerPos.X).ToList();
        var allY = buildings.Select(b => b.Position.Y).Append(playerPos.Y).ToList();

        var minX = allX.Min();
        var maxX = allX.Max();
        var minY = allY.Min();
        var maxY = allY.Max();

        // Add 10% padding
        var padX = Math.Max((maxX - minX) * 0.1, 5000);
        var padY = Math.Max((maxY - minY) * 0.1, 5000);

        return new MapBounds
        {
            MinX = minX - padX,
            MinY = minY - padY,
            MaxX = maxX + padX,
            MaxY = maxY + padY
        };
    }

    private static double Distance2D(WorldPosition a, WorldPosition b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static WorldPosition CalculateCenter(List<PlacedEntity> entities)
    {
        if (entities.Count == 0)
            return new WorldPosition();

        return new WorldPosition
        {
            X = entities.Average(e => e.Position.X),
            Y = entities.Average(e => e.Position.Y),
            Z = entities.Average(e => e.Position.Z)
        };
    }

    private static List<List<PlacedEntity>> ClusterByProximity(List<PlacedEntity> entities, double maxDistance)
    {
        var clusters = new List<List<PlacedEntity>>();
        var assigned = new HashSet<int>();

        foreach (var entity in entities)
        {
            if (assigned.Contains(entity.PersistentId))
                continue;

            var cluster = new List<PlacedEntity> { entity };
            assigned.Add(entity.PersistentId);

            // BFS to find all nearby entities
            var queue = new Queue<PlacedEntity>();
            queue.Enqueue(entity);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var other in entities)
                {
                    if (assigned.Contains(other.PersistentId))
                        continue;

                    if (Distance2D(current.Position, other.Position) <= maxDistance)
                    {
                        cluster.Add(other);
                        assigned.Add(other.PersistentId);
                        queue.Enqueue(other);
                    }
                }
            }

            if (cluster.Count >= 3) // Only create clusters with 3+ buildings
                clusters.Add(cluster);
        }

        return clusters;
    }
}
