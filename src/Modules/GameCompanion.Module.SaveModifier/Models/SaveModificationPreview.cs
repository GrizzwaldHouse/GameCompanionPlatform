namespace GameCompanion.Module.SaveModifier.Models;

/// <summary>
/// A read-only preview of what a save modification would produce.
/// Shows diffs for each field and any potential warnings.
/// </summary>
public sealed class SaveModificationPreview
{
    /// <summary>
    /// The save file path being previewed.
    /// </summary>
    public required string SavePath { get; init; }

    /// <summary>
    /// Individual field change previews.
    /// </summary>
    public required IReadOnlyList<FieldChangePreview> Changes { get; init; }

    /// <summary>
    /// Warnings about potential issues with the proposed modifications.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Whether all proposed modifications are valid and can be applied.
    /// </summary>
    public required bool IsValid { get; init; }
}

/// <summary>
/// Preview of a single field change within a modification.
/// </summary>
public sealed class FieldChangePreview
{
    /// <summary>
    /// The field being modified.
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// Human-readable field name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The current value before modification.
    /// </summary>
    public required object OldValue { get; init; }

    /// <summary>
    /// The value that would be set after modification.
    /// </summary>
    public required object NewValue { get; init; }

    /// <summary>
    /// Whether this specific change is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Reason if the change is not valid.
    /// </summary>
    public string? ValidationError { get; init; }
}
