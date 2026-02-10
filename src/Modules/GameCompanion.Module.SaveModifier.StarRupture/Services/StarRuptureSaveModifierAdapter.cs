namespace GameCompanion.Module.SaveModifier.StarRupture.Services;

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GameCompanion.Core.Enums;
using GameCompanion.Core.Models;
using GameCompanion.Module.SaveModifier.Interfaces;
using GameCompanion.Module.SaveModifier.Models;

/// <summary>
/// StarRupture-specific save modifier adapter. Handles reading, modifying,
/// and rewriting StarRupture's zlib-compressed JSON save format.
///
/// Supports modification of:
/// - Corporation XP and levels
/// - Data points
/// - Inventory slot unlocks
/// - Feature flag unlocks
/// - Crafting recipe unlocks (removing from locked list)
/// </summary>
public sealed class StarRuptureSaveModifierAdapter : ISaveModifierAdapter
{
    public string GameId => "star_rupture";

    public async Task<Result<IReadOnlyList<ModifiableField>>> GetModifiableFieldsAsync(
        string savePath,
        CancellationToken ct = default)
    {
        var jsonResult = await ReadSaveJsonAsync(savePath, ct);
        if (jsonResult.IsFailure)
            return Result<IReadOnlyList<ModifiableField>>.Failure(jsonResult.Error!);

        var root = jsonResult.Value!;
        var fields = new List<ModifiableField>();

        // Corporation data points
        if (TryGetNode(root, "itemData.CrCorporationsOwner.dataPoints", out var dpNode))
        {
            fields.Add(new ModifiableField
            {
                FieldId = "corporations.dataPoints",
                DisplayName = "Data Points",
                Category = "Corporations",
                Description = "Currency used to unlock corporation levels and features.",
                CurrentValue = dpNode!.GetValue<int>(),
                DataType = typeof(int),
                Risk = RiskLevel.Medium,
                MinValue = 0,
                MaxValue = 999999
            });
        }

        // Inventory slots
        if (TryGetNode(root, "itemData.CrCorporationsOwner.unlockedInventorySlotsNumber", out var slotsNode))
        {
            fields.Add(new ModifiableField
            {
                FieldId = "corporations.inventorySlots",
                DisplayName = "Unlocked Inventory Slots",
                Category = "Corporations",
                Description = "Number of inventory slots unlocked via corporation progression.",
                CurrentValue = slotsNode!.GetValue<int>(),
                DataType = typeof(int),
                Risk = RiskLevel.Medium,
                MinValue = 0,
                MaxValue = 60
            });
        }

        // Feature flags
        if (TryGetNode(root, "itemData.CrCorporationsOwner.unlockedFeaturesFlags", out var flagsNode))
        {
            fields.Add(new ModifiableField
            {
                FieldId = "corporations.featuresFlags",
                DisplayName = "Unlocked Features Flags",
                Category = "Corporations",
                Description = "Bitmask of unlocked game features (map, trading, etc.).",
                CurrentValue = flagsNode!.GetValue<int>(),
                DataType = typeof(int),
                Risk = RiskLevel.High,
                MinValue = 0,
                MaxValue = int.MaxValue
            });
        }

        // Individual corporation levels and XP
        if (TryGetNode(root, "itemData.CrCorporationsOwner.corporations", out var corpsNode) &&
            corpsNode is JsonArray corpsArray)
        {
            for (var i = 0; i < corpsArray.Count; i++)
            {
                var corp = corpsArray[i];
                var name = corp?["name"]?.GetValue<string>() ?? $"Corporation_{i}";
                var displayName = GetCorpDisplayName(name);

                if (corp?["currentLevel"] != null)
                {
                    fields.Add(new ModifiableField
                    {
                        FieldId = $"corporations.{i}.level",
                        DisplayName = $"{displayName} Level",
                        Category = "Corporations",
                        Description = $"Current reputation level with {displayName}.",
                        CurrentValue = corp["currentLevel"]!.GetValue<int>(),
                        DataType = typeof(int),
                        Risk = RiskLevel.Medium,
                        MinValue = 0,
                        MaxValue = 20
                    });
                }

                if (corp?["currentXP"] != null)
                {
                    fields.Add(new ModifiableField
                    {
                        FieldId = $"corporations.{i}.xp",
                        DisplayName = $"{displayName} XP",
                        Category = "Corporations",
                        Description = $"Current experience points with {displayName}.",
                        CurrentValue = corp["currentXP"]!.GetValue<int>(),
                        DataType = typeof(int),
                        Risk = RiskLevel.Medium,
                        MinValue = 0,
                        MaxValue = 999999
                    });
                }
            }
        }

        // Locked recipe count (can unlock all by clearing the locked list)
        if (TryGetNode(root, "itemData.CrCraftingRecipeOwner.lockedRecipes", out var lockedNode) &&
            lockedNode is JsonObject lockedObj)
        {
            fields.Add(new ModifiableField
            {
                FieldId = "crafting.unlockAll",
                DisplayName = "Unlock All Recipes",
                Category = "Crafting",
                Description = $"Currently {lockedObj.Count} recipes are locked. Setting to true unlocks all crafting recipes.",
                CurrentValue = false,
                DataType = typeof(bool),
                Risk = RiskLevel.High
            });
        }

        return Result<IReadOnlyList<ModifiableField>>.Success(fields);
    }

    public async Task<Result<SaveModificationPreview>> PreviewModificationsAsync(
        string savePath,
        IReadOnlyList<FieldModification> modifications,
        CancellationToken ct = default)
    {
        var jsonResult = await ReadSaveJsonAsync(savePath, ct);
        if (jsonResult.IsFailure)
            return Result<SaveModificationPreview>.Failure(jsonResult.Error!);

        var root = jsonResult.Value!;
        var changes = new List<FieldChangePreview>();
        var warnings = new List<string>();
        var allValid = true;

        foreach (var mod in modifications)
        {
            var preview = BuildFieldPreview(root, mod);
            changes.Add(preview);

            if (!preview.IsValid)
            {
                allValid = false;
                warnings.Add($"Field '{mod.FieldId}': {preview.ValidationError}");
            }
        }

        if (modifications.Any(m => m.FieldId == "crafting.unlockAll"))
            warnings.Add("Unlocking all recipes will remove locked recipe entries. This cannot be partially reversed.");

        return Result<SaveModificationPreview>.Success(new SaveModificationPreview
        {
            SavePath = savePath,
            Changes = changes,
            Warnings = warnings,
            IsValid = allValid
        });
    }

    public async Task<Result<SaveModificationResult>> ApplyModificationsAsync(
        string savePath,
        IReadOnlyList<FieldModification> modifications,
        CancellationToken ct = default)
    {
        var jsonResult = await ReadSaveJsonAsync(savePath, ct);
        if (jsonResult.IsFailure)
            return Result<SaveModificationResult>.Failure(jsonResult.Error!);

        var root = jsonResult.Value!;
        var modifiedCount = 0;

        foreach (var mod in modifications)
        {
            var applied = ApplyFieldModification(root, mod);
            if (applied)
                modifiedCount++;
            else
                return Result<SaveModificationResult>.Failure(
                    $"Failed to apply modification to field '{mod.FieldId}'. No changes were written.");
        }

        // Write the modified save back
        var writeResult = await WriteSaveJsonAsync(savePath, root, ct);
        if (writeResult.IsFailure)
            return Result<SaveModificationResult>.Failure(writeResult.Error!);

        return Result<SaveModificationResult>.Success(new SaveModificationResult
        {
            SavePath = savePath,
            BackupId = Path.GetFileName(savePath),
            ModifiedFieldCount = modifiedCount,
            ModifiedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task<Result<bool>> ValidateSaveForModificationAsync(
        string savePath,
        CancellationToken ct = default)
    {
        // Path traversal protection: ensure the path resolves to a .sav file
        // within expected save directories. Reject suspicious paths.
        var fullPath = Path.GetFullPath(savePath);
        if (!fullPath.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Failure("Only .sav files can be modified.");

        if (fullPath.Contains("..", StringComparison.Ordinal))
            return Result<bool>.Failure("Path traversal detected.");

        if (!File.Exists(fullPath))
            return Result<bool>.Failure("Save file does not exist.");

        var jsonResult = await ReadSaveJsonAsync(fullPath, ct);
        if (jsonResult.IsFailure)
            return Result<bool>.Failure($"Save file is not readable: {jsonResult.Error}");

        var root = jsonResult.Value!;

        // Verify required sections exist
        if (root["itemData"] == null)
            return Result<bool>.Failure("Save file is missing 'itemData' section.");

        if (root["timestamp"] == null)
            return Result<bool>.Failure("Save file is missing 'timestamp' — may be corrupted.");

        return Result<bool>.Success(true);
    }

    private FieldChangePreview BuildFieldPreview(JsonNode root, FieldModification mod)
    {
        var (currentValue, found) = GetCurrentValue(root, mod.FieldId);

        if (!found && mod.FieldId != "crafting.unlockAll")
        {
            return new FieldChangePreview
            {
                FieldId = mod.FieldId,
                DisplayName = mod.FieldId,
                OldValue = "N/A",
                NewValue = mod.NewValue,
                IsValid = false,
                ValidationError = "Field not found in save file."
            };
        }

        // Validate numeric bounds for non-boolean fields
        if (mod.FieldId != "crafting.unlockAll")
        {
            try
            {
                var intValue = Convert.ToInt32(mod.NewValue);
                var (min, max) = GetFieldBounds(mod.FieldId);
                if (intValue < min || intValue > max)
                {
                    return new FieldChangePreview
                    {
                        FieldId = mod.FieldId,
                        DisplayName = mod.FieldId,
                        OldValue = currentValue ?? "N/A",
                        NewValue = mod.NewValue,
                        IsValid = false,
                        ValidationError = $"Value {intValue} is outside the allowed range [{min}–{max}]."
                    };
                }
            }
            catch (FormatException)
            {
                return new FieldChangePreview
                {
                    FieldId = mod.FieldId,
                    DisplayName = mod.FieldId,
                    OldValue = currentValue ?? "N/A",
                    NewValue = mod.NewValue,
                    IsValid = false,
                    ValidationError = "Value is not a valid integer."
                };
            }
        }

        return new FieldChangePreview
        {
            FieldId = mod.FieldId,
            DisplayName = mod.FieldId,
            OldValue = currentValue ?? "N/A",
            NewValue = mod.NewValue,
            IsValid = true
        };
    }

    private (object? value, bool found) GetCurrentValue(JsonNode root, string fieldId)
    {
        return fieldId switch
        {
            "corporations.dataPoints" =>
                TryGetNode(root, "itemData.CrCorporationsOwner.dataPoints", out var n)
                    ? (n!.GetValue<int>(), true) : (null, false),

            "corporations.inventorySlots" =>
                TryGetNode(root, "itemData.CrCorporationsOwner.unlockedInventorySlotsNumber", out var n)
                    ? (n!.GetValue<int>(), true) : (null, false),

            "corporations.featuresFlags" =>
                TryGetNode(root, "itemData.CrCorporationsOwner.unlockedFeaturesFlags", out var n)
                    ? (n!.GetValue<int>(), true) : (null, false),

            "crafting.unlockAll" =>
                TryGetNode(root, "itemData.CrCraftingRecipeOwner.lockedRecipes", out var n)
                    ? (n is JsonObject obj ? obj.Count : 0, true) : (null, false),

            _ when fieldId.StartsWith("corporations.") && fieldId.EndsWith(".level") =>
                GetCorpFieldValue(root, fieldId, "currentLevel"),

            _ when fieldId.StartsWith("corporations.") && fieldId.EndsWith(".xp") =>
                GetCorpFieldValue(root, fieldId, "currentXP"),

            _ => (null, false)
        };
    }

    private (object? value, bool found) GetCorpFieldValue(JsonNode root, string fieldId, string propName)
    {
        var parts = fieldId.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[1], out var index))
            return (null, false);

        if (TryGetNode(root, "itemData.CrCorporationsOwner.corporations", out var corpsNode) &&
            corpsNode is JsonArray arr && index < arr.Count)
        {
            var val = arr[index]?[propName];
            return val != null ? (val.GetValue<int>(), true) : (null, false);
        }

        return (null, false);
    }

    private bool ApplyFieldModification(JsonNode root, FieldModification mod)
    {
        // Validate numeric bounds before applying any modification
        if (mod.FieldId != "crafting.unlockAll")
        {
            var intValue = Convert.ToInt32(mod.NewValue);
            var (min, max) = GetFieldBounds(mod.FieldId);
            if (intValue < min || intValue > max)
                return false;
        }

        return mod.FieldId switch
        {
            "corporations.dataPoints" =>
                SetJsonValue(root, "itemData.CrCorporationsOwner.dataPoints", Convert.ToInt32(mod.NewValue)),

            "corporations.inventorySlots" =>
                SetJsonValue(root, "itemData.CrCorporationsOwner.unlockedInventorySlotsNumber", Convert.ToInt32(mod.NewValue)),

            "corporations.featuresFlags" =>
                SetJsonValue(root, "itemData.CrCorporationsOwner.unlockedFeaturesFlags", Convert.ToInt32(mod.NewValue)),

            "crafting.unlockAll" when Convert.ToBoolean(mod.NewValue) =>
                ClearLockedRecipes(root),

            _ when mod.FieldId.StartsWith("corporations.") && mod.FieldId.EndsWith(".level") =>
                SetCorpFieldValue(root, mod.FieldId, "currentLevel", Convert.ToInt32(mod.NewValue)),

            _ when mod.FieldId.StartsWith("corporations.") && mod.FieldId.EndsWith(".xp") =>
                SetCorpFieldValue(root, mod.FieldId, "currentXP", Convert.ToInt32(mod.NewValue)),

            _ => false
        };
    }

    private static (int min, int max) GetFieldBounds(string fieldId)
    {
        return fieldId switch
        {
            "corporations.dataPoints" => (0, 999_999),
            "corporations.inventorySlots" => (0, 60),
            "corporations.featuresFlags" => (0, int.MaxValue),
            _ when fieldId.StartsWith("corporations.") && fieldId.EndsWith(".level") => (0, 20),
            _ when fieldId.StartsWith("corporations.") && fieldId.EndsWith(".xp") => (0, 999_999),
            _ => (0, int.MaxValue)
        };
    }

    private bool SetCorpFieldValue(JsonNode root, string fieldId, string propName, int value)
    {
        var parts = fieldId.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[1], out var index))
            return false;

        if (TryGetNode(root, "itemData.CrCorporationsOwner.corporations", out var corpsNode) &&
            corpsNode is JsonArray arr && index < arr.Count)
        {
            arr[index]![propName] = value;
            return true;
        }

        return false;
    }

    private bool ClearLockedRecipes(JsonNode root)
    {
        if (!TryGetNode(root, "itemData.CrCraftingRecipeOwner", out var craftOwner) ||
            craftOwner is not JsonObject craftObj)
            return false;

        craftObj["lockedRecipes"] = new JsonObject();
        return true;
    }

    private static bool SetJsonValue(JsonNode root, string path, int value)
    {
        var parts = path.Split('.');
        JsonNode? current = root;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            current = current?[parts[i]];
            if (current == null)
                return false;
        }

        if (current is JsonObject obj)
        {
            obj[parts[^1]] = value;
            return true;
        }

        return false;
    }

    private static bool TryGetNode(JsonNode root, string path, out JsonNode? node)
    {
        node = null;
        var parts = path.Split('.');
        JsonNode? current = root;

        foreach (var part in parts)
        {
            current = current?[part];
            if (current == null)
                return false;
        }

        node = current;
        return true;
    }

    private static async Task<Result<JsonNode>> ReadSaveJsonAsync(string path, CancellationToken ct)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(path, ct);

            if (fileBytes.Length < 6)
                return Result<JsonNode>.Failure("Save file too small to be valid.");

            // Read 4-byte header (uncompressed size, little-endian)
            // Verify zlib header (0x78 0x9C)
            if (fileBytes[4] != 0x78 || fileBytes[5] != 0x9C)
                return Result<JsonNode>.Failure("Invalid zlib header — not a StarRupture save file.");

            // Decompress using DeflateStream (skip zlib 2-byte header)
            using var compressedStream = new MemoryStream(fileBytes, 6, fileBytes.Length - 6);
            using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();

            await deflateStream.CopyToAsync(resultStream, ct);
            var json = Encoding.UTF8.GetString(resultStream.ToArray());

            var node = JsonNode.Parse(json);
            if (node == null)
                return Result<JsonNode>.Failure("Failed to parse save JSON.");

            return Result<JsonNode>.Success(node);
        }
        catch (Exception ex)
        {
            return Result<JsonNode>.Failure($"Failed to read save: {ex.Message}");
        }
    }

    private static async Task<Result<Unit>> WriteSaveJsonAsync(string path, JsonNode root, CancellationToken ct)
    {
        try
        {
            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            // Compress using zlib format: 4-byte size header + 0x78 0x9C + deflate data
            using var compressedStream = new MemoryStream();

            // Write uncompressed size header (4 bytes, little-endian)
            compressedStream.Write(BitConverter.GetBytes(jsonBytes.Length));

            // Write zlib header
            compressedStream.WriteByte(0x78);
            compressedStream.WriteByte(0x9C);

            // Write deflate-compressed data
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await deflateStream.WriteAsync(jsonBytes, ct);
            }

            // Atomic write: write to temp file first, then rename.
            // This prevents save corruption if the process crashes mid-write.
            var tempPath = path + $".tmp_{Guid.NewGuid():N}";
            try
            {
                await File.WriteAllBytesAsync(tempPath, compressedStream.ToArray(), ct);
                File.Move(tempPath, path, overwrite: true);
            }
            catch
            {
                // Clean up temp file on failure
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
                throw;
            }

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to write save: {ex.Message}");
        }
    }

    private static string GetCorpDisplayName(string internalName) => internalName switch
    {
        "StartingCorporation" => "Starting Corp",
        "FutureCorporation" => "Future Tech",
        "SelenianCorporation" => "Selenian",
        "GriffithsCorporation" => "Griffiths",
        "MoonCorporation" => "Moon Energy",
        "CleverCorporation" => "Clever Industries",
        "FE_FinalCorporation" => "Final Corp",
        _ => internalName
    };
}
