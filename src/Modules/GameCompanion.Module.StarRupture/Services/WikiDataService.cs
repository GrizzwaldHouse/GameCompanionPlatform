namespace GameCompanion.Module.StarRupture.Services;

using System.Net.Http;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for fetching and caching wiki data from starrupture.tools.
/// </summary>
public sealed class WikiDataService : IDisposable
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _cacheDirectory;
    private WikiCache? _cache;
    private bool _disposed;

    public WikiDataService()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarRuptureCompanion",
            "Cache");

        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Gets cached wiki data, refreshing if stale.
    /// </summary>
    public async Task<Result<WikiCache>> GetCachedDataAsync(
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        try
        {
            var cacheFile = Path.Combine(_cacheDirectory, "wiki_cache.json");

            // Try to load existing cache
            if (!forceRefresh && File.Exists(cacheFile))
            {
                var json = await File.ReadAllTextAsync(cacheFile, ct);
                _cache = JsonSerializer.Deserialize<WikiCache>(json);

                if (_cache != null && !IsCacheStale(_cache))
                {
                    return Result<WikiCache>.Success(_cache);
                }
            }

            // Refresh cache
            var refreshResult = await RefreshCacheAsync(ct);
            if (refreshResult.IsFailure)
            {
                // Return stale cache if available
                if (_cache != null)
                {
                    return Result<WikiCache>.Success(_cache);
                }
                return Result<WikiCache>.Failure(refreshResult.Error!);
            }

            // Save refreshed cache
            _cache = refreshResult.Value;
            var cacheJson = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(cacheFile, cacheJson, ct);

            return Result<WikiCache>.Success(_cache);
        }
        catch (Exception ex)
        {
            return Result<WikiCache>.Failure($"Failed to get wiki data: {ex.Message}");
        }
    }

    private bool IsCacheStale(WikiCache cache)
    {
        // Cache is stale after 7 days
        return DateTime.UtcNow - cache.LastUpdated > TimeSpan.FromDays(7);
    }

    private Task<Result<WikiCache>> RefreshCacheAsync(CancellationToken ct)
    {
        try
        {
            // Note: starrupture.tools doesn't have a public API, so we provide
            // embedded data based on known game content. In a production app,
            // you would scrape or use an API if available.

            var cache = new WikiCache
            {
                LastUpdated = DateTime.UtcNow,
                Items = GetKnownItems(),
                Buildings = GetKnownBuildings(),
                Blueprints = GetKnownBlueprints(),
                Corporations = GetKnownCorporations()
            };

            return Task.FromResult(Result<WikiCache>.Success(cache));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<WikiCache>.Failure($"Failed to refresh cache: {ex.Message}"));
        }
    }

    private static List<WikiItem> GetKnownItems()
    {
        return new List<WikiItem>
        {
            new WikiItem { Id = "Hydrobulb", Name = "Hydrobulb", Category = "Resource", Description = "A water-storing plant" },
            new WikiItem { Id = "CalciumOre", Name = "Calcium Ore", Category = "Resource", Description = "Raw mineral used in production" },
            new WikiItem { Id = "IronOre", Name = "Iron Ore", Category = "Resource", Description = "Basic metal ore" },
            new WikiItem { Id = "CopperOre", Name = "Copper Ore", Category = "Resource", Description = "Conductive metal ore" },
            new WikiItem { Id = "CrystalShard", Name = "Crystal Shard", Category = "Resource", Description = "Energy crystal fragment" },
            new WikiItem { Id = "PlasticSheet", Name = "Plastic Sheet", Category = "Crafted", Description = "Processed plastic material" },
            new WikiItem { Id = "IronPlate", Name = "Iron Plate", Category = "Crafted", Description = "Processed iron" },
            new WikiItem { Id = "CopperWire", Name = "Copper Wire", Category = "Crafted", Description = "Conductive wiring" },
            new WikiItem { Id = "CircuitBoard", Name = "Circuit Board", Category = "Crafted", Description = "Electronic component" },
            new WikiItem { Id = "Battery", Name = "Battery", Category = "Crafted", Description = "Power storage unit" },
            new WikiItem { Id = "Motor", Name = "Motor", Category = "Crafted", Description = "Mechanical power source" },
            new WikiItem { Id = "Stator", Name = "Stator", Category = "Crafted", Description = "Advanced motor component" },
            new WikiItem { Id = "Valve", Name = "Valve", Category = "Crafted", Description = "Fluid control device" },
            new WikiItem { Id = "Gear", Name = "Gear", Category = "Crafted", Description = "Mechanical transmission part" }
        };
    }

    private static List<WikiBuilding> GetKnownBuildings()
    {
        return new List<WikiBuilding>
        {
            new WikiBuilding { Id = "SolarPanel", Name = "Solar Panel", Category = "Power", PowerOutput = 50 },
            new WikiBuilding { Id = "WindTurbine", Name = "Wind Turbine", Category = "Power", PowerOutput = 100 },
            new WikiBuilding { Id = "Generator", Name = "Generator", Category = "Power", PowerOutput = 200 },
            new WikiBuilding { Id = "Furnace", Name = "Furnace", Category = "Production", PowerOutput = 0 },
            new WikiBuilding { Id = "Assembler", Name = "Assembler", Category = "Production", PowerOutput = 0 },
            new WikiBuilding { Id = "Constructor", Name = "Constructor", Category = "Production", PowerOutput = 0 },
            new WikiBuilding { Id = "StorageBox", Name = "Storage Box", Category = "Storage", PowerOutput = 0 },
            new WikiBuilding { Id = "Conveyor", Name = "Conveyor", Category = "Logistics", PowerOutput = 0 },
            new WikiBuilding { Id = "Splitter", Name = "Splitter", Category = "Logistics", PowerOutput = 0 },
            new WikiBuilding { Id = "Merger", Name = "Merger", Category = "Logistics", PowerOutput = 0 }
        };
    }

    private static List<WikiBlueprint> GetKnownBlueprints()
    {
        return new List<WikiBlueprint>
        {
            new WikiBlueprint { Id = "BP_SolarPanel", Name = "Solar Panel", Location = "Starting Area", Corporation = null },
            new WikiBlueprint { Id = "BP_WindTurbine", Name = "Wind Turbine", Location = "Windy Plains", Corporation = "Moon Energy" },
            new WikiBlueprint { Id = "BP_Generator", Name = "Generator", Location = "Power Plant POI", Corporation = "Future Tech" },
            new WikiBlueprint { Id = "BP_Furnace", Name = "Furnace", Location = "Starting Area", Corporation = null },
            new WikiBlueprint { Id = "BP_Assembler", Name = "Assembler", Location = "Factory Ruins", Corporation = "Clever Industries" },
            new WikiBlueprint { Id = "BP_Stator", Name = "Stator", Location = "Lemon Souls POI", Corporation = "Griffiths" },
            new WikiBlueprint { Id = "BP_Motor", Name = "Motor", Location = "Tech Lab", Corporation = "Future Tech" },
            new WikiBlueprint { Id = "BP_CircuitBoard", Name = "Circuit Board", Location = "Electronics Plant", Corporation = "Clever Industries" }
        };
    }

    private static List<WikiCorporation> GetKnownCorporations()
    {
        return new List<WikiCorporation>
        {
            new WikiCorporation
            {
                Id = "MoonCorporation",
                Name = "Moon Energy",
                Description = "Specializes in power generation. Level 3 unlocks the map.",
                Rewards = new List<string> { "Map Access (Lvl 3)", "Power Blueprints", "Energy Cells" }
            },
            new WikiCorporation
            {
                Id = "FutureCorporation",
                Name = "Future Tech",
                Description = "Advanced technology corporation.",
                Rewards = new List<string> { "High-tech Blueprints", "Advanced Components" }
            },
            new WikiCorporation
            {
                Id = "GriffithsCorporation",
                Name = "Griffiths",
                Description = "Heavy industry and machinery.",
                Rewards = new List<string> { "Industrial Blueprints", "Heavy Machinery" }
            },
            new WikiCorporation
            {
                Id = "CleverCorporation",
                Name = "Clever Industries",
                Description = "Electronics and automation.",
                Rewards = new List<string> { "Automation Blueprints", "Electronic Components" }
            },
            new WikiCorporation
            {
                Id = "SelenianCorporation",
                Name = "Selenian",
                Description = "Mining and resources.",
                Rewards = new List<string> { "Mining Blueprints", "Ore Processing" }
            }
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
