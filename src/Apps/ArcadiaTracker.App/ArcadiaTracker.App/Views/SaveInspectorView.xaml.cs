using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

public partial class SaveInspectorView : UserControl
{
    private string? _currentJson;

    public SaveInspectorView()
    {
        InitializeComponent();
    }

    public void UpdateFromSave(StarRuptureSave save, string savePath)
    {
        // File info
        SavePathText.Text = savePath;
        try
        {
            var info = new FileInfo(savePath);
            SaveSizeText.Text = info.Exists
                ? $"{info.Length / 1024.0:F1} KB · Last modified: {info.LastWriteTime:g}"
                : "File not found";
        }
        catch
        {
            SaveSizeText.Text = "";
        }

        // Player stats
        InspPlaytime.Text = FormatPlaytime(save.PlayTime);
        InspDataPoints.Text = save.Corporations?.DataPoints.ToString("N0") ?? "—";
        InspInventorySlots.Text = save.Corporations?.UnlockedInventorySlots.ToString() ?? "—";

        // Header fields
        var headerFields = new ObservableCollection<KeyValuePair<string, string>>
        {
            new("Save Version", save.Header?.Version ?? "Unknown"),
            new("Timestamp", save.Header?.Timestamp.ToString("O") ?? "Unknown"),
            new("Checksum", save.Header?.Checksum ?? "N/A"),
            new("Play Time (seconds)", save.PlayTime.ToString("F1")),
            new("Map Unlocked", save.MapUnlocked.ToString()),
            new("Feature Flags", save.Corporations?.UnlockedFeaturesFlags.ToString() ?? "N/A"),
        };
        HeaderFieldsList.ItemsSource = headerFields;

        // Corporation details
        var corpDetails = new ObservableCollection<CorpDetailDisplay>();
        if (save.Corporations?.Entries != null)
        {
            foreach (var corp in save.Corporations.Entries)
            {
                corpDetails.Add(new CorpDetailDisplay
                {
                    Name = FormatCorpName(corp.Name),
                    InternalId = corp.Name,
                    Level = corp.CurrentLevel.ToString(),
                    XP = corp.CurrentXP.ToString("N0"),
                    XPPerLevel = corp.CurrentLevel > 0
                        ? (corp.CurrentXP / (double)corp.CurrentLevel).ToString("F0")
                        : "—"
                });
            }
        }
        CorporationDetailsList.ItemsSource = corpDetails;

        // Internal counters
        var counters = new ObservableCollection<KeyValuePair<string, string>>
        {
            new("Unlocked Inventory Slots", save.Corporations?.UnlockedInventorySlots.ToString() ?? "—"),
            new("Feature Flags (raw)", $"0x{save.Corporations?.UnlockedFeaturesFlags:X8}"),
            new("Data Points (raw)", save.Corporations?.DataPoints.ToString() ?? "—"),
            new("Total Corporations", save.Corporations?.Entries?.Count.ToString() ?? "0"),
            new("Crafting Recipes (locked)", save.Crafting?.LockedRecipeCount.ToString() ?? "—"),
            new("Crafting Recipes (unlocked)", save.Crafting?.UnlockedRecipeCount.ToString() ?? "—"),
            new("Buildings Count", save.Buildings?.Count.ToString() ?? "0"),
            new("Power Grids", save.PowerGrids?.Count.ToString() ?? "0"),
            new("Enviro Wave Stage", save.EnviroWave?.CurrentStage.ToString() ?? "—"),
            new("Enviro Wave Progress", save.EnviroWave?.StageProgress.ToString("P0") ?? "—"),
        };
        InternalCountersList.ItemsSource = counters;

        // Raw JSON (formatted top-level keys only for readability)
        try
        {
            var summary = BuildJsonSummary(save);
            _currentJson = summary;
            RawJsonText.Text = summary;
            RawJsonText.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
        }
        catch
        {
            RawJsonText.Text = "Unable to serialize save data.";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        // Refresh is handled by the parent wiring in MainWindow
    }

    private void CopyJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentJson))
        {
            Clipboard.SetText(_currentJson);
        }
    }

    private static string FormatPlaytime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
            : $"{ts.Minutes}m {ts.Seconds}s";
    }

    private static string FormatCorpName(string internalName)
    {
        // Convert internal names like "corp_moon_energy" to "Moon Energy"
        var name = internalName
            .Replace("corp_", "")
            .Replace("_", " ");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
    }

    private static string BuildJsonSummary(StarRuptureSave save)
    {
        var summary = new JsonObject
        {
            ["header"] = new JsonObject
            {
                ["version"] = save.Header?.Version,
                ["timestamp"] = save.Header?.Timestamp.ToString("O"),
                ["checksum"] = save.Header?.Checksum
            },
            ["playTime"] = save.PlayTime,
            ["mapUnlocked"] = save.MapUnlocked,
            ["corporations"] = new JsonObject
            {
                ["dataPoints"] = save.Corporations?.DataPoints,
                ["inventorySlots"] = save.Corporations?.UnlockedInventorySlots,
                ["featureFlags"] = save.Corporations?.UnlockedFeaturesFlags,
                ["count"] = save.Corporations?.Entries?.Count ?? 0
            },
            ["crafting"] = new JsonObject
            {
                ["unlockedRecipes"] = save.Crafting?.UnlockedRecipeCount,
                ["lockedRecipes"] = save.Crafting?.LockedRecipeCount
            },
            ["buildings"] = new JsonObject
            {
                ["count"] = save.Buildings?.Count ?? 0
            },
            ["powerGrids"] = new JsonObject
            {
                ["count"] = save.PowerGrids?.Count ?? 0
            },
            ["enviroWave"] = new JsonObject
            {
                ["stage"] = save.EnviroWave?.CurrentStage,
                ["progress"] = save.EnviroWave?.StageProgress
            }
        };

        return summary.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}

public class CorpDetailDisplay
{
    public string Name { get; set; } = "";
    public string InternalId { get; set; } = "";
    public string Level { get; set; } = "";
    public string XP { get; set; } = "";
    public string XPPerLevel { get; set; } = "";
}
