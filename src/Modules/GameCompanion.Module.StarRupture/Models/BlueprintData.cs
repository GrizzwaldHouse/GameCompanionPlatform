namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Blueprint library for storing and sharing base layouts.
/// </summary>
public sealed class BlueprintLibrary
{
    public required IReadOnlyList<Blueprint> Blueprints { get; init; }
    public required IReadOnlyList<string> Categories { get; init; }
    public required int TotalCount { get; init; }
}

/// <summary>
/// A saved blueprint/layout.
/// </summary>
public sealed class Blueprint
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required IReadOnlyList<BlueprintEntity> Entities { get; init; }
    public required BlueprintBounds Bounds { get; init; }
    public required BlueprintStats Stats { get; init; }
    public string? ThumbnailPath { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool IsFavorite { get; init; }
}

/// <summary>
/// An entity within a blueprint.
/// </summary>
public sealed class BlueprintEntity
{
    public required string EntityType { get; init; }
    public required WorldPosition Position { get; init; }
    public required double Rotation { get; init; }
}

/// <summary>
/// Bounding box for a blueprint.
/// </summary>
public sealed class BlueprintBounds
{
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required WorldPosition Center { get; init; }
}

/// <summary>
/// Statistics about a blueprint.
/// </summary>
public sealed class BlueprintStats
{
    public required int EntityCount { get; init; }
    public required int UniqueTypes { get; init; }
    public required double EstimatedPower { get; init; }
    public required IReadOnlyDictionary<string, int> EntityCounts { get; init; }
}
