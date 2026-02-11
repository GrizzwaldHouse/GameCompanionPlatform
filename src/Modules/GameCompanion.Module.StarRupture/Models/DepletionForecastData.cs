namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Resource depletion forecast and sustainability analysis.
/// </summary>
public sealed class DepletionForecast
{
    public required IReadOnlyList<ResourceForecast> Forecasts { get; init; }
    public required IReadOnlyList<DepletionAlert> Alerts { get; init; }
    public required SustainabilityScore Sustainability { get; init; }
    public required IReadOnlyList<ResourceMitigation> Mitigations { get; init; }
    public required TimeSpan ForecastHorizon { get; init; }
}

/// <summary>
/// Forecast for a specific resource.
/// </summary>
public sealed class ResourceForecast
{
    public required string ResourceType { get; init; }
    public required double CurrentAmount { get; init; }
    public required double ConsumptionRate { get; init; }
    public required double ProductionRate { get; init; }
    public required TimeSpan TimeUntilDepletion { get; init; }
    public required DepletionStatus Status { get; init; }
    public double NetRate => ProductionRate - ConsumptionRate;
    public bool IsPositive => NetRate >= 0;
    public string TimeDisplay => TimeUntilDepletion.TotalDays >= 1
        ? $"{TimeUntilDepletion.TotalDays:F1} days"
        : TimeUntilDepletion.TotalHours >= 1
            ? $"{TimeUntilDepletion.TotalHours:F1} hours"
            : $"{TimeUntilDepletion.TotalMinutes:F0} min";
}

/// <summary>
/// Alert about resource depletion.
/// </summary>
public sealed class DepletionAlert
{
    public required string ResourceType { get; init; }
    public required AlertSeverity Severity { get; init; }
    public required string Message { get; init; }
    public required TimeSpan TimeUntilCritical { get; init; }
    public required IReadOnlyList<string> ImpactedSystems { get; init; }
}

/// <summary>
/// Overall sustainability score.
/// </summary>
public sealed class SustainabilityScore
{
    public required double OverallScore { get; init; }
    public required int SustainableResources { get; init; }
    public required int DepletingResources { get; init; }
    public required int CriticalResources { get; init; }
    public required string Assessment { get; init; }
    public required SustainabilityLevel Level { get; init; }
}

/// <summary>
/// Mitigation suggestion for depletion.
/// </summary>
public sealed class ResourceMitigation
{
    public required string ResourceType { get; init; }
    public required string Strategy { get; init; }
    public required string Description { get; init; }
    public required MitigationEffort Effort { get; init; }
    public required double ExpectedImprovement { get; init; }
}

/// <summary>
/// Depletion status classification.
/// </summary>
public enum DepletionStatus
{
    Sustainable,
    Stable,
    Declining,
    Critical,
    Depleted
}

/// <summary>
/// Alert severity level.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Critical,
    Emergency
}

/// <summary>
/// Sustainability level classification.
/// </summary>
public enum SustainabilityLevel
{
    Critical,
    Poor,
    Moderate,
    Good,
    Excellent
}

/// <summary>
/// Effort required for mitigation.
/// </summary>
public enum MitigationEffort
{
    Minimal,
    Low,
    Medium,
    High,
    Significant
}
