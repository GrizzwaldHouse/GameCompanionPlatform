using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for BlueprintView.xaml
/// </summary>
public partial class BlueprintView : UserControl
{
    private Blueprint? _selectedBlueprint;

    public BlueprintView()
    {
        InitializeComponent();
    }

    public void UpdateLibrary(BlueprintLibrary library)
    {
        TotalCountText.Text = library.TotalCount.ToString();
        CategoryCountText.Text = library.Categories.Count.ToString();

        // Update category filter
        CategoryFilterCombo.Items.Clear();
        CategoryFilterCombo.Items.Add("All");
        foreach (var category in library.Categories)
        {
            CategoryFilterCombo.Items.Add(category);
        }
        CategoryFilterCombo.SelectedIndex = 0;

        // Update blueprint list
        BlueprintList.ItemsSource = library.Blueprints;

        // Hide loading, show empty state
        LoadingIndicator.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
        DetailsHeader.Visibility = Visibility.Collapsed;
        DetailsContent.Visibility = Visibility.Collapsed;
    }

    public void ShowLoading(bool isLoading)
    {
        LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

        if (isLoading)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            DetailsHeader.Visibility = Visibility.Collapsed;
            DetailsContent.Visibility = Visibility.Collapsed;
        }
    }

    public void ShowError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            ErrorText.Text = errorMessage;
            ErrorPanel.Visibility = Visibility.Visible;
        }
    }

    public void ShowDetails(Blueprint blueprint)
    {
        _selectedBlueprint = blueprint;

        // Update header
        DetailsName.Text = blueprint.Name;
        DetailsDescription.Text = blueprint.Description;

        // Update stats
        DetailsEntityCount.Text = blueprint.Stats.EntityCount.ToString();
        DetailsUniqueTypes.Text = blueprint.Stats.UniqueTypes.ToString();
        DetailsPower.Text = $"{blueprint.Stats.EstimatedPower:F1} MW";
        DetailsDimensions.Text = $"{blueprint.Bounds.Width:F1} x {blueprint.Bounds.Height:F1}";

        // Update entity breakdown
        EntityBreakdownList.ItemsSource = blueprint.Stats.EntityCounts
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        // Update metadata
        DetailsCreated.Text = blueprint.CreatedAt.ToString("g");
        DetailsModified.Text = blueprint.ModifiedAt.ToString("g");

        // Update selected count
        SelectedCountText.Text = $"{blueprint.Stats.EntityCount}";

        // Show details
        EmptyState.Visibility = Visibility.Collapsed;
        DetailsHeader.Visibility = Visibility.Visible;
        DetailsContent.Visibility = Visibility.Visible;
    }

    private void BlueprintItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is Blueprint blueprint)
        {
            ShowDetails(blueprint);
        }
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryFilterCombo.SelectedItem is string category)
        {
            FilterByCategoryRequested?.Invoke(this, category == "All" ? null : category);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBlueprint != null)
        {
            ExportRequested?.Invoke(this, _selectedBlueprint);
        }
    }

    // Events
    public event EventHandler? RefreshRequested;
    public event EventHandler<Blueprint>? ExportRequested;
    public event EventHandler<string?>? FilterByCategoryRequested;
}
