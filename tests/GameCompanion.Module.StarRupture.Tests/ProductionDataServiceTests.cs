using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class ProductionDataServiceTests
{
    private readonly ProductionDataService _service;

    public ProductionDataServiceTests()
    {
        var mapService = new MapDataService();
        _service = new ProductionDataService(mapService);
    }

    [Fact]
    public void BuildProductionSummary_WithNoSpatialData_ShouldReturnFailure()
    {
        var save = CreateSaveWithSpatial(null);

        var result = _service.BuildProductionSummary(save);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No spatial data");
    }

    [Fact]
    public void BuildProductionSummary_WithEmptyEntities_ShouldReturnZeroMachines()
    {
        var save = CreateSaveWithSpatial(new SpatialData
        {
            PlayerPosition = new WorldPosition { X = 0, Y = 0, Z = 0 },
            Entities = [],
            BaseCores = [],
            ElectricityNetwork = new ElectricityNetworkData
            {
                Nodes = [],
                Subgraphs = []
            },
            Logistics = new LogisticsData { Requests = [] }
        });

        var result = _service.BuildProductionSummary(save);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalMachines.Should().Be(0);
        result.Value.RunningMachines.Should().Be(0);
        result.Value.DisabledMachines.Should().Be(0);
        result.Value.MalfunctioningMachines.Should().Be(0);
        result.Value.EfficiencyPercent.Should().Be(100);
    }

    [Fact]
    public void BuildProductionSummary_WithMixedMachines_ShouldCalculateEfficiency()
    {
        var save = CreateSaveWithSpatial(new SpatialData
        {
            PlayerPosition = new WorldPosition { X = 0, Y = 0, Z = 0 },
            Entities = [
                CreateEntity(1, "Production", 100, 200, isBuilding: true, isDisabled: false, hasMalfunction: false),
                CreateEntity(2, "Production", 150, 250, isBuilding: true, isDisabled: true, hasMalfunction: false),
                CreateEntity(3, "Power", 300, 400, isBuilding: true, isDisabled: false, hasMalfunction: true),
                CreateEntity(4, "Storage", 500, 600, isBuilding: true, isDisabled: false, hasMalfunction: false)
            ],
            BaseCores = [],
            ElectricityNetwork = new ElectricityNetworkData
            {
                Nodes = [],
                Subgraphs = []
            },
            Logistics = new LogisticsData { Requests = [] }
        });

        var result = _service.BuildProductionSummary(save);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalMachines.Should().Be(4);
        result.Value.RunningMachines.Should().Be(2); // Entity 1 and 4 are running
        result.Value.DisabledMachines.Should().Be(1); // Entity 2
        result.Value.MalfunctioningMachines.Should().Be(1); // Entity 3
        result.Value.EfficiencyPercent.Should().BeApproximately(50.0, 0.1);
        result.Value.ByCategory.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void CompareBases_WithLessThanTwoBases_ShouldReturnFailure()
    {
        var bases = new List<BaseProductionInfo>
        {
            new BaseProductionInfo
            {
                BaseId = "base_1",
                BaseName = "Base 1",
                TotalBuildings = 10,
                OperationalBuildings = 8,
                DisabledBuildings = 1,
                MalfunctioningBuildings = 1,
                Machines = []
            }
        };

        var result = _service.CompareBases(bases, new[] { "base_1" });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least 2 bases");
    }

    // --- Helpers ---

    private static PlacedEntity CreateEntity(
        int id,
        string category,
        double x,
        double y,
        bool isBuilding,
        bool isDisabled = false,
        bool hasMalfunction = false)
    {
        return new PlacedEntity
        {
            PersistentId = id,
            EntityConfigPath = $"/Buildings/{category}/Test",
            EntityType = $"Test{category}",
            EntityCategory = category,
            Position = new WorldPosition { X = x, Y = y, Z = 0 },
            IsBuilding = isBuilding,
            IsDisabled = isDisabled,
            HasMalfunction = hasMalfunction
        };
    }

    private static StarRuptureSave CreateSaveWithSpatial(SpatialData? spatial)
    {
        return new StarRuptureSave
        {
            FilePath = "test.sav",
            SessionName = "Test",
            SaveTimestamp = DateTime.UtcNow,
            PlayTime = TimeSpan.FromHours(10),
            GameState = new StarRuptureGameState
            {
                TutorialCompleted = true,
                PlaytimeDuration = TimeSpan.FromHours(10).TotalSeconds
            },
            Corporations = new CorporationsData
            {
                DataPoints = 5000,
                UnlockedInventorySlots = 10,
                Corporations = []
            },
            Crafting = new CraftingData
            {
                LockedRecipes = [],
                PickedUpItems = [],
                TotalRecipeCount = 180
            },
            EnviroWave = new EnviroWaveData { Wave = "", Stage = "", Progress = 0 },
            Spatial = spatial
        };
    }
}
