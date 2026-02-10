namespace ArcadiaTracker.App.ViewModels;

using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for application settings with JSON persistence.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcadiaTracker");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    [ObservableProperty]
    private bool _autoRefreshEnabled = true;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds = 60;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private string _customSavePath = string.Empty;

    [ObservableProperty]
    private string _version = "0.1.0";

    public SettingsViewModel()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version != null)
        {
            Version = $"{version.Major}.{version.Minor}.{version.Build}";
        }

        LoadSettings();
    }

    [RelayCommand]
    public void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);

            var data = new SettingsData
            {
                AutoRefreshEnabled = AutoRefreshEnabled,
                AutoRefreshIntervalSeconds = AutoRefreshIntervalSeconds,
                ShowNotifications = ShowNotifications,
                CustomSavePath = CustomSavePath
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Settings save failure is non-critical
        }
    }

    [RelayCommand]
    public void ResetSettings()
    {
        AutoRefreshEnabled = true;
        AutoRefreshIntervalSeconds = 60;
        ShowNotifications = true;
        CustomSavePath = string.Empty;
        SaveSettings();
    }

    [RelayCommand]
    public void BrowseForSavePath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Save File Directory"
        };

        if (dialog.ShowDialog() == true)
        {
            CustomSavePath = dialog.FolderName;
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data == null) return;

            AutoRefreshEnabled = data.AutoRefreshEnabled;
            AutoRefreshIntervalSeconds = data.AutoRefreshIntervalSeconds;
            ShowNotifications = data.ShowNotifications;
            CustomSavePath = data.CustomSavePath;
        }
        catch
        {
            // Settings load failure is non-critical, use defaults
        }
    }

    private sealed class SettingsData
    {
        public bool AutoRefreshEnabled { get; set; } = true;
        public int AutoRefreshIntervalSeconds { get; set; } = 60;
        public bool ShowNotifications { get; set; } = true;
        public string CustomSavePath { get; set; } = string.Empty;
    }
}
