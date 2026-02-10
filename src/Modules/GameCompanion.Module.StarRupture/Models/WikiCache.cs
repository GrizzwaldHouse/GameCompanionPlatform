namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Cached wiki data from starrupture.tools.
/// </summary>
public sealed class WikiCache
{
    public DateTime LastUpdated { get; set; }
    public List<WikiItem> Items { get; set; } = new();
    public List<WikiBuilding> Buildings { get; set; } = new();
    public List<WikiBlueprint> Blueprints { get; set; } = new();
    public List<WikiCorporation> Corporations { get; set; } = new();
}

/// <summary>
/// Wiki data for an item.
/// </summary>
public sealed class WikiItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }
    public string? WikiUrl { get; init; }
}

/// <summary>
/// Wiki data for a building.
/// </summary>
public sealed class WikiBuilding
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public int PowerOutput { get; init; }
    public string? WikiUrl { get; init; }
}

/// <summary>
/// Wiki data for a blueprint.
/// </summary>
public sealed class WikiBlueprint
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Location { get; init; }
    public string? Corporation { get; init; }
    public string? WikiUrl { get; init; }
}

/// <summary>
/// Wiki data for a corporation.
/// </summary>
public sealed class WikiCorporation
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<string> Rewards { get; init; }
    public string? WikiUrl { get; init; }
}
