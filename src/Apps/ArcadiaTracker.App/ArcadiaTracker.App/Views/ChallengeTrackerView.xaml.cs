using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Challenge tracker view showing challenge progress and objectives.
/// </summary>
public partial class ChallengeTrackerView : UserControl
{
    private ChallengeTracker? _currentTracker;
    private Challenge? _selectedChallenge;
    private ChallengeFilter _currentFilter = ChallengeFilter.All;

    public ChallengeTrackerView()
    {
        InitializeComponent();
    }

    public void UpdateChallenges(ChallengeTracker tracker)
    {
        _currentTracker = tracker;

        // Header completion count
        CompletionCountText.Text = $"{tracker.CompletedCount} / {tracker.TotalCount}";

        // Summary stats
        CompletedCountText.Text = tracker.CompletedCount.ToString();
        InProgressCountText.Text = tracker.InProgress.Count.ToString();
        CompletionPercentText.Text = $"{tracker.CompletionPercent:F1}%";

        // Update progress circle
        UpdateProgressCircle(tracker.CompletionPercent);

        // Apply current filter
        ApplyFilter(_currentFilter);
    }

    public void ShowLoading(bool isLoading)
    {
        LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            ErrorOverlay.Visibility = Visibility.Collapsed;
        }
        else
        {
            ErrorMessageText.Text = errorMessage;
            ErrorOverlay.Visibility = Visibility.Visible;
        }
    }

    private void UpdateProgressCircle(double percent)
    {
        // Circle circumference = 2 * π * r (r = 40, so circumference ≈ 251.2)
        var circumference = 251.2;
        var offset = circumference - (percent / 100.0 * circumference);
        ProgressCircle.StrokeDashOffset = offset;
        ProgressCircleText.Text = $"{percent:F0}%";
    }

    private void ApplyFilter(ChallengeFilter filter)
    {
        _currentFilter = filter;

        if (_currentTracker == null)
            return;

        var filtered = filter switch
        {
            ChallengeFilter.InProgress => _currentTracker.Challenges
                .Where(c => !c.IsCompleted && _currentTracker.InProgress.Any(p => p.Challenge.Id == c.Id)),
            ChallengeFilter.Completed => _currentTracker.Challenges.Where(c => c.IsCompleted),
            _ => _currentTracker.Challenges
        };

        var displayItems = filtered.Select(c => new ChallengeDisplayItem(c, _currentTracker.InProgress)).ToList();
        ChallengesList.ItemsSource = displayItems;
        NoChallengesText.Visibility = displayItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void FilterAll_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(ChallengeFilter.All);
    }

    private void FilterInProgress_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(ChallengeFilter.InProgress);
    }

    private void FilterCompleted_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(ChallengeFilter.Completed);
    }

    private void Challenge_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is ChallengeDisplayItem item)
        {
            ShowChallengeDetails(item.Challenge);
        }
    }

    private void ShowChallengeDetails(Challenge challenge)
    {
        _selectedChallenge = challenge;

        NoSelectionText.Visibility = Visibility.Collapsed;
        DetailsPanel.Visibility = Visibility.Visible;

        // Basic info
        DetailName.Text = challenge.Name;
        DetailDescription.Text = challenge.Description;
        DetailDifficulty.Text = challenge.Difficulty.ToString();

        // Difficulty color
        var difficultyBrush = GetDifficultyBrush(challenge.Difficulty);
        DetailDifficultyBadge.Background = difficultyBrush;

        // Objectives
        var objectiveItems = challenge.Objectives
            .Select(o => new ObjectiveDisplayItem(o))
            .ToList();
        ObjectivesList.ItemsSource = objectiveItems;

        // Rewards
        var rewardItems = challenge.Rewards
            .Select(r => new RewardDisplayItem(r))
            .ToList();
        RewardsList.ItemsSource = rewardItems;

        // Completion status
        if (challenge.IsCompleted && challenge.CompletedAt.HasValue)
        {
            CompletionStatusBorder.Background = (Brush)FindResource("SuccessBrush");
            CompletionStatusBorder.Visibility = Visibility.Visible;
            CompletionDateText.Text = $"Completed on {challenge.CompletedAt.Value:MMM dd, yyyy HH:mm}";
        }
        else
        {
            CompletionStatusBorder.Visibility = Visibility.Collapsed;
        }
    }

    private static Brush GetDifficultyBrush(ChallengeDifficulty difficulty)
    {
        return difficulty switch
        {
            ChallengeDifficulty.Easy => new SolidColorBrush(Color.FromRgb(46, 213, 115)), // Green
            ChallengeDifficulty.Medium => new SolidColorBrush(Color.FromRgb(254, 211, 48)), // Yellow
            ChallengeDifficulty.Hard => new SolidColorBrush(Color.FromRgb(255, 107, 53)), // Orange
            ChallengeDifficulty.Expert => new SolidColorBrush(Color.FromRgb(235, 59, 90)), // Red
            ChallengeDifficulty.Insane => new SolidColorBrush(Color.FromRgb(153, 69, 255)), // Purple
            _ => new SolidColorBrush(Color.FromRgb(96, 96, 112)) // Muted
        };
    }
}

public enum ChallengeFilter
{
    All,
    InProgress,
    Completed
}

public class ChallengeDisplayItem
{
    public ChallengeDisplayItem(Challenge challenge, IReadOnlyList<ChallengeProgress> inProgress)
    {
        Challenge = challenge;
        Name = challenge.Name;
        Description = challenge.Description;
        DifficultyDisplay = challenge.Difficulty.ToString();
        CategoryDisplay = $"📁 {challenge.Category}";

        // Get progress
        var progress = inProgress.FirstOrDefault(p => p.Challenge.Id == challenge.Id);
        ProgressValue = challenge.IsCompleted ? 100 : (progress?.OverallProgress ?? 0);
        ProgressDisplay = challenge.IsCompleted
            ? "Completed"
            : $"{ProgressValue:F0}%";

        // Color-coded by difficulty
        DifficultyBrush = challenge.Difficulty switch
        {
            ChallengeDifficulty.Easy => new SolidColorBrush(Color.FromRgb(46, 213, 115)),
            ChallengeDifficulty.Medium => new SolidColorBrush(Color.FromRgb(254, 211, 48)),
            ChallengeDifficulty.Hard => new SolidColorBrush(Color.FromRgb(255, 107, 53)),
            ChallengeDifficulty.Expert => new SolidColorBrush(Color.FromRgb(235, 59, 90)),
            ChallengeDifficulty.Insane => new SolidColorBrush(Color.FromRgb(153, 69, 255)),
            _ => new SolidColorBrush(Color.FromRgb(96, 96, 112))
        };

        // Background and border
        if (challenge.IsCompleted)
        {
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(26, 46, 213, 115)); // Subtle green tint
            BorderBrush = new SolidColorBrush(Color.FromRgb(46, 213, 115));
        }
        else if (progress != null && progress.OverallProgress > 0)
        {
            BackgroundBrush = new SolidColorBrush(Color.FromRgb(42, 42, 62)); // SurfaceLightBrush
            BorderBrush = new SolidColorBrush(Color.FromRgb(96, 217, 232)); // PrimaryBrush
        }
        else
        {
            BackgroundBrush = new SolidColorBrush(Color.FromRgb(26, 26, 46)); // SurfaceBrush (darker)
            BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Transparent
        }
    }

    public Challenge Challenge { get; }
    public string Name { get; }
    public string Description { get; }
    public string DifficultyDisplay { get; }
    public string CategoryDisplay { get; }
    public double ProgressValue { get; }
    public string ProgressDisplay { get; }
    public Brush DifficultyBrush { get; }
    public Brush BackgroundBrush { get; }
    public Brush BorderBrush { get; }
}

public class ObjectiveDisplayItem
{
    public ObjectiveDisplayItem(ChallengeObjective objective)
    {
        Description = objective.Description;
        Progress = objective.Progress;
        ProgressText = $"{objective.Current} / {objective.Target}";
        StatusIcon = objective.IsCompleted ? "✅" : "⏳";
    }

    public string Description { get; }
    public double Progress { get; }
    public string ProgressText { get; }
    public string StatusIcon { get; }
}

public class RewardDisplayItem
{
    public RewardDisplayItem(ChallengeReward reward)
    {
        DisplayText = $"{reward.Description} ({reward.Amount})";
    }

    public string DisplayText { get; }
}
