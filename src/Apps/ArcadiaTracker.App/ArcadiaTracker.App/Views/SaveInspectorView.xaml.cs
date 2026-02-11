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
            new("Session Name", save.SessionName ?? "Unknown"),
            new("Save Timestamp", save.SaveTimestamp.ToString("O")),
            new("Play Time (seconds)", save.PlayTime.TotalSeconds.ToString("F1")),
            new("Tutorial Completed", save.GameState?.TutorialCompleted.ToString() ?? "Unknown"),
            new("Feature Flags", save.Corporations?.UnlockedFeaturesFlags.ToString() ?? "N/A"),
        };
        HeaderFieldsList.ItemsSource = headerFields;

        // Corporation details
        var corpDetails = new ObservableCollection<CorpDetailDisplay>();
        if (save.Corporations?.Corporations != null)
        {
            foreach (var corp in save.Corporations.Corporations)
            {
                corpDetails.Add(new CorpDetailDisplay
                {
                    Name = corp.DisplayName,
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
            new("Total Corporations", save.Corporations?.Corporations?.Count.ToString() ?? "0"),
            new("Crafting Recipes (locked)", save.Crafting?.LockedRecipes?.Count.ToString() ?? "—"),
            new("Crafting Recipes (unlocked)", save.Crafting?.UnlockedRecipeCount.ToString() ?? "—"),
            new("Crafting Total Recipes", save.Crafting?.TotalRecipeCount.ToString() ?? "—"),
            new("Picked Up Items", save.Crafting?.PickedUpItems?.Count.ToString() ?? "—"),
            new("Enviro Wave", save.EnviroWave?.Wave ?? "—"),
            new("Enviro Wave Stage", save.EnviroWave?.Stage ?? "—"),
            new("Enviro Wave Progress", save.EnviroWave?.Progress.ToString("P0") ?? "—"),
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

    private static string FormatPlaytime(TimeSpan playTime)
    {
        return playTime.TotalHours >= 1
            ? $"{(int)playTime.TotalHours}h {playTime.Minutes}m"
            : $"{playTime.Minutes}m {playTime.Seconds}s";
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
            ["sessionName"] = save.SessionName,
            ["saveTimestamp"] = save.SaveTimestamp.ToString("O"),
            ["playTime"] = save.PlayTime.TotalSeconds,
            ["gameState"] = new JsonObject
            {
                ["tutorialCompleted"] = save.GameState?.TutorialCompleted,
                ["playtimeDuration"] = save.GameState?.PlaytimeDuration
            },
            ["corporations"] = new JsonObject
            {
                ["dataPoints"] = save.Corporations?.DataPoints,
                ["inventorySlots"] = save.Corporations?.UnlockedInventorySlots,
                ["featureFlags"] = save.Corporations?.UnlockedFeaturesFlags,
                ["count"] = save.Corporations?.Corporations?.Count ?? 0
            },
            ["crafting"] = new JsonObject
            {
                ["unlockedRecipes"] = save.Crafting?.UnlockedRecipeCount,
                ["lockedRecipes"] = save.Crafting?.LockedRecipes?.Count ?? 0,
                ["totalRecipes"] = save.Crafting?.TotalRecipeCount,
                ["pickedUpItems"] = save.Crafting?.PickedUpItems?.Count ?? 0
            },
            ["enviroWave"] = new JsonObject
            {
                ["wave"] = save.EnviroWave?.Wave,
                ["stage"] = save.EnviroWave?.Stage,
                ["progress"] = save.EnviroWave?.Progress
            },
            ["spatial"] = save.Spatial != null ? JsonValue.Create(true) : JsonValue.Create(false)
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
