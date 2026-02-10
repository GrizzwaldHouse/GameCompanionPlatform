namespace GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Complete rage-click analysis report with events, remediations, and validation results.
/// </summary>
public sealed record RageClickReport
{
    /// <summary>
    /// All rage-click events detected during analysis.
    /// </summary>
    public required IReadOnlyList<RageClickEvent> Events { get; init; }

    /// <summary>
    /// Suggested or applied remediation actions.
    /// </summary>
    public required IReadOnlyList<RemediationAction> Remediations { get; init; }

    /// <summary>
    /// Validation results comparing before/after remediation.
    /// Null if validation has not been run.
    /// </summary>
    public ValidationDelta? Validation { get; init; }

    /// <summary>
    /// UTC timestamp when the report was generated.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Total number of interactions analyzed.
    /// </summary>
    public required int TotalInteractionsAnalyzed { get; init; }
}

/// <summary>
/// Before/after comparison of rage-click intensity after remediation.
/// </summary>
public sealed record ValidationDelta
{
    /// <summary>
    /// Average rage intensity before remediation.
    /// </summary>
    public required double AverageIntensityBefore { get; init; }

    /// <summary>
    /// Average rage intensity after remediation.
    /// </summary>
    public required double AverageIntensityAfter { get; init; }

    /// <summary>
    /// Change in intensity (negative means improvement).
    /// </summary>
    public double IntensityDelta => AverageIntensityAfter - AverageIntensityBefore;

    /// <summary>
    /// Remaining high-confidence frustration points after remediation.
    /// </summary>
    public required IReadOnlyList<RageClickEvent> RemainingHighConfidenceEvents { get; init; }
}
