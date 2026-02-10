namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Defines available export types and their configurations.
/// </summary>
public enum ExportFormat
{
    Csv,
    Excel
}

/// <summary>
/// Types of data that can be exported.
/// </summary>
public enum ExportDataType
{
    PlayerProgress,
    SessionHistory,
    ProductionSummary,
    BaseDetails,
    Badges,
    CorporationStats,
    AllData
}

/// <summary>
/// Configuration for an export operation.
/// </summary>
public sealed class ExportRequest
{
    public required ExportFormat Format { get; init; }
    public required ExportDataType DataType { get; init; }
    public required string OutputPath { get; init; }
    public string SessionName { get; init; } = string.Empty;
}

/// <summary>
/// Result of an export operation.
/// </summary>
public sealed class ExportResult
{
    public required string FilePath { get; init; }
    public required ExportDataType DataType { get; init; }
    public required int RowCount { get; init; }
    public required long FileSizeBytes { get; init; }

    public string DisplaySize => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
