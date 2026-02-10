namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Analyzes production machines, power grids, and enables base comparison.
/// </summary>
public sealed class ProductionDataService
{
    private readonly MapDataService _mapService;

    public ProductionDataService(MapDataService mapService)
    {
        _mapService = mapService;
    }

    /// <summary>
    /// Builds a global production summary from save data.
    /// </summary>
    public Result<ProductionSummary> BuildProductionSummary(StarRuptureSave save)
    {
        // First build the map data to get clustered bases
        var mapResult = _mapService.BuildMapData(save);

        if (save.Spatial == null)
            return Result<ProductionSummary>.Failure("No spatial data available in save.");

        // Count all machines from the spatial entities
        var entities = save.Spatial.Entities;
        var machines = entities.Where(e => e.IsBuilding).ToList();

        var totalMachines = machines.Count;
        var running = machines.Count(m => !m.IsDisabled && !m.HasMalfunction);
        var disabled = machines.Count(m => m.IsDisabled);
        var malfunctioning = machines.Count(m => m.HasMalfunction);

        // Category breakdown
        var byCategory = machines
            .GroupBy(m => m.EntityCategory)
            .Select(g => new CategoryBreakdown
            {
                Category = g.Key,
                Total = g.Count(),
                Running = g.Count(m => !m.IsDisabled && !m.HasMalfunction)
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        // Power grid summary from electricity network
        var powerSummary = BuildPowerGridSummary(save.Spatial.ElectricityNetwork);

        // Per-base info (use map data if available)
        var perBase = new List<BaseProductionInfo>();
        if (mapResult.IsSuccess && mapResult.Value != null)
        {
            foreach (var baseCluster in mapResult.Value.Bases)
            {
                perBase.Add(new BaseProductionInfo
                {
                    BaseId = baseCluster.Id,
                    BaseName = baseCluster.Name,
                    TotalBuildings = baseCluster.TotalBuildingCount,
                    OperationalBuildings = baseCluster.OperationalCount,
                    DisabledBuildings = baseCluster.DisabledCount,
                    MalfunctioningBuildings = baseCluster.MalfunctionCount,
                    Machines = baseCluster.Machines
                });
            }
        }

        return Result<ProductionSummary>.Success(new ProductionSummary
        {
            TotalMachines = totalMachines,
            RunningMachines = running,
            DisabledMachines = disabled,
            MalfunctioningMachines = malfunctioning,
            ByCategory = byCategory,
            PowerSummary = powerSummary,
            PerBase = perBase
        });
    }

    /// <summary>
    /// Compares selected bases side by side.
    /// </summary>
    public Result<BaseComparison> CompareBases(IReadOnlyList<BaseProductionInfo> allBases, IReadOnlyList<string> baseIdsToCompare)
    {
        if (baseIdsToCompare.Count < 2)
            return Result<BaseComparison>.Failure("Select at least 2 bases to compare.");

        var selected = allBases.Where(b => baseIdsToCompare.Contains(b.BaseId)).ToList();
        if (selected.Count < 2)
            return Result<BaseComparison>.Failure("Could not find selected bases.");

        return Result<BaseComparison>.Success(new BaseComparison { Bases = selected });
    }

    private static PowerGridSummary BuildPowerGridSummary(ElectricityNetworkData? network)
    {
        if (network == null)
            return new PowerGridSummary { TotalGrids = 0, TotalNodes = 0, Grids = [] };

        var grids = network.Subgraphs.Select(sg => new PowerGridInfo
        {
            GridId = sg.SubgraphId,
            NodeCount = network.Nodes.Count(n => n.SubgraphId == sg.SubgraphId)
        }).ToList();

        return new PowerGridSummary
        {
            TotalGrids = grids.Count,
            TotalNodes = network.Nodes.Count,
            Grids = grids
        };
    }
}
