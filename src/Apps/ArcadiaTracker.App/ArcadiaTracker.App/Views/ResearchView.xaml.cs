using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ArcadiaTracker.App.ViewModels;
using GameCompanion.Module.StarRupture.Models;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Research view showing tech tree unlock status.
/// </summary>
public partial class ResearchView : UserControl
{
    private ResearchTreeData? _currentTree;
    private string _selectedCategory = "All";

    public ResearchView()
    {
        InitializeComponent();
    }

    public void UpdateTree(ResearchTreeData tree)
    {
        _currentTree = tree;

        // Progress bar
        UnlockProgressBar.Value = tree.UnlockPercent;
        UnlockProgressText.Text = $"{tree.UnlockedRecipes} / {tree.TotalRecipes}";

        // Build category filter buttons
        CategoryFilterPanel.Children.Clear();

        // Add "All" button
        var allButton = CreateCategoryButton("All", tree.TotalRecipes);
        CategoryFilterPanel.Children.Add(allButton);

        // Add category-specific buttons
        foreach (var category in tree.Categories.OrderBy(c => c.Order))
        {
            var button = CreateCategoryButton(category.Name, category.TotalCount);
            CategoryFilterPanel.Children.Add(button);
        }

        // Show all recipes initially
        FilterByCategory("All");
    }

    private Button CreateCategoryButton(string categoryName, int recipeCount)
    {
        var button = new Button
        {
            Content = $"{categoryName} ({recipeCount})",
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(16, 8, 16, 8),
            FontWeight = FontWeights.SemiBold,
            BorderThickness = new Thickness(0),
            Background = _selectedCategory == categoryName
                ? (Brush)FindResource("PrimaryBrush")
                : (Brush)FindResource("SurfaceLightBrush"),
            Foreground = _selectedCategory == categoryName
                ? (Brush)FindResource("BackgroundBrush")
                : (Brush)FindResource("TextSecondaryBrush")
        };

        button.Click += (s, e) => FilterByCategory(categoryName);
        return button;
    }

    private void FilterByCategory(string categoryName)
    {
        if (_currentTree == null) return;

        _selectedCategory = categoryName;
        CategoryHeaderText.Text = categoryName == "All"
            ? "All Recipes"
            : $"{categoryName} Recipes";

        // Get filtered recipes
        var recipes = categoryName == "All"
            ? _currentTree.Categories.SelectMany(c => c.Nodes).OrderBy(n => n.Name).ToList()
            : _currentTree.Categories
                .FirstOrDefault(c => c.Name == categoryName)
                ?.Nodes.OrderBy(n => n.Name).ToList() ?? [];

        RecipeList.ItemsSource = recipes;
        NoRecipesText.Visibility = recipes.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Rebuild category buttons to update selected state
        UpdateTree(_currentTree);
    }
}
