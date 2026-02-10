namespace GameCompanion.Module.SaveModifier.Models;

using GameCompanion.Core.Enums;

/// <summary>
/// Describes a single field within a save file that can be modified,
/// including its current value, constraints, and risk classification.
/// </summary>
public sealed class ModifiableField
{
    /// <summary>
    /// Unique identifier for this field within the save structure (e.g., "corporations.moon_energy.level").
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Category for grouping in the UI (e.g., "Corporations", "Crafting", "Inventory").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Description of what this field controls and the impact of changing it.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The current value of the field in the save file.
    /// </summary>
    public required object CurrentValue { get; init; }

    /// <summary>
    /// The data type of the field (int, string, bool, etc.).
    /// </summary>
    public required Type DataType { get; init; }

    /// <summary>
    /// Risk level classification for this field.
    /// </summary>
    public required RiskLevel Risk { get; init; }

    /// <summary>
    /// Minimum allowed value (for numeric fields). Null if not applicable.
    /// </summary>
    public object? MinValue { get; init; }

    /// <summary>
    /// Maximum allowed value (for numeric fields). Null if not applicable.
    /// </summary>
    public object? MaxValue { get; init; }

    /// <summary>
    /// Allowed discrete values (for enum-like fields). Null if any value is allowed.
    /// </summary>
    public IReadOnlyList<object>? AllowedValues { get; init; }
}
