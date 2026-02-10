namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Global production summary across all bases.
/// </summary>
public sealed class ProductionSummary
{
    public required int TotalMachines { get; init; }
    public required int RunningMachines { get; init; }
    public required int DisabledMachines { get; init; }
    public required int MalfunctioningMachines { get; init; }
    public double EfficiencyPercent => TotalMachines > 0 ? (double)RunningMachines / TotalMachines * 100 : 100;
    public required IReadOnlyList<CategoryBreakdown> ByCategory { get; init; }
    public required PowerGridSummary PowerSummary { get; init; }
    public required IReadOnlyList<BaseProductionInfo> PerBase { get; init; }
}

/// <summary>
/// Machine breakdown by category (Production, Power, Storage, etc.).
/// </summary>
public sealed class CategoryBreakdown
{
    public required string Category { get; init; }
    public required int Total { get; init; }
    public required int Running { get; init; }
    public double EfficiencyPercent => Total > 0 ? (double)Running / Total * 100 : 100;
}

/// <summary>
/// Power grid summary.
/// </summary>
public sealed class PowerGridSummary
{
    public required int TotalGrids { get; init; }
    public required int TotalNodes { get; init; }
    public required IReadOnlyList<PowerGridInfo> Grids { get; init; }
}

/// <summary>
/// Individual power grid info.
/// </summary>
public sealed class PowerGridInfo
{
    public required int GridId { get; init; }
    public required int NodeCount { get; init; }
}

/// <summary>
/// Per-base production info for base comparator.
/// </summary>
public sealed class BaseProductionInfo
{
    public required string BaseId { get; init; }
    public required string BaseName { get; init; }
    public required int TotalBuildings { get; init; }
    public required int OperationalBuildings { get; init; }
    public required int DisabledBuildings { get; init; }
    public required int MalfunctioningBuildings { get; init; }
    public double EfficiencyPercent => TotalBuildings > 0 ? (double)OperationalBuildings / TotalBuildings * 100 : 100;
    public required IReadOnlyList<MachineSummary> Machines { get; init; }
}

/// <summary>
/// Side-by-side comparison of selected bases.
/// </summary>
public sealed class BaseComparison
{
    public required IReadOnlyList<BaseProductionInfo> Bases { get; init; }
}
