namespace GameCompanion.Core.Models;

/// <summary>
/// Represents a major phase in game progression (e.g., "Early Game", "Mid Game").
/// </summary>
public sealed record Phase(
    string Id,
    string Name,
    string Description,
    int Order,
    IReadOnlyList<Step> Steps);
