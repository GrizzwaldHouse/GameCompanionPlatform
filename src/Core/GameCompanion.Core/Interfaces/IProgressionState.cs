namespace GameCompanion.Core.Interfaces;

/// <summary>
/// Represents the current state of a player's progression through the game.
/// Implementations are game-specific and read from save files.
/// </summary>
public interface IProgressionState
{
    /// <summary>
    /// Unique identifier for this progression state (typically the save/world ID).
    /// </summary>
    string StateId { get; }

    /// <summary>
    /// Checks if a specific step has been completed.
    /// </summary>
    bool IsStepCompleted(string stepId);

    /// <summary>
    /// Checks if a specific checklist item has been marked complete.
    /// </summary>
    bool IsChecklistItemCompleted(string stepId, string itemId);

    /// <summary>
    /// Gets all completed step IDs.
    /// </summary>
    IReadOnlySet<string> CompletedStepIds { get; }

    /// <summary>
    /// Gets completed checklist item IDs for a specific step.
    /// </summary>
    IReadOnlySet<string> GetCompletedChecklistItems(string stepId);
}
