namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Manages blueprint storage and retrieval.
/// </summary>
public sealed class BlueprintService
{
    private readonly string _blueprintDir;
    private readonly List<Blueprint> _blueprints = [];
    private bool _isLoaded;

    public BlueprintService()
    {
        _blueprintDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArcadiaTracker", "blueprints");
        Directory.CreateDirectory(_blueprintDir);
    }

    /// <summary>
    /// Gets the blueprint library.
    /// </summary>
    public async Task<Result<BlueprintLibrary>> GetLibraryAsync()
    {
        try
        {
            if (!_isLoaded)
                await LoadBlueprintsAsync();

            var categories = _blueprints
                .Select(b => b.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Result<BlueprintLibrary>.Success(new BlueprintLibrary
            {
                Blueprints = _blueprints.OrderByDescending(b => b.ModifiedAt).ToList(),
                Categories = categories,
                TotalCount = _blueprints.Count
            });
        }
        catch (Exception ex)
        {
            return Result<BlueprintLibrary>.Failure($"Failed to get library: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a blueprint from current save entities.
    /// </summary>
    public Result<Blueprint> CreateFromSave(StarRuptureSave save, string name, string category, IEnumerable<int> entityIds)
    {
        try
        {
            if (save.Spatial == null)
                return Result<Blueprint>.Failure("No spatial data available");

            var selectedEntities = save.Spatial.Entities
                .Where(e => entityIds.Contains(e.PersistentId))
                .ToList();

            if (selectedEntities.Count == 0)
                return Result<Blueprint>.Failure("No entities selected");

            var blueprintEntities = selectedEntities.Select(e => new BlueprintEntity
            {
                EntityType = e.EntityType,
                Position = e.Position,
                Rotation = 0 // Would need rotation data from save
            }).ToList();

            var minX = blueprintEntities.Min(e => e.Position.X);
            var maxX = blueprintEntities.Max(e => e.Position.X);
            var minY = blueprintEntities.Min(e => e.Position.Y);
            var maxY = blueprintEntities.Max(e => e.Position.Y);

            var entityCounts = blueprintEntities
                .GroupBy(e => ExtractTypeName(e.EntityType))
                .ToDictionary(g => g.Key, g => g.Count());

            var blueprint = new Blueprint
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = $"Blueprint with {blueprintEntities.Count} entities",
                Category = category,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                Entities = blueprintEntities,
                Bounds = new BlueprintBounds
                {
                    Width = maxX - minX,
                    Height = maxY - minY,
                    Center = new WorldPosition
                    {
                        X = (minX + maxX) / 2,
                        Y = (minY + maxY) / 2,
                        Z = blueprintEntities.Average(e => e.Position.Z)
                    }
                },
                Stats = new BlueprintStats
                {
                    EntityCount = blueprintEntities.Count,
                    UniqueTypes = entityCounts.Count,
                    EstimatedPower = EstimatePower(blueprintEntities),
                    EntityCounts = entityCounts
                },
                Tags = [],
                IsFavorite = false
            };

            _blueprints.Add(blueprint);
            return Result<Blueprint>.Success(blueprint);
        }
        catch (Exception ex)
        {
            return Result<Blueprint>.Failure($"Failed to create blueprint: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves blueprints to disk.
    /// </summary>
    public async Task<Result<bool>> SaveBlueprintsAsync()
    {
        try
        {
            foreach (var blueprint in _blueprints)
            {
                var path = Path.Combine(_blueprintDir, $"{blueprint.Id}.json");
                var json = JsonSerializer.Serialize(blueprint, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(path, json);
            }
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to save blueprints: {ex.Message}");
        }
    }

    private async Task LoadBlueprintsAsync()
    {
        _blueprints.Clear();

        var files = Directory.GetFiles(_blueprintDir, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var blueprint = JsonSerializer.Deserialize<Blueprint>(json);
                if (blueprint != null)
                    _blueprints.Add(blueprint);
            }
            catch
            {
                // Skip invalid files
            }
        }

        _isLoaded = true;
    }

    private static string ExtractTypeName(string entityType)
    {
        var parts = entityType.Split('/');
        return parts.LastOrDefault() ?? entityType;
    }

    private static double EstimatePower(List<BlueprintEntity> entities)
    {
        var power = 0.0;
        foreach (var entity in entities)
        {
            var type = entity.EntityType.ToLowerInvariant();
            if (type.Contains("generator") || type.Contains("solar"))
                power += 100;
            else if (type.Contains("smelter") || type.Contains("assembler"))
                power -= 30;
            else if (type.Contains("constructor"))
                power -= 20;
        }
        return power;
    }
}
