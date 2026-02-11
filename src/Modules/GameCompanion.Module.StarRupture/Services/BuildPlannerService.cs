namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Manages build plans for factory layouts.
/// </summary>
public sealed class BuildPlannerService
{
    private readonly string _plansDir;
    private readonly List<BuildPlan> _plans = [];
    private bool _isLoaded;

    // Building cost estimates
    private static readonly Dictionary<string, Dictionary<string, int>> BuildingCosts = new(StringComparer.OrdinalIgnoreCase)
    {
        { "smelter", new Dictionary<string, int> { { "iron_plate", 20 }, { "wire", 10 } } },
        { "constructor", new Dictionary<string, int> { { "iron_plate", 15 }, { "cable", 5 } } },
        { "assembler", new Dictionary<string, int> { { "iron_plate", 30 }, { "cable", 10 }, { "iron_rod", 20 } } },
        { "conveyor", new Dictionary<string, int> { { "iron_plate", 2 } } },
        { "storage", new Dictionary<string, int> { { "iron_plate", 10 }, { "screw", 20 } } },
        { "generator", new Dictionary<string, int> { { "iron_plate", 25 }, { "wire", 15 }, { "iron_rod", 10 } } }
    };

    public BuildPlannerService()
    {
        _plansDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArcadiaTracker", "build_plans");
        Directory.CreateDirectory(_plansDir);
    }

    /// <summary>
    /// Gets all saved build plans.
    /// </summary>
    public async Task<Result<IReadOnlyList<BuildPlan>>> GetPlansAsync()
    {
        try
        {
            if (!_isLoaded)
                await LoadPlansAsync();

            return Result<IReadOnlyList<BuildPlan>>.Success(_plans.OrderByDescending(p => p.ModifiedAt).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<BuildPlan>>.Failure($"Failed to get plans: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new build plan.
    /// </summary>
    public Result<BuildPlan> CreatePlan(string name, string description)
    {
        try
        {
            var plan = new BuildPlan
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                Buildings = [],
                Connections = [],
                Stats = new BuildPlanStats
                {
                    TotalBuildings = 0,
                    BuiltCount = 0,
                    RemainingCount = 0,
                    EstimatedPowerUsage = 0,
                    EstimatedThroughput = 0
                },
                TotalCost = new BuildPlanCost
                {
                    TotalResources = new Dictionary<string, int>(),
                    CurrentResources = new Dictionary<string, int>(),
                    MissingResources = new Dictionary<string, int>()
                }
            };

            _plans.Add(plan);
            return Result<BuildPlan>.Success(plan);
        }
        catch (Exception ex)
        {
            return Result<BuildPlan>.Failure($"Failed to create plan: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a building to a plan.
    /// </summary>
    public Result<BuildPlan> AddBuilding(string planId, string buildingType, WorldPosition position, double rotation = 0)
    {
        try
        {
            var plan = _plans.FirstOrDefault(p => p.Id == planId);
            if (plan == null)
                return Result<BuildPlan>.Failure("Plan not found");

            var cost = GetBuildingCost(buildingType);
            var building = new PlannedBuilding
            {
                Id = Guid.NewGuid().ToString(),
                BuildingType = buildingType,
                Position = position,
                Rotation = rotation,
                Recipe = null,
                IsBuilt = false,
                Cost = cost
            };

            var buildings = plan.Buildings.ToList();
            buildings.Add(building);

            var updatedPlan = RecalculatePlan(plan, buildings, plan.Connections.ToList());
            var index = _plans.FindIndex(p => p.Id == planId);
            if (index >= 0)
                _plans[index] = updatedPlan;

            return Result<BuildPlan>.Success(updatedPlan);
        }
        catch (Exception ex)
        {
            return Result<BuildPlan>.Failure($"Failed to add building: {ex.Message}");
        }
    }

    /// <summary>
    /// Marks a building as built.
    /// </summary>
    public Result<BuildPlan> MarkBuilt(string planId, string buildingId)
    {
        try
        {
            var plan = _plans.FirstOrDefault(p => p.Id == planId);
            if (plan == null)
                return Result<BuildPlan>.Failure("Plan not found");

            var buildings = plan.Buildings.Select(b =>
            {
                if (b.Id != buildingId) return b;
                return new PlannedBuilding
                {
                    Id = b.Id,
                    BuildingType = b.BuildingType,
                    Position = b.Position,
                    Rotation = b.Rotation,
                    Recipe = b.Recipe,
                    IsBuilt = true,
                    Cost = b.Cost
                };
            }).ToList();

            var updatedPlan = RecalculatePlan(plan, buildings, plan.Connections.ToList());
            var index = _plans.FindIndex(p => p.Id == planId);
            if (index >= 0)
                _plans[index] = updatedPlan;

            return Result<BuildPlan>.Success(updatedPlan);
        }
        catch (Exception ex)
        {
            return Result<BuildPlan>.Failure($"Failed to mark built: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets available build templates.
    /// </summary>
    public Result<IReadOnlyList<BuildTemplate>> GetTemplates()
    {
        var templates = new List<BuildTemplate>
        {
            new BuildTemplate
            {
                Id = "basic_smelting",
                Name = "Basic Smelting Line",
                Description = "4 smelters producing iron plates",
                Category = "Production",
                OutputItem = "iron_plate",
                OutputRate = 120,
                Buildings = GenerateSmelterLine(4),
                EstimatedCost = CalculateTemplateCost(4, "smelter")
            },
            new BuildTemplate
            {
                Id = "constructor_array",
                Name = "Constructor Array",
                Description = "6 constructors for component production",
                Category = "Production",
                OutputItem = "various",
                OutputRate = 90,
                Buildings = GenerateConstructorArray(6),
                EstimatedCost = CalculateTemplateCost(6, "constructor")
            }
        };

        return Result<IReadOnlyList<BuildTemplate>>.Success(templates);
    }

    /// <summary>
    /// Saves all plans to disk.
    /// </summary>
    public async Task<Result<bool>> SavePlansAsync()
    {
        try
        {
            foreach (var plan in _plans)
            {
                var path = Path.Combine(_plansDir, $"{plan.Id}.json");
                var json = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(path, json);
            }
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to save plans: {ex.Message}");
        }
    }

    private async Task LoadPlansAsync()
    {
        _plans.Clear();

        var files = Directory.GetFiles(_plansDir, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var plan = JsonSerializer.Deserialize<BuildPlan>(json);
                if (plan != null)
                    _plans.Add(plan);
            }
            catch
            {
                // Skip invalid files
            }
        }

        _isLoaded = true;
    }

    private static BuildPlan RecalculatePlan(BuildPlan plan, List<PlannedBuilding> buildings, List<PlannedConnection> connections)
    {
        var totalResources = new Dictionary<string, int>();
        var builtCount = 0;

        foreach (var building in buildings)
        {
            foreach (var (resource, amount) in building.Cost.Resources)
            {
                if (!totalResources.ContainsKey(resource))
                    totalResources[resource] = 0;
                totalResources[resource] += amount;
            }

            if (building.IsBuilt)
                builtCount++;
        }

        var estimatedPower = buildings.Sum(b => EstimateBuildingPower(b.BuildingType));

        return new BuildPlan
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            CreatedAt = plan.CreatedAt,
            ModifiedAt = DateTime.Now,
            Buildings = buildings,
            Connections = connections,
            Stats = new BuildPlanStats
            {
                TotalBuildings = buildings.Count,
                BuiltCount = builtCount,
                RemainingCount = buildings.Count - builtCount,
                EstimatedPowerUsage = estimatedPower,
                EstimatedThroughput = buildings.Count * 15 // Simplified
            },
            TotalCost = new BuildPlanCost
            {
                TotalResources = totalResources,
                CurrentResources = new Dictionary<string, int>(), // Would need inventory data
                MissingResources = totalResources
            }
        };
    }

    private static BuildingCost GetBuildingCost(string buildingType)
    {
        var type = buildingType.ToLowerInvariant();
        foreach (var (key, cost) in BuildingCosts)
        {
            if (type.Contains(key))
                return new BuildingCost { Resources = cost };
        }
        return new BuildingCost { Resources = new Dictionary<string, int> { { "iron_plate", 10 } } };
    }

    private static double EstimateBuildingPower(string buildingType)
    {
        var type = buildingType.ToLowerInvariant();
        if (type.Contains("generator")) return 100;
        if (type.Contains("smelter")) return -30;
        if (type.Contains("assembler")) return -25;
        if (type.Contains("constructor")) return -20;
        return -10;
    }

    private static List<PlannedBuilding> GenerateSmelterLine(int count)
    {
        var buildings = new List<PlannedBuilding>();
        for (int i = 0; i < count; i++)
        {
            buildings.Add(new PlannedBuilding
            {
                Id = Guid.NewGuid().ToString(),
                BuildingType = "Smelter",
                Position = new WorldPosition { X = i * 500, Y = 0, Z = 0 },
                Rotation = 0,
                IsBuilt = false,
                Cost = GetBuildingCost("smelter")
            });
        }
        return buildings;
    }

    private static List<PlannedBuilding> GenerateConstructorArray(int count)
    {
        var buildings = new List<PlannedBuilding>();
        for (int i = 0; i < count; i++)
        {
            buildings.Add(new PlannedBuilding
            {
                Id = Guid.NewGuid().ToString(),
                BuildingType = "Constructor",
                Position = new WorldPosition { X = (i % 3) * 500, Y = (i / 3) * 500, Z = 0 },
                Rotation = 0,
                IsBuilt = false,
                Cost = GetBuildingCost("constructor")
            });
        }
        return buildings;
    }

    private static BuildPlanCost CalculateTemplateCost(int count, string buildingType)
    {
        var baseCost = GetBuildingCost(buildingType);
        var total = new Dictionary<string, int>();
        foreach (var (resource, amount) in baseCost.Resources)
        {
            total[resource] = amount * count;
        }
        return new BuildPlanCost
        {
            TotalResources = total,
            CurrentResources = new Dictionary<string, int>(),
            MissingResources = total
        };
    }
}
