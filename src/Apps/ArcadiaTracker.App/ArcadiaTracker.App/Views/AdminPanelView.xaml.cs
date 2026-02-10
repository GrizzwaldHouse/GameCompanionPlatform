using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;
using GameCompanion.Engine.Entitlements.Services;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Admin panel for generating activation codes, self-activating features,
/// and viewing audit logs. Only available in DEBUG builds with admin capabilities.
/// </summary>
public partial class AdminPanelView : UserControl
{
    private IActivationCodeService? _activationService;
    private IEntitlementService? _entitlementService;
    private LocalAuditLogger? _auditLogger;
    private string _gameScope = "star_rupture";

    /// <summary>
    /// Raised when features are activated via the admin panel,
    /// signaling MainWindow to refresh premium nav items.
    /// </summary>
    public event EventHandler? FeaturesActivated;

    public AdminPanelView()
    {
        InitializeComponent();
    }

    public void Initialize(
        IActivationCodeService activationService,
        IEntitlementService entitlementService,
        LocalAuditLogger auditLogger,
        string gameScope)
    {
        _activationService = activationService;
        _entitlementService = entitlementService;
        _auditLogger = auditLogger;
        _gameScope = gameScope;
        _ = RefreshAuditLog();
    }

    private void GenerateCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activationService == null) return;

        var bundleIndex = BundleComboBox.SelectedIndex;
        if (bundleIndex < 0) return;

        var bundle = (ActivationBundle)bundleIndex;
        var code = _activationService.GenerateCode(bundle);

        GeneratedCodeText.Text = code;
        GeneratedCodeBorder.Visibility = Visibility.Visible;
    }

    private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var code = GeneratedCodeText.Text;
        if (!string.IsNullOrEmpty(code))
            Clipboard.SetText(code);
    }

    private async void SelfActivateProButton_Click(object sender, RoutedEventArgs e)
    {
        await SelfActivateBundle(ActivationBundle.Pro);
    }

    private async void SelfActivateAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activationService == null || _entitlementService == null) return;

        var allBundles = new[]
        {
            ActivationBundle.Pro,
            ActivationBundle.Optimizer,
            ActivationBundle.Milestones,
            ActivationBundle.ExportPro
        };

        var totalGranted = 0;
        foreach (var bundle in allBundles)
        {
            var code = _activationService.GenerateCode(bundle);
            var result = await _activationService.RedeemAsync(code, _gameScope);
            if (result.IsSuccess)
                totalGranted += result.Value!.Count;
        }

        ShowSelfActivateStatus($"Activated {totalGranted} features across all bundles.", isError: false);
        FeaturesActivated?.Invoke(this, EventArgs.Empty);
        await RefreshAuditLog();
    }

    private async Task SelfActivateBundle(ActivationBundle bundle)
    {
        if (_activationService == null) return;

        var code = _activationService.GenerateCode(bundle);
        var result = await _activationService.RedeemAsync(code, _gameScope);

        if (result.IsSuccess)
        {
            ShowSelfActivateStatus(
                $"Activated {result.Value!.Count} feature(s): {string.Join(", ", result.Value)}",
                isError: false);
            FeaturesActivated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ShowSelfActivateStatus($"Activation failed: {result.Error}", isError: true);
        }

        await RefreshAuditLog();
    }

    private void BatchGenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activationService == null) return;

        if (!int.TryParse(BatchCountTextBox.Text, out var count) || count < 1 || count > 100)
        {
            BatchCodesTextBox.Text = "Enter a count between 1 and 100.";
            return;
        }

        var bundleIndex = BundleComboBox.SelectedIndex;
        var bundle = (ActivationBundle)(bundleIndex < 0 ? 0 : bundleIndex);

        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var code = _activationService.GenerateCode(bundle);
            sb.AppendLine(code);
        }

        BatchCodesTextBox.Text = sb.ToString().TrimEnd();
    }

    private void CopyBatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(BatchCodesTextBox.Text) &&
            BatchCodesTextBox.Text != "Generated codes will appear here...")
        {
            Clipboard.SetText(BatchCodesTextBox.Text);
        }
    }

    private async void RefreshAuditButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAuditLog();
    }

    private async Task RefreshAuditLog()
    {
        if (_auditLogger == null) return;

        var result = await _auditLogger.ReadAllAsync();
        if (result.IsSuccess)
        {
            var entries = result.Value!
                .OrderByDescending(e => e.Timestamp)
                .Take(50)
                .Select(e => new AuditLogDisplayItem
                {
                    TimestampDisplay = e.Timestamp.ToLocalTime().ToString("g"),
                    Detail = $"[{e.Action}] {e.Detail}",
                    Outcome = e.Outcome.ToString(),
                    OutcomeColor = e.Outcome switch
                    {
                        AuditOutcome.Success => "#2ED573",
                        AuditOutcome.Denied => "#FF6B35",
                        AuditOutcome.TamperDetected => "#FF4757",
                        _ => "#A0A0B0"
                    }
                });

            AuditLogList.ItemsSource = new ObservableCollection<AuditLogDisplayItem>(entries);
        }
    }

    private void ShowSelfActivateStatus(string message, bool isError)
    {
        SelfActivateStatusBorder.Visibility = Visibility.Visible;
        SelfActivateStatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        SelfActivateStatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        SelfActivateStatusText.Text = message;
    }
}

public class AuditLogDisplayItem
{
    public string TimestampDisplay { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string OutcomeColor { get; set; } = "#A0A0B0";
}
