using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Activation code entry view — allows users to redeem codes for premium features.
/// </summary>
public partial class ActivationView : UserControl
{
    private IActivationCodeService? _activationService;
    private IEntitlementService? _entitlementService;
    private string _gameScope = "star_rupture";

    /// <summary>
    /// Raised when features are successfully activated, signaling the main window
    /// to re-evaluate which premium nav items to show.
    /// </summary>
    public event EventHandler? FeaturesActivated;

    public ActivationView()
    {
        InitializeComponent();
    }

    public void Initialize(
        IActivationCodeService activationService,
        IEntitlementService entitlementService,
        string gameScope)
    {
        _activationService = activationService;
        _entitlementService = entitlementService;
        _gameScope = gameScope;
        _ = RefreshActiveFeatures();
    }

    private async void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activationService == null) return;

        var code = CodeTextBox.Text.Trim();
        ActivateButton.IsEnabled = false;

        try
        {
            var result = await _activationService.RedeemAsync(code, _gameScope);

            if (result.IsSuccess)
            {
                ShowStatus($"Activated {result.Value!.Count} feature(s) successfully!", isError: false);
                CodeTextBox.Text = "ARCA-";
                await RefreshActiveFeatures();
                FeaturesActivated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowStatus(result.Error!, isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Activation failed: {ex.Message}", isError: true);
        }
        finally
        {
            ActivateButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CodeTextBox.Text = "ARCA-";
        StatusBorder.Visibility = Visibility.Collapsed;
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusBorder.Visibility = Visibility.Visible;
        StatusBorder.Background = new SolidColorBrush(
            isError ? Color.FromArgb(40, 255, 71, 87) : Color.FromArgb(40, 46, 213, 115));
        StatusText.Foreground = new SolidColorBrush(
            isError ? Color.FromRgb(255, 71, 87) : Color.FromRgb(46, 213, 115));
        StatusText.Text = message;
    }

    private async Task RefreshActiveFeatures()
    {
        if (_entitlementService == null) return;

        var activeFeatures = new ObservableCollection<string>();
        var featureNames = new Dictionary<string, string>
        {
            [CapabilityActions.SaveModify] = "Save Modifier",
            [CapabilityActions.SaveInspect] = "Save Inspector",
            [CapabilityActions.BackupManage] = "Backup Manager",
            [CapabilityActions.UiThemes] = "Theme Customizer",
            [CapabilityActions.AnalyticsOptimizer] = "Efficiency Optimizer",
            [CapabilityActions.AlertsMilestones] = "Milestones & Alerts",
            [CapabilityActions.ExportPro] = "Export Pro",
            [CapabilityActions.AnalyticsCompare] = "Multi-Save Compare",
            [CapabilityActions.AnalyticsReplay] = "Session Replay"
        };

        foreach (var (action, name) in featureNames)
        {
            var result = await _entitlementService.CheckEntitlementAsync(action, _gameScope);
            if (result.IsSuccess)
                activeFeatures.Add(name);
        }

        ActiveFeaturesList.ItemsSource = activeFeatures;
        NoFeaturesText.Visibility = activeFeatures.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
