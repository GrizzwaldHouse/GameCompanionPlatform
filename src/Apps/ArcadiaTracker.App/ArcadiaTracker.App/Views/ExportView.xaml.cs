using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Export view for exporting game data to various formats.
/// </summary>
public partial class ExportView : UserControl
{
    public ExportView()
    {
        InitializeComponent();
    }

    public event EventHandler<ExportRequest>? ExportRequested;

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var format = FormatCombo.SelectedIndex == 0 ? ExportFormat.Csv : ExportFormat.Excel;
        var dataType = DataTypeCombo.SelectedIndex switch
        {
            0 => ExportDataType.AllData,
            1 => ExportDataType.PlayerProgress,
            2 => ExportDataType.ProductionSummary,
            3 => ExportDataType.SessionHistory,
            4 => ExportDataType.CorporationStats,
            5 => ExportDataType.Badges,
            6 => ExportDataType.BaseDetails,
            7 => ExportDataType.BaseDetails, // PowerGrids mapped to BaseDetails
            _ => ExportDataType.AllData
        };

        var request = new ExportRequest
        {
            Format = format,
            DataType = dataType,
            OutputPath = string.Empty // Will be set by the handler
        };

        ExportRequested?.Invoke(this, request);
    }

    public void UpdateHistory(List<ExportResult> history)
    {
        var displayItems = history.Select(h => new ExportHistoryDisplayItem(h)).ToList();
        ExportHistoryList.ItemsSource = displayItems;
        NoExportsText.Visibility = displayItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}

public class ExportHistoryDisplayItem
{
    public ExportHistoryDisplayItem(ExportResult result)
    {
        FilePath = result.FilePath;
        Timestamp = DateTime.Now; // Result doesn't have timestamp, would need to be added to ExportResult or tracked separately
        RowCount = result.RowCount;
        SizeDisplay = result.DisplaySize;
    }

    public string FilePath { get; }
    public DateTime Timestamp { get; }
    public int RowCount { get; }
    public string SizeDisplay { get; }
}
