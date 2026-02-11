namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Generates logistics traffic heatmaps from save data.
/// </summary>
public sealed class LogisticsHeatmapService
{
    private const double DefaultCellSize = 1000; // 10 meters in Unreal units
    private const int CongestionThreshold = 10;
    private const int HighTrafficThreshold = 5;

    /// <summary>
    /// Generates a heatmap from logistics data.
    /// </summary>
    public Result<LogisticsHeatmap> GenerateHeatmap(StarRuptureSave save, double? cellSize = null)
    {
        try
        {
            var gridCellSize = cellSize ?? DefaultCellSize;

            if (save.Spatial == null)
            {
                return Result<LogisticsHeatmap>.Success(new LogisticsHeatmap
                {
                    Cells = [],
                    CongestionZones = [],
                    DeadZones = [],
                    GridCellSize = gridCellSize,
                    TotalActiveRequests = 0,
                    AverageTrafficDensity = 0
                });
            }

            // Get active logistics requests
            var requests = save.Spatial.Logistics.Requests
                .Where(r => !r.IsAborted)
                .ToList();

            // Map requests to entity positions
            var entityPositions = save.Spatial.Entities
                .ToDictionary(e => e.PersistentId, e => e.Position);

            // Build grid cells
            var cellTraffic = new Dictionary<(int, int), List<int>>();

            foreach (var request in requests)
            {
                if (!entityPositions.TryGetValue(request.RequesterEntityId, out var pos))
                    continue;

                var gridX = (int)Math.Floor(pos.X / gridCellSize);
                var gridY = (int)Math.Floor(pos.Y / gridCellSize);
                var key = (gridX, gridY);

                if (!cellTraffic.ContainsKey(key))
                    cellTraffic[key] = [];

                cellTraffic[key].Add(request.RequesterEntityId);
            }

            // Convert to heatmap cells
            var cells = cellTraffic.Select(kvp => new HeatmapCell
            {
                GridX = kvp.Key.Item1,
                GridY = kvp.Key.Item2,
                CenterPosition = new WorldPosition
                {
                    X = (kvp.Key.Item1 + 0.5) * gridCellSize,
                    Y = (kvp.Key.Item2 + 0.5) * gridCellSize,
                    Z = 0
                },
                RequestCount = kvp.Value.Count,
                Heat = DetermineHeatLevel(kvp.Value.Count),
                EntityIds = kvp.Value.Distinct().ToList()
            }).ToList();

            // Find congestion zones
            var congestionZones = cells
                .Where(c => c.Heat == HeatLevel.Critical)
                .Select(c => new CongestionZone
                {
                    Center = c.CenterPosition,
                    Radius = gridCellSize,
                    RequestCount = c.RequestCount,
                    Cause = $"High logistics demand ({c.RequestCount} requests)",
                    Recommendation = "Add more logistics drones or distribute workload"
                })
                .ToList();

            // Find dead zones (areas with buildings but no logistics)
            var deadZones = FindDeadZones(save.Spatial, cellTraffic, gridCellSize);

            var totalRequests = requests.Count;
            var avgDensity = cells.Count > 0 ? cells.Average(c => c.RequestCount) : 0;

            return Result<LogisticsHeatmap>.Success(new LogisticsHeatmap
            {
                Cells = cells,
                CongestionZones = congestionZones,
                DeadZones = deadZones,
                GridCellSize = gridCellSize,
                TotalActiveRequests = totalRequests,
                AverageTrafficDensity = avgDensity
            });
        }
        catch (Exception ex)
        {
            return Result<LogisticsHeatmap>.Failure($"Failed to generate heatmap: {ex.Message}");
        }
    }

    private static HeatLevel DetermineHeatLevel(int requestCount)
    {
        return requestCount switch
        {
            0 => HeatLevel.None,
            < 3 => HeatLevel.Low,
            < HighTrafficThreshold => HeatLevel.Medium,
            < CongestionThreshold => HeatLevel.High,
            _ => HeatLevel.Critical
        };
    }

    private static List<DeadZone> FindDeadZones(
        SpatialData spatial,
        Dictionary<(int, int), List<int>> cellTraffic,
        double gridCellSize)
    {
        var deadZones = new List<DeadZone>();

        // Group buildings by grid cell
        var buildingCells = new Dictionary<(int, int), List<PlacedEntity>>();

        foreach (var entity in spatial.Entities.Where(e => e.IsBuilding))
        {
            var gridX = (int)Math.Floor(entity.Position.X / gridCellSize);
            var gridY = (int)Math.Floor(entity.Position.Y / gridCellSize);
            var key = (gridX, gridY);

            if (!buildingCells.ContainsKey(key))
                buildingCells[key] = [];

            buildingCells[key].Add(entity);
        }

        // Find cells with buildings but no traffic
        foreach (var (key, buildings) in buildingCells)
        {
            if (cellTraffic.ContainsKey(key) || buildings.Count < 3)
                continue;

            var avgX = buildings.Average(b => b.Position.X);
            var avgY = buildings.Average(b => b.Position.Y);

            deadZones.Add(new DeadZone
            {
                Center = new WorldPosition { X = avgX, Y = avgY, Z = 0 },
                Radius = gridCellSize,
                EntityCount = buildings.Count,
                PossibleReason = "Buildings not connected to logistics network"
            });
        }

        return deadZones;
    }
}
