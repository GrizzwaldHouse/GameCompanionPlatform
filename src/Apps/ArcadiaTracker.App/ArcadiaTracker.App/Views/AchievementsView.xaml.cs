using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Achievements view showing Steam achievement cross-reference.
/// </summary>
public partial class AchievementsView : UserControl
{
    private AchievementSummary? _currentSummary;
    private AchievementFilter _currentFilter = AchievementFilter.All;

    public AchievementsView()
    {
        InitializeComponent();
    }

    public void UpdateAchievements(AchievementSummary summary)
    {
        _currentSummary = summary;

        // Header completion count
        CompletionCountText.Text = $"{summary.EarnedLocally} / {summary.TotalAchievements}";

        // Summary stats
        EarnedCountText.Text = summary.EarnedLocally.ToString();
        LockedCountText.Text = (summary.TotalAchievements - summary.EarnedLocally).ToString();
        CompletionPercentText.Text = $"{summary.CompletionPercentage * 100:F1}%";

        // Steam API status
        if (summary.SteamApiAvailable)
        {
            SteamStatusIndicator.Background = (Brush)FindResource("SuccessBrush");
            SteamStatusText.Text = "Connected";
        }
        else
        {
            SteamStatusIndicator.Background = (Brush)FindResource("WarningBrush");
            SteamStatusText.Text = "Disconnected";
        }

        // Apply current filter
        ApplyFilter(_currentFilter);
    }

    private void ApplyFilter(AchievementFilter filter)
    {
        _currentFilter = filter;

        if (_currentSummary == null)
            return;

        var filtered = filter switch
        {
            AchievementFilter.Earned => _currentSummary.Achievements.Where(a => a.IsEarnedLocally),
            AchievementFilter.Locked => _currentSummary.Achievements.Where(a => !a.IsEarnedLocally),
            AchievementFilter.Mismatched => _currentSummary.Achievements.Where(a => a.HasMismatch),
            _ => _currentSummary.Achievements
        };

        var displayItems = filtered.Select(a => new AchievementDisplayItem(a)).ToList();
        AchievementsList.ItemsSource = displayItems;
        NoAchievementsText.Visibility = displayItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void FilterAll_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(AchievementFilter.All);
    }

    private void FilterEarned_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(AchievementFilter.Earned);
    }

    private void FilterLocked_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(AchievementFilter.Locked);
    }

    private void FilterMismatched_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(AchievementFilter.Mismatched);
    }
}

public enum AchievementFilter
{
    All,
    Earned,
    Locked,
    Mismatched
}

public class AchievementDisplayItem
{
    public AchievementDisplayItem(SteamAchievementStatus achievement)
    {
        Icon = achievement.Icon;
        Name = achievement.Name;
        Description = achievement.Description;
        StatusDisplay = achievement.StatusDisplay;
        HasGlobalUnlock = achievement.GlobalUnlockPercentage.HasValue;
        GlobalUnlockDisplay = achievement.GlobalUnlockPercentage.HasValue
            ? $"🌍 {achievement.GlobalUnlockPercentage.Value:F1}% unlocked"
            : string.Empty;

        // Color-coded based on status
        if (achievement.IsEarnedLocally)
        {
            BackgroundBrush = new SolidColorBrush(Color.FromRgb(42, 42, 62)); // SurfaceLightBrush
            StatusBadgeBrush = new SolidColorBrush(Color.FromRgb(46, 213, 115)); // SuccessBrush
        }
        else if (achievement.HasMismatch)
        {
            BackgroundBrush = new SolidColorBrush(Color.FromRgb(42, 42, 62)); // SurfaceLightBrush
            StatusBadgeBrush = new SolidColorBrush(Color.FromRgb(255, 107, 53)); // WarningBrush
        }
        else // Locked
        {
            BackgroundBrush = new SolidColorBrush(Color.FromRgb(26, 26, 46)); // SurfaceBrush (darker)
            StatusBadgeBrush = new SolidColorBrush(Color.FromRgb(96, 96, 112)); // TextMutedBrush
        }
    }

    public string Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public string StatusDisplay { get; }
    public bool HasGlobalUnlock { get; }
    public string GlobalUnlockDisplay { get; }
    public Brush BackgroundBrush { get; }
    public Brush StatusBadgeBrush { get; }
}
