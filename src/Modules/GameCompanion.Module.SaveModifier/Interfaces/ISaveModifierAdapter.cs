namespace GameCompanion.Module.SaveModifier.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Module.SaveModifier.Models;

/// <summary>
/// Game-specific adapter for save modification. Each supported game implements
/// this interface to define which fields can be modified and how to apply changes.
/// Adapters handle the translation between the generic modification model
/// and the game's specific save format.
/// </summary>
public interface ISaveModifierAdapter
{
    /// <summary>
    /// The game ID this adapter supports (must match IGameModule.GameId).
    /// </summary>
    string GameId { get; }

    /// <summary>
    /// Returns all modifiable fields for the given save file.
    /// Each field includes its current value, valid range, and risk level.
    /// </summary>
    Task<Result<IReadOnlyList<ModifiableField>>> GetModifiableFieldsAsync(
        string savePath,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a preview of what the save would look like after applying changes.
    /// Does NOT write anything — purely read-only analysis.
    /// </summary>
    Task<Result<SaveModificationPreview>> PreviewModificationsAsync(
        string savePath,
        IReadOnlyList<FieldModification> modifications,
        CancellationToken ct = default);

    /// <summary>
    /// Applies modifications to a save file. A backup MUST have already been created
    /// before this method is called. Returns failure if any modification cannot be applied
    /// atomically (all-or-nothing).
    /// </summary>
    Task<Result<SaveModificationResult>> ApplyModificationsAsync(
        string savePath,
        IReadOnlyList<FieldModification> modifications,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that a save file is in a modifiable state (not corrupted,
    /// correct format version, etc.).
    /// </summary>
    Task<Result<bool>> ValidateSaveForModificationAsync(
        string savePath,
        CancellationToken ct = default);
}
