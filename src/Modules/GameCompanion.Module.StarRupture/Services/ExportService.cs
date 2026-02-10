namespace GameCompanion.Module.StarRupture.Services;

using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Progression;

/// <summary>
/// Exports game data to CSV or Excel format.
/// </summary>
public sealed class ExportService
{
    private readonly ProgressionAnalyzerService _analyzer;
    private readonly ProductionDataService _productionService;
    private readonly SessionTrackingService _sessionTracker;
    private readonly MapDataService _mapService;

    public ExportService(
        ProgressionAnalyzerService analyzer,
        ProductionDataService productionService,
        SessionTrackingService sessionTracker,
        MapDataService mapService)
    {
        _analyzer = analyzer;
        _productionService = productionService;
        _sessionTracker = sessionTracker;
        _mapService = mapService;
    }

    /// <summary>
    /// Exports data based on the export request configuration.
    /// </summary>
    public async Task<Result<ExportResult>> ExportAsync(
        ExportRequest request,
        StarRuptureSave save,
        CancellationToken ct = default)
    {
        try
        {
            var outputDir = Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            int rowCount = request.Format switch
            {
                ExportFormat.Csv => await ExportToCsvAsync(request, save, ct),
                ExportFormat.Excel => await ExportToExcelAsync(request, save, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(request.Format))
            };

            var fileInfo = new FileInfo(request.OutputPath);
            return Result<ExportResult>.Success(new ExportResult
            {
                FilePath = request.OutputPath,
                DataType = request.DataType,
                RowCount = rowCount,
                FileSizeBytes = fileInfo.Length,
            });
        }
        catch (Exception ex)
        {
            return Result<ExportResult>.Failure($"Export failed: {ex.Message}");
        }
    }

    private async Task<int> ExportToCsvAsync(
        ExportRequest request,
        StarRuptureSave save,
        CancellationToken ct)
    {
        var records = await GetExportRecords(request.DataType, save, request.SessionName, ct);

        await using var writer = new StreamWriter(request.OutputPath);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        });

        csv.WriteRecords(records);
        return records.Count;
    }

    private async Task<int> ExportToExcelAsync(
        ExportRequest request,
        StarRuptureSave save,
        CancellationToken ct)
    {
        using var workbook = new XLWorkbook();
        int totalRows = 0;

        if (request.DataType == ExportDataType.AllData)
        {
            // Export all data types to separate sheets
            foreach (var dataType in Enum.GetValues<ExportDataType>().Where(d => d != ExportDataType.AllData))
            {
                var records = await GetExportRecords(dataType, save, request.SessionName, ct);
                if (records.Count > 0)
                {
                    var sheet = workbook.Worksheets.Add(dataType.ToString());
                    AddRecordsToSheet(sheet, records);
                    totalRows += records.Count;
                }
            }
        }
        else
        {
            var records = await GetExportRecords(request.DataType, save, request.SessionName, ct);
            var sheet = workbook.Worksheets.Add(request.DataType.ToString());
            AddRecordsToSheet(sheet, records);
            totalRows = records.Count;
        }

        // Ensure at least one worksheet exists
        if (workbook.Worksheets.Count == 0)
            workbook.Worksheets.Add("Empty");

        workbook.SaveAs(request.OutputPath);
        return totalRows;
    }

    private async Task<List<Dictionary<string, object>>> GetExportRecords(
        ExportDataType dataType,
        StarRuptureSave save,
        string sessionName,
        CancellationToken ct)
    {
        return dataType switch
        {
            ExportDataType.PlayerProgress => GetProgressRecords(save),
            ExportDataType.SessionHistory => await GetSessionRecords(sessionName, ct),
            ExportDataType.ProductionSummary => GetProductionRecords(save),
            ExportDataType.BaseDetails => GetBaseRecords(save),
            ExportDataType.Badges => GetBadgeRecords(save),
            ExportDataType.CorporationStats => GetCorporationRecords(save),
            ExportDataType.AllData => [],
            _ => []
        };
    }

    private List<Dictionary<string, object>> GetProgressRecords(StarRuptureSave save)
    {
        var progress = _analyzer.AnalyzeSave(save);
        return
        [
            new Dictionary<string, object>
            {
                ["PlayTime"] = progress.TotalPlayTime.ToString(@"hh\:mm\:ss"),
                ["Phase"] = progress.CurrentPhase.ToString(),
                ["OverallProgress"] = $"{progress.OverallProgress:P1}",
                ["BlueprintsUnlocked"] = progress.BlueprintsUnlocked,
                ["BlueprintsTotal"] = progress.BlueprintsTotal,
                ["DataPoints"] = progress.DataPointsEarned,
                ["HighestCorpLevel"] = progress.HighestCorporationLevel,
                ["HighestCorpName"] = progress.HighestCorporationName,
                ["MapUnlocked"] = progress.MapUnlocked,
                ["Wave"] = progress.CurrentWave,
                ["BadgesEarned"] = progress.EarnedBadges.Count,
            }
        ];
    }

    private async Task<List<Dictionary<string, object>>> GetSessionRecords(
        string sessionName,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sessionName))
            return [];

        var historyResult = await _sessionTracker.GetHistoryAsync(sessionName, ct);
        if (historyResult.IsFailure || historyResult.Value == null)
            return [];

        return historyResult.Value.Snapshots.Select(s => new Dictionary<string, object>
        {
            ["Timestamp"] = s.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            ["PlayTime"] = s.PlayTimeAtSnapshot.ToString(@"hh\:mm\:ss"),
            ["Phase"] = s.Phase,
            ["OverallProgress"] = $"{s.OverallProgress:P1}",
            ["BlueprintsUnlocked"] = s.BlueprintsUnlocked,
            ["DataPoints"] = s.DataPoints,
        }).ToList();
    }

    private List<Dictionary<string, object>> GetProductionRecords(StarRuptureSave save)
    {
        var result = _productionService.BuildProductionSummary(save);
        if (result.IsFailure || result.Value == null)
            return [];

        return result.Value.ByCategory.Select(c => new Dictionary<string, object>
        {
            ["Category"] = c.Category,
            ["TotalMachines"] = c.Total,
            ["Operational"] = c.Running,
            ["Efficiency"] = $"{c.EfficiencyPercent:F0}%",
        }).ToList();
    }

    private List<Dictionary<string, object>> GetBaseRecords(StarRuptureSave save)
    {
        var result = _mapService.BuildMapData(save);
        if (result.IsFailure || result.Value == null)
            return [];

        return result.Value.Bases.Select(b => new Dictionary<string, object>
        {
            ["BaseName"] = b.Name,
            ["TotalBuildings"] = b.TotalBuildingCount,
            ["Operational"] = b.OperationalCount,
            ["Disabled"] = b.DisabledCount,
            ["Malfunctioning"] = b.MalfunctionCount,
        }).ToList();
    }

    private List<Dictionary<string, object>> GetBadgeRecords(StarRuptureSave save)
    {
        var progress = _analyzer.AnalyzeSave(save);
        var allBadges = Badges.AllBadges;

        return allBadges.Select(b => new Dictionary<string, object>
        {
            ["BadgeId"] = b.Id,
            ["Name"] = b.Name,
            ["Description"] = b.Description,
            ["Rarity"] = b.Rarity,
            ["Earned"] = progress.EarnedBadges.Any(e => e.Id == b.Id) ? "Yes" : "No",
        }).ToList();
    }

    private List<Dictionary<string, object>> GetCorporationRecords(StarRuptureSave save)
    {
        return save.Corporations.Corporations.Select(c => new Dictionary<string, object>
        {
            ["Name"] = c.DisplayName,
            ["Level"] = c.CurrentLevel,
            ["XP"] = c.CurrentXP,
        }).ToList();
    }

    private static void AddRecordsToSheet(IXLWorksheet sheet, List<Dictionary<string, object>> records)
    {
        if (records.Count == 0)
            return;

        // Headers
        var headers = records[0].Keys.ToList();
        for (int col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkGray;
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        for (int row = 0; row < records.Count; row++)
        {
            var record = records[row];
            for (int col = 0; col < headers.Count; col++)
            {
                var value = record[headers[col]];
                sheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }
}
