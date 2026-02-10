namespace GameCompanion.Module.StarRupture.Services;

using System.Text.RegularExpressions;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Service for building research/tech tree from crafting data crossed with wiki data.
/// </summary>
public sealed class ResearchTreeService
{
    private readonly WikiDataService _wikiService;

    public ResearchTreeService(WikiDataService wikiService)
    {
        _wikiService = wikiService;
    }

    /// <summary>
    /// Builds a research tree from save file crafting data, enriched with wiki information.
    /// </summary>
    public async Task<Result<ResearchTreeData>> BuildTreeAsync(
        CraftingData craftingData,
        CancellationToken ct = default)
    {
        try
        {
            // Try to get wiki data for enrichment (optional - graceful fallback)
            WikiCache? wiki = null;
            try
            {
                var wikiResult = await _wikiService.GetCachedDataAsync(false, ct);
                if (wikiResult.IsSuccess)
                    wiki = wikiResult.Value;
            }
            catch { /* wiki unavailable, continue without */ }

            // Build lookup for locked recipe names
            var lockedSet = new HashSet<string>(craftingData.LockedRecipes, StringComparer.OrdinalIgnoreCase);

            // Combine all known recipes (locked + unlocked/picked up)
            var allRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in craftingData.LockedRecipes) allRecipes.Add(r);
            foreach (var r in craftingData.PickedUpItems) allRecipes.Add(r);

            // Create nodes and group by category
            var nodes = allRecipes.Select(recipe =>
            {
                var name = ExtractRecipeName(recipe);
                var category = ExtractCategory(recipe);
                var status = lockedSet.Contains(recipe) ? ResearchNodeStatus.Locked : ResearchNodeStatus.Unlocked;

                // Enrich with wiki data
                var blueprintInfo = wiki?.Blueprints.FirstOrDefault(b =>
                    b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                return new ResearchNode
                {
                    Id = recipe,
                    Name = name,
                    Category = category,
                    Status = status,
                    Location = blueprintInfo?.Location,
                    Corporation = blueprintInfo?.Corporation,
                    WikiUrl = blueprintInfo?.WikiUrl
                };
            }).ToList();

            // Group by category
            var categories = nodes
                .GroupBy(n => n.Category)
                .Select((g, i) => new ResearchCategory
                {
                    Name = g.Key,
                    Order = i,
                    Nodes = g.OrderBy(n => n.Name).ToList()
                })
                .OrderBy(c => c.Name)
                .ToList();

            return Result<ResearchTreeData>.Success(new ResearchTreeData
            {
                Categories = categories,
                TotalRecipes = craftingData.TotalRecipeCount,
                UnlockedRecipes = craftingData.UnlockedRecipeCount
            });
        }
        catch (Exception ex)
        {
            return Result<ResearchTreeData>.Failure($"Failed to build research tree: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract a human-readable name from an Unreal asset path.
    /// Example: "/Game/Items/Resources/IronOre" -> "Iron Ore"
    /// </summary>
    private static string ExtractRecipeName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(name)) name = path;

        // Insert spaces before capital letters for CamelCase names
        var spaced = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        // Also handle underscore-separated names
        spaced = spaced.Replace('_', ' ');
        return spaced.Trim();
    }

    /// <summary>
    /// Extract category from Unreal asset path.
    /// Example: "/Game/Items/Resources/IronOre" -> "Resources"
    /// </summary>
    private static string ExtractCategory(string path)
    {
        var parts = path.Split('/');
        // Try to find a meaningful category segment
        // Pattern: /Game/Items/{Category}/{ItemName} or /Game/Blueprints/{Category}/...
        if (parts.Length >= 4)
        {
            // Return the second-to-last directory segment
            return parts[^2];
        }
        return "Uncategorized";
    }
}
