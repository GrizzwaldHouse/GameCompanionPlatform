namespace GameCompanion.Core.Interfaces;

using GameCompanion.Core.Models;

/// <summary>
/// Defines the progression structure for a game, including all phases and steps.
/// Each game module provides its own implementation.
/// </summary>
public interface IProgressionMap
{
    /// <summary>
    /// All phases in this game's progression, ordered by expected completion.
    /// </summary>
    IReadOnlyList<Phase> Phases { get; }

    /// <summary>
    /// Gets the current phase based on the player's progression state.
    /// </summary>
    Phase GetCurrentPhase(IProgressionState state);

    /// <summary>
    /// Gets the steps that are currently available (prerequisites met).
    /// </summary>
    IReadOnlyList<Step> GetAvailableSteps(IProgressionState state);

    /// <summary>
    /// Gets the next recommended step based on the player's state.
    /// </summary>
    Step? GetNextRecommendedStep(IProgressionState state);

    /// <summary>
    /// Calculates overall progression percentage (0.0 to 1.0).
    /// </summary>
    double GetProgressPercentage(IProgressionState state);
}
