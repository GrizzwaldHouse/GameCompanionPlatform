namespace ArcadiaTracker.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// ViewModel for the Expansion Advisor view.
/// </summary>
public sealed partial class ExpansionAdvisorViewModel : ObservableObject
{
    [ObservableProperty]
    private ExpansionPlan? _plan;

    [ObservableProperty]
    private ObservableCollection<ExpansionSite> _recommendedSites = [];

    [ObservableProperty]
    private ObservableCollection<ResourceDeposit> _nearbyResources = [];

    [ObservableProperty]
    private ObservableCollection<ExpansionWarning> _warnings = [];

    [ObservableProperty]
    private ExpansionReadiness? _readiness;

    [ObservableProperty]
    private BaseStatistics? _baseStats;

    [ObservableProperty]
    private bool _isReady;

    [ObservableProperty]
    private int _siteCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private ExpansionSite? _selectedSite;

    public void UpdatePlan(ExpansionPlan plan)
    {
        Plan = plan;
        RecommendedSites = new ObservableCollection<ExpansionSite>(plan.RecommendedSites);
        NearbyResources = new ObservableCollection<ResourceDeposit>(plan.NearbyResources);
        Warnings = new ObservableCollection<ExpansionWarning>(plan.Warnings);
        Readiness = plan.Readiness;
        BaseStats = plan.CurrentBaseStats;
        IsReady = plan.Readiness.IsReady;
        SiteCount = plan.RecommendedSites.Count;
        WarningCount = plan.Warnings.Count;
    }
}
