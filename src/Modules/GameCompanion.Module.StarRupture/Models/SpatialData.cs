namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// All spatial/world data extracted from a save file.
/// </summary>
public sealed class SpatialData
{
    public required WorldPosition PlayerPosition { get; init; }
    public required IReadOnlyList<PlacedEntity> Entities { get; init; }
    public required IReadOnlyList<BaseCoreData> BaseCores { get; init; }
    public required ElectricityNetworkData ElectricityNetwork { get; init; }
    public required LogisticsData Logistics { get; init; }
}

/// <summary>
/// 3D world position (Unreal coordinates).
/// </summary>
public sealed class WorldPosition
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; }
}

/// <summary>
/// A placed entity (building, machine, or world object) from the Mass entity system.
/// </summary>
public sealed class PlacedEntity
{
    public required int PersistentId { get; init; }
    public required string EntityConfigPath { get; init; }
    public required string EntityType { get; init; }
    public required string EntityCategory { get; init; }
    public required WorldPosition Position { get; init; }
    public required bool IsBuilding { get; init; }
    public required bool IsDisabled { get; init; }
    public required bool HasMalfunction { get; init; }
}

/// <summary>
/// Base core data from BaseCoreReplicationHelperSaveData.
/// </summary>
public sealed class BaseCoreData
{
    public required int EntityId { get; init; }
    public required int UpgradeLevel { get; init; }
    public required bool HasInfectionSphere { get; init; }
}

/// <summary>
/// Electricity network topology from electricitySubsystemState.
/// </summary>
public sealed class ElectricityNetworkData
{
    public required IReadOnlyList<ElectricityNode> Nodes { get; init; }
    public required IReadOnlyList<ElectricitySubgraph> Subgraphs { get; init; }
}

/// <summary>
/// A node in the electricity network (maps to an entity).
/// </summary>
public sealed class ElectricityNode
{
    public required int EntityId { get; init; }
    public required int SubgraphId { get; init; }
    public required IReadOnlyList<int> NeighbourIds { get; init; }
}

/// <summary>
/// A connected subgraph (power grid) in the electricity network.
/// </summary>
public sealed class ElectricitySubgraph
{
    public required int SubgraphId { get; init; }
}

/// <summary>
/// Logistics request data from logisticsRequestSubsystemState.
/// </summary>
public sealed class LogisticsData
{
    public required IReadOnlyList<LogisticsRequest> Requests { get; init; }
}

/// <summary>
/// An active logistics request (drone delivery, etc.).
/// </summary>
public sealed class LogisticsRequest
{
    public required int RequestId { get; init; }
    public required int RequesterEntityId { get; init; }
    public required string RequestType { get; init; }
    public required string WantedItem { get; init; }
    public required int WantedCount { get; init; }
    public required string Priority { get; init; }
    public required bool IsAborted { get; init; }
}
