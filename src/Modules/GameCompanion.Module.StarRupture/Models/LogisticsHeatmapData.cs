namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Logistics traffic heatmap data.
/// </summary>
public sealed class LogisticsHeatmap
{
    public required IReadOnlyList<HeatmapCell> Cells { get; init; }
    public required IReadOnlyList<CongestionZone> CongestionZones { get; init; }
    public required IReadOnlyList<DeadZone> DeadZones { get; init; }
    public required double GridCellSize { get; init; }
    public required int TotalActiveRequests { get; init; }
    public required double AverageTrafficDensity { get; init; }
}

/// <summary>
/// A cell in the heatmap grid.
/// </summary>
public sealed class HeatmapCell
{
    public required int GridX { get; init; }
    public required int GridY { get; init; }
    public required WorldPosition CenterPosition { get; init; }
    public required int RequestCount { get; init; }
    public required HeatLevel Heat { get; init; }
    public required IReadOnlyList<int> EntityIds { get; init; }
}

/// <summary>
/// An area of congestion.
/// </summary>
public sealed class CongestionZone
{
    public required WorldPosition Center { get; init; }
    public required double Radius { get; init; }
    public required int RequestCount { get; init; }
    public required string Cause { get; init; }
    public required string Recommendation { get; init; }
}

/// <summary>
/// An area with no logistics activity.
/// </summary>
public sealed class DeadZone
{
    public required WorldPosition Center { get; init; }
    public required double Radius { get; init; }
    public required int EntityCount { get; init; }
    public required string PossibleReason { get; init; }
}

/// <summary>
/// Heat level for visualization.
/// </summary>
public enum HeatLevel
{
    None,      // No traffic
    Low,       // Light traffic
    Medium,    // Moderate traffic
    High,      // Heavy traffic
    Critical   // Congestion
}
