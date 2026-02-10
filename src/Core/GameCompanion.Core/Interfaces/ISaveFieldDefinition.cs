namespace GameCompanion.Core.Interfaces;

using GameCompanion.Core.Enums;
using GameCompanion.Core.Models;

/// <summary>
/// Defines a field within a save file that can potentially be edited.
/// </summary>
public interface ISaveFieldDefinition
{
    /// <summary>
    /// Unique identifier for this field (e.g., "player.health", "world.spawn_position").
    /// </summary>
    string FieldId { get; }

    /// <summary>
    /// Human-readable name for display in the UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of what this field represents and what editing it affects.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The data type of the field value.
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Risk level classification for this field.
    /// </summary>
    RiskLevel Risk { get; }

    /// <summary>
    /// Whether this field is read-only (CRITICAL fields are always read-only).
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Optional validator function for the field value.
    /// </summary>
    Func<object, ValidationResult>? Validator { get; }
}
