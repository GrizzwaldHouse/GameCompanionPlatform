namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Export view managing data export operations.
/// </summary>
public sealed partial class ExportViewModel : ObservableObject
{
    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.Csv;

    [ObservableProperty]
    private ExportDataType _selectedDataType = ExportDataType.AllData;

    [ObservableProperty]
    private string _lastExportPath = string.Empty;

    [ObservableProperty]
    private string _lastExportStatus = string.Empty;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private ObservableCollection<ExportResult> _exportHistory = [];

    public void AddExportResult(ExportResult result)
    {
        ExportHistory.Insert(0, result);
        LastExportPath = result.FilePath;
        LastExportStatus = $"Exported {result.RowCount} rows ({result.DisplaySize})";
    }
}
