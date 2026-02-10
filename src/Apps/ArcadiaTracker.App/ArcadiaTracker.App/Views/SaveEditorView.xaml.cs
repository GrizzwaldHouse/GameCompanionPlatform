using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Module.SaveModifier.Interfaces;
using GameCompanion.Module.SaveModifier.Models;
using GameCompanion.Module.SaveModifier.Services;

namespace ArcadiaTracker.App.Views;

public partial class SaveEditorView : UserControl
{
    private SaveModificationOrchestrator? _orchestrator;
    private IConsentService? _consentService;
    private ISaveModifierAdapter? _adapter;
    private string? _currentSavePath;
    private string _gameId = "star_rupture";
    private ObservableCollection<EditableFieldDisplay> _fields = [];
    private bool _hasConsent;

    public SaveEditorView()
    {
        InitializeComponent();
    }

    public void Initialize(
        SaveModificationOrchestrator orchestrator,
        IConsentService consentService,
        ISaveModifierAdapter adapter)
    {
        _orchestrator = orchestrator;
        _consentService = consentService;
        _adapter = adapter;
        _ = CheckConsent();
    }

    public void SetSavePath(string savePath)
    {
        _currentSavePath = savePath;
        SaveFilePathText.Text = savePath;
    }

    private async Task CheckConsent()
    {
        if (_consentService == null) return;

        var info = _consentService.GetConsentInfo(_gameId);
        var result = await _consentService.HasConsentAsync(_gameId, info.Version);

        if (result.IsSuccess && result.Value!)
        {
            _hasConsent = true;
            ConsentBanner.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;
        }
        else
        {
            ConsentBanner.Visibility = Visibility.Visible;
            EditorPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void ConsentAcceptButton_Click(object sender, RoutedEventArgs e)
    {
        if (_consentService == null) return;

        var info = _consentService.GetConsentInfo(_gameId);
        await _consentService.RecordConsentAsync(_gameId, info.Version, info.TextHash);
        _hasConsent = true;
        ConsentBanner.Visibility = Visibility.Collapsed;
        EditorPanel.Visibility = Visibility.Visible;
    }

    private void ConsentDeclineButton_Click(object sender, RoutedEventArgs e)
    {
        ConsentBanner.Visibility = Visibility.Collapsed;
    }

    private async void LoadFieldsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_adapter == null || string.IsNullOrEmpty(_currentSavePath)) return;

        LoadFieldsButton.IsEnabled = false;
        try
        {
            var result = await _adapter.GetModifiableFieldsAsync(_currentSavePath);
            if (result.IsSuccess)
            {
                _fields = new ObservableCollection<EditableFieldDisplay>(
                    result.Value!.Select(f => new EditableFieldDisplay(f)));

                FieldsList.ItemsSource = _fields;
                NoFieldsText.Visibility = _fields.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
                PreviewButton.IsEnabled = _fields.Count > 0;
            }
            else
            {
                ShowStatus($"Failed to load fields: {result.Error}", isError: true);
            }
        }
        finally
        {
            LoadFieldsButton.IsEnabled = true;
        }
    }

    private async void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_orchestrator == null || string.IsNullOrEmpty(_currentSavePath)) return;

        var modifications = GetModifications();
        if (modifications.Count == 0)
        {
            ShowStatus("No changes to preview. Enter new values in the fields above.", isError: false);
            return;
        }

        PreviewButton.IsEnabled = false;
        try
        {
            var result = await _orchestrator.PreviewAsync(_gameId, _currentSavePath, modifications);
            if (result.IsSuccess)
            {
                var preview = result.Value!;
                var displayItems = preview.Changes.Select(c => new PreviewDisplayItem
                {
                    StatusIcon = c.IsValid ? "✓" : "✗",
                    FieldId = c.FieldId,
                    OldValue = c.OldValue?.ToString() ?? "—",
                    NewValue = c.NewValue?.ToString() ?? "—"
                });

                PreviewList.ItemsSource = displayItems;
                PreviewList.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = preview.IsValid;

                if (preview.Warnings.Count > 0)
                {
                    ShowStatus(string.Join("\n", preview.Warnings), isError: false);
                }
            }
            else
            {
                ShowStatus($"Preview failed: {result.Error}", isError: true);
            }
        }
        finally
        {
            PreviewButton.IsEnabled = true;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_orchestrator == null || string.IsNullOrEmpty(_currentSavePath)) return;

        var confirm = MessageBox.Show(
            "Apply these changes? A backup will be created automatically before modification.",
            "Confirm Save Modification",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        var modifications = GetModifications();
        ApplyButton.IsEnabled = false;

        try
        {
            var result = await _orchestrator.ApplyAsync(
                _gameId, _currentSavePath, modifications, userConfirmed: true);

            if (result.IsSuccess)
            {
                ShowStatus(
                    $"Successfully modified {result.Value!.ModifiedFieldCount} field(s). Backup saved.",
                    isError: false);
                PreviewList.Visibility = Visibility.Collapsed;

                // Reload fields to show updated values
                LoadFieldsButton_Click(sender, e);
            }
            else
            {
                ShowStatus($"Modification failed: {result.Error}", isError: true);
            }
        }
        finally
        {
            ApplyButton.IsEnabled = true;
        }
    }

    private List<FieldModification> GetModifications()
    {
        return _fields
            .Where(f => !string.IsNullOrWhiteSpace(f.NewValue) &&
                        f.NewValue != f.CurrentValueDisplay)
            .Select(f => new FieldModification
            {
                FieldId = f.FieldId,
                NewValue = ConvertValue(f)
            })
            .ToList();
    }

    private static object ConvertValue(EditableFieldDisplay field)
    {
        if (field.DataType == typeof(bool))
            return bool.Parse(field.NewValue);
        if (field.DataType == typeof(int))
            return int.Parse(field.NewValue);
        return field.NewValue;
    }

    private void ShowStatus(string message, bool isError)
    {
        EditorStatusBorder.Visibility = Visibility.Visible;
        EditorStatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        EditorStatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        EditorStatusText.Text = message;
    }
}

public class EditableFieldDisplay : INotifyPropertyChanged
{
    private string _newValue = "";

    public string FieldId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public string CurrentValueDisplay { get; init; } = "";
    public Type DataType { get; init; } = typeof(string);
    public string RiskDisplay { get; init; } = "";
    public string RiskColor { get; init; } = "#A0A0B0";
    public string BoundsDisplay { get; init; } = "";

    public string NewValue
    {
        get => _newValue;
        set
        {
            _newValue = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewValue)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditableFieldDisplay(ModifiableField field)
    {
        FieldId = field.FieldId;
        DisplayName = field.DisplayName;
        Description = field.Description;
        Category = field.Category;
        CurrentValueDisplay = field.CurrentValue?.ToString() ?? "—";
        DataType = field.DataType;
        RiskDisplay = $"Risk: {field.Risk}";
        RiskColor = field.Risk switch
        {
            GameCompanion.Core.Enums.RiskLevel.Low => "#2ED573",
            GameCompanion.Core.Enums.RiskLevel.Medium => "#FF6B35",
            GameCompanion.Core.Enums.RiskLevel.High => "#FF4757",
            GameCompanion.Core.Enums.RiskLevel.Critical => "#FF0000",
            _ => "#A0A0B0"
        };
        BoundsDisplay = field.MinValue != null && field.MaxValue != null
            ? $"[{field.MinValue}–{field.MaxValue}]"
            : "";
    }
}

public class PreviewDisplayItem
{
    public string StatusIcon { get; set; } = "";
    public string FieldId { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
}
