using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;
using GameCompanion.Engine.Entitlements.Services;
using Microsoft.Win32;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Hardened admin panel with:
/// - Admin status display (scope, expiry, method)
/// - Break-glass emergency access
/// - Activation code generation (single + batch)
/// - Self-activation
/// - Capability inspector
/// - System diagnostics
/// - Entitlement repair tools
/// - Audit log viewer with export
/// - Revoke admin functionality
///
/// Only loads when admin capability is valid. Cannot be triggered accidentally.
/// </summary>
public partial class AdminPanelView : UserControl
{
    private IActivationCodeService? _activationService;
    private IEntitlementService? _entitlementService;
    private IAdminTokenService? _adminTokenService;
    private AdminCapabilityProvider? _adminProvider;
    private LocalAuditLogger? _auditLogger;
    private TamperDetector? _tamperDetector;
    private ICapabilityStore? _capabilityStore;
    private string _gameScope = "star_rupture";
    private string? _currentChallenge;

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
        string gameScope,
        AdminCapabilityProvider? adminProvider = null,
        IAdminTokenService? adminTokenService = null,
        TamperDetector? tamperDetector = null,
        ICapabilityStore? capabilityStore = null)
    {
        _activationService = activationService;
        _entitlementService = entitlementService;
        _auditLogger = auditLogger;
        _gameScope = gameScope;
        _adminProvider = adminProvider;
        _adminTokenService = adminTokenService;
        _tamperDetector = tamperDetector;
        _capabilityStore = capabilityStore;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        UpdateAdminStatus();
        await RefreshCapabilities();
        await RefreshDiagnostics();
        await RefreshAuditLog();
    }

    private void UpdateAdminStatus()
    {
        var token = _adminProvider?.CurrentToken;

        if (token != null && !token.IsExpired)
        {
            AdminStatusBanner.Background = new SolidColorBrush(Color.FromArgb(30, 46, 213, 115));
            AdminStatusText.Text = "Admin Active";
            AdminStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 213, 115));
            AdminScopeText.Text = $"Scope: {token.Scope}";
            AdminExpiryText.Text = $"Expires: {token.ExpiresAt.ToLocalTime():g}";
            AdminMethodText.Text = $"Via: {token.Method}";
            RevokeAdminButton.Visibility = Visibility.Visible;
            BreakGlassSection.Visibility = Visibility.Collapsed;
        }
        else
        {
            AdminStatusBanner.Background = new SolidColorBrush(Color.FromArgb(30, 255, 107, 53));
            AdminStatusText.Text = token?.IsExpired == true ? "Admin Token Expired" : "Admin Inactive";
            AdminStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 53));
            AdminScopeText.Text = "";
            AdminExpiryText.Text = "";
            AdminMethodText.Text = "";
            RevokeAdminButton.Visibility = Visibility.Collapsed;

            // Show break-glass if token service available but no valid token
            if (_adminTokenService != null)
            {
                BreakGlassSection.Visibility = Visibility.Visible;
                _currentChallenge = _adminTokenService.GenerateBreakGlassChallenge();
                BreakGlassChallengeText.Text = _currentChallenge;
            }
        }
    }

    private async void RevokeAdminButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Revoke admin access? This will delete the admin token and revoke admin capabilities.\n\nYou will need to re-authenticate to regain access.",
            "Confirm Revoke",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        if (_adminProvider != null)
        {
            await _adminProvider.RevokeAdminAsync(_gameScope);
            UpdateAdminStatus();
            await RefreshCapabilities();
            await RefreshAuditLog();
        }
    }

    private async void BreakGlassActivateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_adminProvider == null || string.IsNullOrEmpty(_currentChallenge)) return;

        var response = BreakGlassResponseBox.Text.Trim();
        if (string.IsNullOrEmpty(response))
        {
            ShowBreakGlassStatus("Enter the response code.", isError: true);
            return;
        }

        BreakGlassActivateButton.IsEnabled = false;
        try
        {
            var result = await _adminProvider.ActivateBreakGlassAsync(
                _currentChallenge, response, _gameScope);

            if (result.IsSuccess)
            {
                ShowBreakGlassStatus("Emergency admin access granted (4 hours).", isError: false);
                UpdateAdminStatus();
                await RefreshCapabilities();
                await RefreshAuditLog();
                FeaturesActivated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowBreakGlassStatus($"Failed: {result.Error}", isError: true);
            }
        }
        finally
        {
            BreakGlassActivateButton.IsEnabled = true;
        }
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
        await RefreshCapabilities();
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

        await RefreshCapabilities();
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

    // --- Capability Inspector ---

    private async void RefreshCapabilitiesButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshCapabilities();
    }

    private async Task RefreshCapabilities()
    {
        if (_entitlementService == null) return;

        var allActions = CapabilityActions.GetAllPaidActions()
            .Concat([CapabilityActions.AdminSaveOverride, CapabilityActions.AdminCapabilityIssue])
            .ToList();

        var items = new List<CapabilityDisplayItem>();

        foreach (var action in allActions)
        {
            var result = await _entitlementService.CheckEntitlementAsync(action, _gameScope);
            if (result.IsSuccess)
            {
                var cap = result.Value!;
                var remaining = cap.ExpiresAt.HasValue
                    ? cap.ExpiresAt.Value - DateTimeOffset.UtcNow
                    : (TimeSpan?)null;

                items.Add(new CapabilityDisplayItem
                {
                    Action = cap.Action,
                    ScopeDisplay = $"Scope: {cap.GameScope}",
                    ExpiryDisplay = remaining.HasValue
                        ? $"{remaining.Value.Hours}h {remaining.Value.Minutes}m"
                        : "Permanent",
                    ExpiryColor = remaining.HasValue && remaining.Value.TotalHours < 1
                        ? "#FF6B35" : "#2ED573",
                    StatusIcon = cap.IsExpired ? "x" : "o"
                });
            }
        }

        CapabilityList.ItemsSource = new ObservableCollection<CapabilityDisplayItem>(items);
        NoCapabilitiesText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // --- System Diagnostics ---

    private async void RefreshDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDiagnostics();
    }

    private async Task RefreshDiagnostics()
    {
        var items = new List<DiagnosticDisplayItem>();

        if (_adminTokenService != null)
        {
            var diag = await _adminTokenService.GetDiagnosticsAsync();

            items.Add(new DiagnosticDisplayItem
            {
                Label = "Machine Fingerprint",
                Value = diag.MachineFingerprint,
                ValueColor = "#A0A0B0"
            });
            items.Add(new DiagnosticDisplayItem
            {
                Label = "Admin Token",
                Value = diag.HasValidToken ? "Valid" : "None/Expired",
                ValueColor = diag.HasValidToken ? "#2ED573" : "#FF6B35"
            });
            if (diag.HasValidToken)
            {
                items.Add(new DiagnosticDisplayItem
                {
                    Label = "  Token Scope",
                    Value = diag.TokenScope ?? "—"
                });
                items.Add(new DiagnosticDisplayItem
                {
                    Label = "  Token Expires",
                    Value = diag.TokenExpiresAt?.ToLocalTime().ToString("g") ?? "—"
                });
                items.Add(new DiagnosticDisplayItem
                {
                    Label = "  Activation Method",
                    Value = diag.ActivationMethod?.ToString() ?? "—"
                });
            }
            items.Add(new DiagnosticDisplayItem
            {
                Label = "Store Integrity",
                Value = diag.StoreIntegrityOk ? "OK" : "FAILED",
                ValueColor = diag.StoreIntegrityOk ? "#2ED573" : "#FF4757"
            });
            items.Add(new DiagnosticDisplayItem
            {
                Label = "Store Size",
                Value = FormatBytes(diag.StoreSizeBytes)
            });
            items.Add(new DiagnosticDisplayItem
            {
                Label = "Total Audit Entries",
                Value = diag.TotalAuditEntries.ToString()
            });
            items.Add(new DiagnosticDisplayItem
            {
                Label = "Last Admin Action",
                Value = diag.LastAdminAction?.ToLocalTime().ToString("g") ?? "Never"
            });
        }
        else
        {
            items.Add(new DiagnosticDisplayItem
            {
                Label = "Admin Token Service",
                Value = "Not available",
                ValueColor = "#FF6B35"
            });
        }

        DiagnosticsList.ItemsSource = new ObservableCollection<DiagnosticDisplayItem>(items);
    }

    // --- Repair Tools ---

    private async void PurgeExpiredButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capabilityStore == null)
        {
            ShowRepairStatus("Capability store not available.", isError: true);
            return;
        }

        var result = await _capabilityStore.PurgeExpiredAsync();
        if (result.IsSuccess)
        {
            ShowRepairStatus($"Purged expired capabilities. {result.Value} removed.", isError: false);
            await RefreshCapabilities();
        }
        else
        {
            ShowRepairStatus($"Purge failed: {result.Error}", isError: true);
        }
    }

    private async void VerifyIntegrityButton_Click(object sender, RoutedEventArgs e)
    {
        if (_tamperDetector == null)
        {
            ShowRepairStatus("Tamper detector not available.", isError: true);
            return;
        }

        var entitlementsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArcadiaTracker", "entitlements");

        var storeFile = Path.Combine(entitlementsDir, "capabilities.dat");
        if (!File.Exists(storeFile))
        {
            ShowRepairStatus("No capability store file found.", isError: true);
            return;
        }

        var integrityResult = await _tamperDetector.VerifyIntegrityAsync(storeFile);
        var ok = integrityResult.IsSuccess && integrityResult.Value!;
        ShowRepairStatus(
            ok ? "Store integrity verified. No tampering detected."
               : "INTEGRITY FAILURE: Store may have been tampered with.",
            isError: !ok);
    }

    private async void ExportAuditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_auditLogger == null) return;

        var result = await _auditLogger.ReadAllAsync();
        if (result.IsFailure)
        {
            ShowRepairStatus($"Failed to read audit log: {result.Error}", isError: true);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"ArcadiaTracker_AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExt = ".json",
            Filter = "JSON Files (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            var entries = result.Value!;
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(dialog.FileName, json);
            ShowRepairStatus($"Exported {entries.Count} audit entries to {Path.GetFileName(dialog.FileName)}", isError: false);
        }
    }

    // --- Audit Log ---

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

    // --- Helpers ---

    private void ShowSelfActivateStatus(string message, bool isError)
    {
        SelfActivateStatusBorder.Visibility = Visibility.Visible;
        SelfActivateStatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        SelfActivateStatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        SelfActivateStatusText.Text = message;
    }

    private void ShowBreakGlassStatus(string message, bool isError)
    {
        BreakGlassStatusBorder.Visibility = Visibility.Visible;
        BreakGlassStatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        BreakGlassStatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        BreakGlassStatusText.Text = message;
    }

    private void ShowRepairStatus(string message, bool isError)
    {
        RepairStatusBorder.Visibility = Visibility.Visible;
        RepairStatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        RepairStatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        RepairStatusText.Text = message;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}

public class CapabilityDisplayItem
{
    public string Action { get; set; } = "";
    public string ScopeDisplay { get; set; } = "";
    public string ExpiryDisplay { get; set; } = "";
    public string ExpiryColor { get; set; } = "#A0A0B0";
    public string StatusIcon { get; set; } = "";
}

public class DiagnosticDisplayItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string ValueColor { get; set; } = "#E0E0E8";
}

public class AuditLogDisplayItem
{
    public string TimestampDisplay { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string OutcomeColor { get; set; } = "#A0A0B0";
}
