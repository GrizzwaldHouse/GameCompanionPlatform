namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Research view showing tech tree unlock status.
/// </summary>
public sealed partial class ResearchViewModel : ObservableObject
{
    [ObservableProperty]
    private ResearchTreeData? _tree;

    [ObservableProperty]
    private ObservableCollection<ResearchCategory> _categories = [];

    [ObservableProperty]
    private double _unlockPercent;

    [ObservableProperty]
    private string _unlockProgressText = "0 / 0";

    [ObservableProperty]
    private string _selectedCategoryName = "All";

    [ObservableProperty]
    private ObservableCollection<ResearchNode> _displayedNodes = [];

    public void UpdateTree(ResearchTreeData tree)
    {
        Tree = tree;
        Categories = new ObservableCollection<ResearchCategory>(tree.Categories);
        UnlockPercent = tree.UnlockPercent;
        UnlockProgressText = $"{tree.UnlockedRecipes} / {tree.TotalRecipes}";
        SelectedCategoryName = "All";
        DisplayedNodes = new ObservableCollection<ResearchNode>(
            tree.Categories.SelectMany(c => c.Nodes).OrderBy(n => n.Name));
    }

    public void FilterByCategory(string categoryName)
    {
        SelectedCategoryName = categoryName;
        if (Tree == null) return;

        if (categoryName == "All")
        {
            DisplayedNodes = new ObservableCollection<ResearchNode>(
                Tree.Categories.SelectMany(c => c.Nodes).OrderBy(n => n.Name));
        }
        else
        {
            var cat = Tree.Categories.FirstOrDefault(c => c.Name == categoryName);
            DisplayedNodes = cat != null
                ? new ObservableCollection<ResearchNode>(cat.Nodes.OrderBy(n => n.Name))
                : [];
        }
    }
}
