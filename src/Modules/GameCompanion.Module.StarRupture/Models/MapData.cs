namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Map-ready data transformed from raw spatial data.
/// </summary>
public sealed class MapData
{
    public required MapBounds WorldBounds { get; init; }
    public required WorldPosition PlayerPosition { get; init; }
    public required IReadOnlyList<BaseCluster> Bases { get; init; }
    public required IReadOnlyList<MapConnection> Connections { get; init; }
    public required int TotalBuildingCount { get; init; }
    public required int TotalPowerGrids { get; init; }
    public required int ActiveLogisticsRequests { get; init; }
}

/// <summary>
/// World coordinate bounding box.
/// </summary>
public sealed class MapBounds
{
    public double MinX { get; init; }
    public double MinY { get; init; }
    public double MaxX { get; init; }
    public double MaxY { get; init; }
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double CenterX => (MinX + MaxX) / 2;
    public double CenterY => (MinY + MaxY) / 2;
}

/// <summary>
/// A cluster of buildings detected as a base.
/// </summary>
public sealed class BaseCluster
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required WorldPosition Center { get; init; }
    public required double Radius { get; init; }
    public required int BaseCoreEntityId { get; init; }
    public required int BaseCoreLevel { get; init; }
    public required IReadOnlyList<MachineSummary> Machines { get; init; }
    public required int TotalBuildingCount { get; init; }
    public required int OperationalCount { get; init; }
    public required int DisabledCount { get; init; }
    public required int MalfunctionCount { get; init; }
    public required int PowerGridId { get; init; }
}

/// <summary>
/// Summary of machines of a single type within a base.
/// </summary>
public sealed class MachineSummary
{
    public required string TypeName { get; init; }
    public required string Category { get; init; }
    public required int Count { get; init; }
    public required int RunningCount { get; init; }
}

/// <summary>
/// A connection between two bases (shared power grid or drone logistics).
/// </summary>
public sealed class MapConnection
{
    public required string FromBaseId { get; init; }
    public required string ToBaseId { get; init; }
    public required WorldPosition Start { get; init; }
    public required WorldPosition End { get; init; }
    public required ConnectionType Type { get; init; }
    public required ConnectionStatus Status { get; init; }
}

/// <summary>
/// Type of inter-base connection.
/// </summary>
public enum ConnectionType
{
    PowerGrid,
    DroneLogistics
}

/// <summary>
/// Status of a connection.
/// </summary>
public enum ConnectionStatus
{
    Active,
    Idle,
    Broken
}
