namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Research/tech tree data structure.
/// </summary>
public sealed class ResearchTreeData
{
    public required IReadOnlyList<ResearchCategory> Categories { get; init; }
    public required int TotalRecipes { get; init; }
    public required int UnlockedRecipes { get; init; }
    public double UnlockPercent => TotalRecipes > 0 ? (double)UnlockedRecipes / TotalRecipes * 100 : 0;
}

/// <summary>
/// A category grouping in the research tree.
/// </summary>
public sealed class ResearchCategory
{
    public required string Name { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyList<ResearchNode> Nodes { get; init; }
    public int UnlockedCount => Nodes.Count(n => n.Status == ResearchNodeStatus.Unlocked);
    public int TotalCount => Nodes.Count;
}

/// <summary>
/// A single research node (recipe/blueprint).
/// </summary>
public sealed class ResearchNode
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required ResearchNodeStatus Status { get; init; }
    public string? Location { get; init; }
    public string? Corporation { get; init; }
    public string? WikiUrl { get; init; }
}

/// <summary>
/// Status of a research node.
/// </summary>
public enum ResearchNodeStatus
{
    Locked,
    Unlocked,
    Unknown
}
