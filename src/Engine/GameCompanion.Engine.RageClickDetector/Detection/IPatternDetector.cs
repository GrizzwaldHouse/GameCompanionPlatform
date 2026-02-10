namespace GameCompanion.Engine.RageClickDetector.Detection;

using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Interface for individual rage-click pattern detectors.
/// Each detector analyzes a rolling buffer of interactions for a specific frustration pattern.
/// </summary>
public interface IPatternDetector
{
    /// <summary>
    /// The pattern type this detector identifies.
    /// </summary>
    RageClickPattern PatternType { get; }

    /// <summary>
    /// Analyzes the interaction buffer and returns any detected rage-click events.
    /// </summary>
    IReadOnlyList<RageClickEvent> Detect(IReadOnlyList<InteractionRecord> interactions, DetectorConfiguration config);
}
