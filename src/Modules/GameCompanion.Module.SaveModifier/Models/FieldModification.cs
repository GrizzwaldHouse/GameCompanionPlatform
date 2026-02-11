namespace GameCompanion.Module.SaveModifier.Models;

/// <summary>
/// Represents a single field modification to apply to a save file.
/// </summary>
public sealed class FieldModification
{
    /// <summary>
    /// The field ID to modify (must match a ModifiableField.FieldId).
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// The new value to set for this field.
    /// </summary>
    public required object NewValue { get; init; }
}
