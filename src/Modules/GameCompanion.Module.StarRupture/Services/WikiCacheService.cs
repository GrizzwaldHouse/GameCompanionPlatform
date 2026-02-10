namespace GameCompanion.Module.StarRupture.Services;

using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Enhanced wiki service providing item/building/blueprint lookup with caching.
/// </summary>
public sealed class WikiCacheService
{
    private readonly WikiDataService _wikiDataService;
    private WikiCache? _cachedData;

    public WikiCacheService(WikiDataService wikiDataService)
    {
        _wikiDataService = wikiDataService;
    }

    /// <summary>
    /// Gets item information by name or ID.
    /// </summary>
    public async Task<Result<WikiItem?>> GetItemInfoAsync(
        string itemName,
        CancellationToken ct = default)
    {
        try
        {
            var cacheResult = await EnsureCacheLoadedAsync(ct);
            if (cacheResult.IsFailure)
                return Result<WikiItem?>.Failure(cacheResult.Error!);

            var cache = cacheResult.Value!;
            var item = cache.Items.FirstOrDefault(i =>
                i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

            return Result<WikiItem?>.Success(item);
        }
        catch (Exception ex)
        {
            return Result<WikiItem?>.Failure($"Failed to get item info: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets building information by type or ID.
    /// </summary>
    public async Task<Result<WikiBuilding?>> GetBuildingInfoAsync(
        string buildingType,
        CancellationToken ct = default)
    {
        try
        {
            var cacheResult = await EnsureCacheLoadedAsync(ct);
            if (cacheResult.IsFailure)
                return Result<WikiBuilding?>.Failure(cacheResult.Error!);

            var cache = cacheResult.Value!;
            var building = cache.Buildings.FirstOrDefault(b =>
                b.Name.Equals(buildingType, StringComparison.OrdinalIgnoreCase) ||
                b.Id.Equals(buildingType, StringComparison.OrdinalIgnoreCase));

            return Result<WikiBuilding?>.Success(building);
        }
        catch (Exception ex)
        {
            return Result<WikiBuilding?>.Failure($"Failed to get building info: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets blueprint information by name or ID.
    /// </summary>
    public async Task<Result<WikiBlueprint?>> GetBlueprintInfoAsync(
        string blueprintName,
        CancellationToken ct = default)
    {
        try
        {
            var cacheResult = await EnsureCacheLoadedAsync(ct);
            if (cacheResult.IsFailure)
                return Result<WikiBlueprint?>.Failure(cacheResult.Error!);

            var cache = cacheResult.Value!;
            var blueprint = cache.Blueprints.FirstOrDefault(bp =>
                bp.Name.Equals(blueprintName, StringComparison.OrdinalIgnoreCase) ||
                bp.Id.Equals(blueprintName, StringComparison.OrdinalIgnoreCase));

            return Result<WikiBlueprint?>.Success(blueprint);
        }
        catch (Exception ex)
        {
            return Result<WikiBlueprint?>.Failure($"Failed to get blueprint info: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches across all wiki data for matching entities.
    /// </summary>
    public async Task<Result<List<object>>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        try
        {
            var cacheResult = await EnsureCacheLoadedAsync(ct);
            if (cacheResult.IsFailure)
                return Result<List<object>>.Failure(cacheResult.Error!);

            var cache = cacheResult.Value!;
            var results = new List<object>();

            var lowerQuery = query.ToLowerInvariant();

            // Search items
            var matchingItems = cache.Items.Where(i =>
                i.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                i.Category.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase));
            results.AddRange(matchingItems);

            // Search buildings
            var matchingBuildings = cache.Buildings.Where(b =>
                b.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                b.Category.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase));
            results.AddRange(matchingBuildings);

            // Search blueprints
            var matchingBlueprints = cache.Blueprints.Where(bp =>
                bp.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                bp.Location.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                (bp.Corporation?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ?? false));
            results.AddRange(matchingBlueprints);

            // Search corporations
            var matchingCorporations = cache.Corporations.Where(c =>
                c.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase));
            results.AddRange(matchingCorporations);

            return Result<List<object>>.Success(results);
        }
        catch (Exception ex)
        {
            return Result<List<object>>.Failure($"Failed to search wiki: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets formatted tooltip text for an entity.
    /// </summary>
    public async Task<Result<string>> GetTooltipTextAsync(
        string entityName,
        CancellationToken ct = default)
    {
        try
        {
            var cacheResult = await EnsureCacheLoadedAsync(ct);
            if (cacheResult.IsFailure)
                return Result<string>.Failure(cacheResult.Error!);

            var cache = cacheResult.Value!;

            // Try to find as item
            var item = cache.Items.FirstOrDefault(i =>
                i.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                i.Id.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                return Result<string>.Success($"{item.Name}\n{item.Category}\n\n{item.Description}");
            }

            // Try to find as building
            var building = cache.Buildings.FirstOrDefault(b =>
                b.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                b.Id.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (building != null)
            {
                var powerInfo = building.PowerOutput > 0 ? $"\nPower: {building.PowerOutput}W" : "";
                return Result<string>.Success($"{building.Name}\n{building.Category}{powerInfo}");
            }

            // Try to find as blueprint
            var blueprint = cache.Blueprints.FirstOrDefault(bp =>
                bp.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                bp.Id.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (blueprint != null)
            {
                var corpInfo = !string.IsNullOrEmpty(blueprint.Corporation)
                    ? $"\nCorporation: {blueprint.Corporation}"
                    : "";
                return Result<string>.Success($"{blueprint.Name}\nLocation: {blueprint.Location}{corpInfo}");
            }

            // Try to find as corporation
            var corp = cache.Corporations.FirstOrDefault(c =>
                c.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (corp != null)
            {
                var rewards = corp.Rewards.Count > 0 ? $"\nRewards: {string.Join(", ", corp.Rewards)}" : "";
                return Result<string>.Success($"{corp.Name}\n{corp.Description}{rewards}");
            }

            return Result<string>.Failure($"Entity not found: {entityName}");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to get tooltip: {ex.Message}");
        }
    }

    /// <summary>
    /// Forces a cache refresh from the wiki data service.
    /// </summary>
    public async Task<Result<Unit>> RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var refreshResult = await _wikiDataService.GetCachedDataAsync(forceRefresh: true, ct);
            if (refreshResult.IsFailure)
                return Result<Unit>.Failure(refreshResult.Error!);

            _cachedData = refreshResult.Value;
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to refresh cache: {ex.Message}");
        }
    }

    private async Task<Result<WikiCache>> EnsureCacheLoadedAsync(CancellationToken ct)
    {
        if (_cachedData != null)
            return Result<WikiCache>.Success(_cachedData);

        var cacheResult = await _wikiDataService.GetCachedDataAsync(forceRefresh: false, ct);
        if (cacheResult.IsFailure)
            return cacheResult;

        _cachedData = cacheResult.Value;
        return cacheResult;
    }
}
