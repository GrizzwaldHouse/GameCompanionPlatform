using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class MapDataServiceTests
{
    private readonly MapDataService _service = new();

    [Fact]
    public void BuildMapData_WithNoSpatialData_ShouldReturnFailure()
    {
        var save = CreateSaveWithSpatial(null);

        var result = _service.BuildMapData(save);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void BuildMapData_WithEmptyEntities_ShouldReturnEmptyMap()
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

        var result = _service.BuildMapData(save);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bases.Should().BeEmpty();
        result.Value.TotalBuildingCount.Should().Be(0);
    }

    [Fact]
    public void BuildMapData_ShouldIncludePlayerPosition()
    {
        var save = CreateSaveWithSpatial(new SpatialData
        {
            PlayerPosition = new WorldPosition { X = 100, Y = 200, Z = 0 },
            Entities = [],
            BaseCores = [],
            ElectricityNetwork = new ElectricityNetworkData
            {
                Nodes = [],
                Subgraphs = []
            },
            Logistics = new LogisticsData { Requests = [] }
        });

        var result = _service.BuildMapData(save);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlayerPosition.X.Should().Be(100);
        result.Value.PlayerPosition.Y.Should().Be(200);
    }

    [Fact]
    public void BuildMapData_WithBaseCore_ShouldCreateBaseCluster()
    {
        var save = CreateSaveWithSpatial(new SpatialData
        {
            PlayerPosition = new WorldPosition { X = 0, Y = 0, Z = 0 },
            Entities = [
                CreateEntity(1, "Hub", 1000, 2000, isBuilding: true),
                CreateEntity(2, "Production", 1100, 2100, isBuilding: true),
                CreateEntity(3, "Storage", 1200, 2050, isBuilding: true)
            ],
            BaseCores = [
                new BaseCoreData { EntityId = 1, UpgradeLevel = 2, HasInfectionSphere = false }
            ],
            ElectricityNetwork = new ElectricityNetworkData
            {
                Nodes = [],
                Subgraphs = []
            },
            Logistics = new LogisticsData { Requests = [] }
        });

        var result = _service.BuildMapData(save);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bases.Should().NotBeEmpty();
        result.Value.TotalBuildingCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildMapData_WorldBounds_ShouldContainAllEntities()
    {
        var save = CreateSaveWithSpatial(new SpatialData
        {
            PlayerPosition = new WorldPosition { X = 500, Y = 500, Z = 0 },
            Entities = [
                CreateEntity(1, "Hub", -1000, -2000, isBuilding: true),
                CreateEntity(2, "Hub", 3000, 4000, isBuilding: true)
            ],
            BaseCores = [
                new BaseCoreData { EntityId = 1, UpgradeLevel = 1, HasInfectionSphere = false },
                new BaseCoreData { EntityId = 2, UpgradeLevel = 1, HasInfectionSphere = false }
            ],
            ElectricityNetwork = new ElectricityNetworkData
            {
                Nodes = [],
                Subgraphs = []
            },
            Logistics = new LogisticsData { Requests = [] }
        });

        var result = _service.BuildMapData(save);

        result.IsSuccess.Should().BeTrue();
        var bounds = result.Value!.WorldBounds;
        bounds.MinX.Should().BeLessThanOrEqualTo(-1000);
        bounds.MaxX.Should().BeGreaterThanOrEqualTo(3000);
        bounds.MinY.Should().BeLessThanOrEqualTo(-2000);
        bounds.MaxY.Should().BeGreaterThanOrEqualTo(4000);
        bounds.Width.Should().BePositive();
        bounds.Height.Should().BePositive();
    }

    // --- Helpers ---

    private static PlacedEntity CreateEntity(int id, string category, double x, double y, bool isBuilding)
    {
        return new PlacedEntity
        {
            PersistentId = id,
            EntityConfigPath = $"/Buildings/{category}/Test",
            EntityType = $"Test{category}",
            EntityCategory = category,
            Position = new WorldPosition { X = x, Y = y, Z = 0 },
            IsBuilding = isBuilding,
            IsDisabled = false,
            HasMalfunction = false
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
