namespace GameCompanion.Module.StarRupture.Services;

using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Parses StarRupture .sav files (zlib-compressed JSON).
/// </summary>
public sealed class SaveParserService
{
    // Known corporation internal names to display names
    private static readonly Dictionary<string, string> CorporationDisplayNames = new()
    {
        ["StartingCorporation"] = "Starting Corp",
        ["FutureCorporation"] = "Future Tech",
        ["SelenianCorporation"] = "Selenian",
        ["GriffithsCorporation"] = "Griffiths",
        ["MoonCorporation"] = "Moon Energy",
        ["CleverCorporation"] = "Clever Industries",
        ["FE_FinalCorporation"] = "Final Corp"
    };

    /// <summary>
    /// Parses a StarRupture save file.
    /// </summary>
    public async Task<Result<StarRuptureSave>> ParseSaveAsync(
        string savePath,
        CancellationToken ct = default)
    {
        try
        {
            var sessionName = Path.GetFileName(Path.GetDirectoryName(savePath)) ?? "Unknown";
            var fileInfo = new FileInfo(savePath);

            // Read and decompress
            var jsonData = await DecompressSaveFileAsync(savePath, ct);
            if (jsonData == null)
                return Result<StarRuptureSave>.Failure("Failed to decompress save file");

            // Parse JSON
            using var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;

            // Extract timestamp
            var timestampStr = root.GetProperty("timestamp").GetString() ?? "";
            var saveTimestamp = ParseTimestamp(timestampStr);

            // Extract playtime
            var playTimeSeconds = root.TryGetProperty("worldUnpausedTimeSeconds", out var pts)
                ? pts.GetDouble()
                : 0;

            // Parse itemData sections
            var itemData = root.GetProperty("itemData");

            var gameState = ParseGameState(itemData);
            var corporations = ParseCorporations(itemData);
            var crafting = ParseCrafting(itemData);
            var enviroWave = ParseEnviroWave(itemData);
            var spatial = ParseSpatialData(itemData);

            return Result<StarRuptureSave>.Success(new StarRuptureSave
            {
                FilePath = savePath,
                SessionName = sessionName,
                SaveTimestamp = saveTimestamp,
                PlayTime = TimeSpan.FromSeconds(playTimeSeconds),
                GameState = gameState,
                Corporations = corporations,
                Crafting = crafting,
                EnviroWave = enviroWave,
                Spatial = spatial
            });
        }
        catch (Exception ex)
        {
            return Result<StarRuptureSave>.Failure($"Failed to parse save: {ex.Message}");
        }
    }

    private async Task<string?> DecompressSaveFileAsync(string path, CancellationToken ct)
    {
        var fileBytes = await File.ReadAllBytesAsync(path, ct);

        if (fileBytes.Length < 6)
            return null;

        // Read 4-byte header (uncompressed size, little-endian)
        var uncompressedSize = BitConverter.ToInt32(fileBytes, 0);

        // Verify zlib header (0x78 0x9C)
        if (fileBytes[4] != 0x78 || fileBytes[5] != 0x9C)
            return null;

        // Decompress using DeflateStream (skip zlib 2-byte header)
        using var compressedStream = new MemoryStream(fileBytes, 6, fileBytes.Length - 6);
        using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();

        await deflateStream.CopyToAsync(resultStream, ct);
        var decompressed = resultStream.ToArray();

        return System.Text.Encoding.UTF8.GetString(decompressed);
    }

    private DateTime ParseTimestamp(string timestamp)
    {
        // Format: 20260209113622 (YYYYMMDDHHmmss)
        if (DateTime.TryParseExact(timestamp, "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt;
        }
        return DateTime.MinValue;
    }

    private StarRuptureGameState ParseGameState(JsonElement itemData)
    {
        if (!itemData.TryGetProperty("GameStateData", out var gameState))
            return new StarRuptureGameState();

        return new StarRuptureGameState
        {
            TutorialCompleted = gameState.TryGetProperty("bTutorialCompleted", out var tc) && tc.GetBoolean(),
            PlaytimeDuration = gameState.TryGetProperty("playtimeDuration", out var pd) ? pd.GetDouble() : 0
        };
    }

    private CorporationsData ParseCorporations(JsonElement itemData)
    {
        if (!itemData.TryGetProperty("CrCorporationsOwner", out var corpsOwner))
            return new CorporationsData();

        var dataPoints = corpsOwner.TryGetProperty("dataPoints", out var dp) ? dp.GetInt32() : 0;
        var inventorySlots = corpsOwner.TryGetProperty("unlockedInventorySlotsNumber", out var slots) ? slots.GetInt32() : 0;
        var featureFlags = corpsOwner.TryGetProperty("unlockedFeaturesFlags", out var flags) ? flags.GetInt32() : 0;

        var corporations = new List<CorporationInfo>();
        if (corpsOwner.TryGetProperty("corporations", out var corpsArray))
        {
            foreach (var corp in corpsArray.EnumerateArray())
            {
                var name = corp.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

                // Save file uses "level" (not "currentLevel")
                var level = corp.TryGetProperty("level", out var l) ? l.GetInt32() : 0;

                // Save file uses "reputation" for XP towards next level (not "currentXP")
                var xp = corp.TryGetProperty("reputation", out var r) ? r.GetInt32() : 0;

                corporations.Add(new CorporationInfo
                {
                    Name = name,
                    DisplayName = CorporationDisplayNames.GetValueOrDefault(name, name),
                    CurrentLevel = level,
                    CurrentXP = xp
                });
            }
        }

        return new CorporationsData
        {
            DataPoints = dataPoints,
            UnlockedInventorySlots = inventorySlots,
            UnlockedFeaturesFlags = featureFlags,
            Corporations = corporations
        };
    }

    private CraftingData ParseCrafting(JsonElement itemData)
    {
        if (!itemData.TryGetProperty("CrCraftingRecipeOwner", out var craftOwner))
            return new CraftingData();

        var lockedRecipes = new List<string>();
        if (craftOwner.TryGetProperty("lockedRecipes", out var locked) && locked.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in locked.EnumerateObject())
            {
                // Extract recipe name from path like "/Script/Chimera.CrItemRecipeData'/Game/Chimera/Crafting/CR_Valve.CR_Valve'"
                var recipePath = prop.Name;
                var recipeName = ExtractRecipeName(recipePath);
                lockedRecipes.Add(recipeName);
            }
        }

        var pickedItems = new List<string>();
        if (craftOwner.TryGetProperty("pickedUpItems", out var picked) && picked.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in picked.EnumerateArray())
            {
                var itemPath = item.GetString() ?? "";
                var itemName = ExtractItemName(itemPath);
                pickedItems.Add(itemName);
            }
        }

        // Estimate total recipes (locked + unlocked)
        // Based on wiki data, there are approximately 150-200 craftable items
        const int EstimatedTotalRecipes = 180;
        var unlockedCount = EstimatedTotalRecipes - lockedRecipes.Count;

        return new CraftingData
        {
            LockedRecipes = lockedRecipes,
            PickedUpItems = pickedItems,
            UnlockedRecipeCount = Math.Max(0, unlockedCount),
            TotalRecipeCount = EstimatedTotalRecipes
        };
    }

    private EnviroWaveData ParseEnviroWave(JsonElement itemData)
    {
        if (!itemData.TryGetProperty("EnviroWave", out var wave))
            return new EnviroWaveData();

        return new EnviroWaveData
        {
            Wave = wave.TryGetProperty("wave", out var w) ? w.GetString() ?? "" : "",
            Stage = wave.TryGetProperty("stage", out var s) ? s.GetString() ?? "" : "",
            Progress = wave.TryGetProperty("progress", out var p) ? p.GetDouble() : 0
        };
    }

    private string ExtractRecipeName(string path)
    {
        // Extract "CR_Valve" from "/Script/Chimera.CrItemRecipeData'/Game/Chimera/Crafting/CR_Valve.CR_Valve'"
        var lastSlash = path.LastIndexOf('/');
        if (lastSlash < 0) return path;

        var afterSlash = path[(lastSlash + 1)..];
        var dotIndex = afterSlash.IndexOf('.');
        if (dotIndex > 0)
            afterSlash = afterSlash[..dotIndex];

        // Remove CR_ prefix for display
        if (afterSlash.StartsWith("CR_"))
            afterSlash = afterSlash[3..];

        return afterSlash;
    }

    private string ExtractItemName(string path)
    {
        // Extract "Hydrobulb" from "/Script/Engine.BlueprintGeneratedClass'/Game/Chimera/Items/I_Hydrobulb.I_Hydrobulb_C'"
        var lastSlash = path.LastIndexOf('/');
        if (lastSlash < 0) return path;

        var afterSlash = path[(lastSlash + 1)..];
        var dotIndex = afterSlash.IndexOf('.');
        if (dotIndex > 0)
            afterSlash = afterSlash[..dotIndex];

        // Remove I_ prefix for display
        if (afterSlash.StartsWith("I_"))
            afterSlash = afterSlash[2..];

        return afterSlash;
    }

    // --- Spatial data parsing ---

    // Building path categories based on the game's asset folder structure
    private static readonly Dictionary<string, string> PathCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/Buildings/Hub"] = "Hub",
        ["/Buildings/Interiors"] = "Interior",
        ["/Buildings/Modular"] = "Structure",
        ["/Buildings/Antena"] = "Antenna",
        ["/Buildings/DroneConnections"] = "Logistics",
        ["/Buildings/Defences"] = "Defense",
        ["/Buildings/Extractors"] = "Extraction",
        ["/Buildings/Lights"] = "Utility",
        ["/Buildings/Power"] = "Power",
        ["/Buildings/Production"] = "Production",
        ["/Buildings/Storage"] = "Storage",
        ["/Buildings/Research"] = "Research",
        ["/Infection/"] = "Alien",
        ["/Items/Foundable"] = "Foundable",
        ["/Gatherable"] = "Gatherable",
    };

    private static readonly Regex BuildingStateRegex = new(
        @"CrBuildingStateFragment\(bInitialized=(\w+),bDisabled=(\w+),MalfunctionFlags=(\w+)\)",
        RegexOptions.Compiled);

    private SpatialData? ParseSpatialData(JsonElement itemData)
    {
        try
        {
            var playerPos = ParsePlayerPosition(itemData);
            var entities = ParseEntities(itemData);
            var baseCores = ParseBaseCores(itemData);
            var electricity = ParseElectricity(itemData);
            var logistics = ParseLogistics(itemData);

            return new SpatialData
            {
                PlayerPosition = playerPos,
                Entities = entities,
                BaseCores = baseCores,
                ElectricityNetwork = electricity,
                Logistics = logistics
            };
        }
        catch
        {
            return null;
        }
    }

    private WorldPosition ParsePlayerPosition(JsonElement itemData)
    {
        // Path: itemData.allCharactersBaseSaveData.allPlayersSaveData.<steamId>.survivalData.transform.translation
        if (!itemData.TryGetProperty("allCharactersBaseSaveData", out var charData))
            return new WorldPosition();

        if (!charData.TryGetProperty("allPlayersSaveData", out var playersData))
            return new WorldPosition();

        // Iterate player entries (keyed by Steam ID)
        foreach (var player in playersData.EnumerateObject())
        {
            if (!player.Value.TryGetProperty("survivalData", out var survival))
                continue;
            if (!survival.TryGetProperty("transform", out var transform))
                continue;
            if (!transform.TryGetProperty("translation", out var translation))
                continue;

            return ParsePosition(translation);
        }

        return new WorldPosition();
    }

    private static WorldPosition ParsePosition(JsonElement element)
    {
        return new WorldPosition
        {
            X = element.TryGetProperty("x", out var x) ? x.GetDouble() : 0,
            Y = element.TryGetProperty("y", out var y) ? y.GetDouble() : 0,
            Z = element.TryGetProperty("z", out var z) ? z.GetDouble() : 0
        };
    }

    private List<PlacedEntity> ParseEntities(JsonElement itemData)
    {
        var entities = new List<PlacedEntity>();

        if (!itemData.TryGetProperty("Mass", out var mass))
            return entities;

        if (!mass.TryGetProperty("entities", out var entitiesObj))
            return entities;

        foreach (var entityProp in entitiesObj.EnumerateObject())
        {
            var entity = entityProp.Value;
            var idStr = entityProp.Name; // e.g., "(ID=232)"

            if (!TryParseEntityId(idStr, out var persistentId))
                continue;

            if (!entity.TryGetProperty("spawnData", out var spawnData))
                continue;

            var configPath = spawnData.TryGetProperty("entityConfigDataPath", out var path)
                ? path.GetString() ?? ""
                : "";

            if (string.IsNullOrEmpty(configPath))
                continue;

            var position = new WorldPosition();
            if (spawnData.TryGetProperty("transform", out var transform) &&
                transform.TryGetProperty("translation", out var translation))
            {
                position = ParsePosition(translation);
            }

            var entityType = ExtractEntityType(configPath);
            var category = ClassifyEntityCategory(configPath);
            var isBuilding = configPath.Contains("/Buildings/");

            // Parse building state from fragmentValues
            var isDisabled = false;
            var hasMalfunction = false;
            if (entity.TryGetProperty("fragmentValues", out var fragments) &&
                fragments.ValueKind == JsonValueKind.Array)
            {
                foreach (var frag in fragments.EnumerateArray())
                {
                    var fragStr = frag.GetString() ?? "";
                    var match = BuildingStateRegex.Match(fragStr);
                    if (match.Success)
                    {
                        isDisabled = match.Groups[2].Value == "True";
                        hasMalfunction = match.Groups[3].Value != "None";
                        break;
                    }
                }
            }

            entities.Add(new PlacedEntity
            {
                PersistentId = persistentId,
                EntityConfigPath = configPath,
                EntityType = entityType,
                EntityCategory = category,
                Position = position,
                IsBuilding = isBuilding,
                IsDisabled = isDisabled,
                HasMalfunction = hasMalfunction
            });
        }

        return entities;
    }

    private List<BaseCoreData> ParseBaseCores(JsonElement itemData)
    {
        var cores = new List<BaseCoreData>();

        if (!itemData.TryGetProperty("BaseCoreReplicationHelperSaveData", out var baseCoreHelper))
            return cores;

        if (!baseCoreHelper.TryGetProperty("baseCoreSaveData", out var coreArray) ||
            coreArray.ValueKind != JsonValueKind.Array)
            return cores;

        foreach (var core in coreArray.EnumerateArray())
        {
            var entityId = 0;
            if (core.TryGetProperty("baseCore", out var baseCore) &&
                baseCore.TryGetProperty("iD", out var id))
            {
                entityId = id.GetInt32();
            }

            // Skip sentinel values
            if (entityId <= 0 || entityId == unchecked((int)0xFFFFFFFF))
                continue;

            cores.Add(new BaseCoreData
            {
                EntityId = entityId,
                UpgradeLevel = core.TryGetProperty("upgradeLevel", out var lvl) ? lvl.GetInt32() : 0,
                HasInfectionSphere = core.TryGetProperty("infectionSphere", out var inf) && inf.GetBoolean()
            });
        }

        return cores;
    }

    private ElectricityNetworkData ParseElectricity(JsonElement itemData)
    {
        if (!itemData.TryGetProperty("Mass", out var mass))
            return new ElectricityNetworkData { Nodes = [], Subgraphs = [] };

        if (!mass.TryGetProperty("electricitySubsystemState", out var elecState))
            return new ElectricityNetworkData { Nodes = [], Subgraphs = [] };

        var nodes = new List<ElectricityNode>();
        if (elecState.TryGetProperty("nodeData", out var nodeData))
        {
            foreach (var nodeProp in nodeData.EnumerateObject())
            {
                var node = nodeProp.Value;

                var entityId = 0;
                if (node.TryGetProperty("handle", out var handle) &&
                    handle.TryGetProperty("iD", out var id))
                {
                    entityId = id.GetInt32();
                }

                var subgraphId = 0;
                if (node.TryGetProperty("subGraph", out var sg) &&
                    sg.TryGetProperty("subGraphId", out var sgId))
                {
                    subgraphId = sgId.GetInt32();
                }

                var neighbours = new List<int>();
                if (node.TryGetProperty("neighbourData", out var neighbourArray) &&
                    neighbourArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var neighbour in neighbourArray.EnumerateArray())
                    {
                        if (neighbour.TryGetProperty("neighbour", out var n) &&
                            n.TryGetProperty("iD", out var nId))
                        {
                            neighbours.Add(nId.GetInt32());
                        }
                    }
                }

                nodes.Add(new ElectricityNode
                {
                    EntityId = entityId,
                    SubgraphId = subgraphId,
                    NeighbourIds = neighbours
                });
            }
        }

        var subgraphs = new List<ElectricitySubgraph>();
        if (elecState.TryGetProperty("subgraphData", out var subgraphData))
        {
            foreach (var sgProp in subgraphData.EnumerateObject())
            {
                if (int.TryParse(sgProp.Name, out var sgId))
                {
                    subgraphs.Add(new ElectricitySubgraph { SubgraphId = sgId });
                }
            }
        }

        return new ElectricityNetworkData
        {
            Nodes = nodes,
            Subgraphs = subgraphs
        };
    }

    private LogisticsData ParseLogistics(JsonElement itemData)
    {
        if (!itemData.TryGetProperty("Mass", out var mass))
            return new LogisticsData { Requests = [] };

        if (!mass.TryGetProperty("logisticsRequestSubsystemState", out var logState))
            return new LogisticsData { Requests = [] };

        var requests = new List<LogisticsRequest>();
        if (logState.TryGetProperty("requestData", out var requestData))
        {
            foreach (var reqProp in requestData.EnumerateObject())
            {
                var req = reqProp.Value;

                var requestId = 0;
                if (req.TryGetProperty("uId", out var uid) &&
                    uid.TryGetProperty("iD", out var id))
                {
                    requestId = id.GetInt32();
                }

                var requesterEntityId = 0;
                if (req.TryGetProperty("requesterEntity", out var requester) &&
                    requester.TryGetProperty("iD", out var rId))
                {
                    requesterEntityId = rId.GetInt32();
                }

                var wantedItem = "";
                var wantedCount = 0;
                if (req.TryGetProperty("wantedItem", out var wanted))
                {
                    var itemPath = wanted.TryGetProperty("itemDataBase", out var idb) ? idb.GetString() ?? "" : "";
                    wantedItem = ExtractItemName(itemPath);
                    wantedCount = wanted.TryGetProperty("count", out var cnt) ? cnt.GetInt32() : 0;
                }

                requests.Add(new LogisticsRequest
                {
                    RequestId = requestId,
                    RequesterEntityId = requesterEntityId,
                    RequestType = req.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                    WantedItem = wantedItem,
                    WantedCount = wantedCount,
                    Priority = req.TryGetProperty("currentPriority", out var p) ? p.GetString() ?? "" : "",
                    IsAborted = req.TryGetProperty("bAborted", out var a) && a.GetBoolean()
                });
            }
        }

        return new LogisticsData { Requests = requests };
    }

    private static bool TryParseEntityId(string idStr, out int id)
    {
        // Parse "(ID=232)" format
        id = 0;
        if (!idStr.StartsWith("(ID=") || !idStr.EndsWith(")"))
            return false;

        return int.TryParse(idStr[4..^1], out id);
    }

    private static string ExtractEntityType(string configPath)
    {
        // Extract "CloningBed_NonDeconstructible" from
        // "/Game/Chimera/Buildings/Interiors/CloningBed/CloningBed_NonDeconstructible/DA_CloningBed_NonDeconstructible.DA_..."
        var lastSlash = configPath.LastIndexOf('/');
        if (lastSlash < 0) return configPath;

        var afterSlash = configPath[(lastSlash + 1)..];
        var dotIndex = afterSlash.IndexOf('.');
        if (dotIndex > 0)
            afterSlash = afterSlash[..dotIndex];

        // Remove DA_ prefix
        if (afterSlash.StartsWith("DA_"))
            afterSlash = afterSlash[3..];

        return afterSlash;
    }

    private static string ClassifyEntityCategory(string configPath)
    {
        foreach (var (pathFragment, category) in PathCategoryMap)
        {
            if (configPath.Contains(pathFragment, StringComparison.OrdinalIgnoreCase))
                return category;
        }
        return "Other";
    }
}
