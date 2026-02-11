namespace ArcadiaTracker.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using GameCompanion.Module.StarRupture.Services;

/// <summary>
/// ViewModel for comparing two save sessions.
/// </summary>
public sealed partial class SessionDiffViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _beforeSessionName;

    [ObservableProperty]
    private string? _afterSessionName;

    [ObservableProperty]
    private SaveDifference? _difference;

    [ObservableProperty]
    private string _statusMessage = "Select two saves to compare";

    [ObservableProperty]
    private bool _hasComparison;

    [ObservableProperty]
    private string _summary = "";

    // Delta display properties
    [ObservableProperty]
    private string _playTimeDelta = "";

    [ObservableProperty]
    private string _dataPointsDelta = "";

    [ObservableProperty]
    private string _waveChange = "";

    [ObservableProperty]
    private string _entitiesBuiltDisplay = "";

    [ObservableProperty]
    private string _entitiesDestroyedDisplay = "";

    [ObservableProperty]
    private string _newRecipesDisplay = "";

    public void UpdateComparison(SaveDifference diff)
    {
        Difference = diff;
        HasComparison = true;

        BeforeSessionName = diff.BeforeSession;
        AfterSessionName = diff.AfterSession;

        PlayTimeDelta = $"+{diff.PlayTimeDelta:hh\\:mm\\:ss}";
        DataPointsDelta = FormatDelta(diff.DataPointsDelta);
        WaveChange = diff.WaveChange;
        EntitiesBuiltDisplay = diff.EntitiesBuilt > 0 ? $"+{diff.EntitiesBuilt}" : "0";
        EntitiesDestroyedDisplay = diff.EntitiesDestroyed > 0 ? $"-{diff.EntitiesDestroyed}" : "0";
        NewRecipesDisplay = diff.NewUnlockedRecipes.Count > 0
            ? $"+{diff.NewUnlockedRecipes.Count} recipes"
            : "No new recipes";

        Summary = SessionDiffService.GenerateSummary(diff);
        StatusMessage = "Comparison complete";
    }

    public void ClearComparison()
    {
        Difference = null;
        HasComparison = false;
        Summary = "";
        StatusMessage = "Select two saves to compare";
    }

    private static string FormatDelta(int value)
    {
        return value switch
        {
            > 0 => $"+{value}",
            < 0 => value.ToString(),
            _ => "0"
        };
    }
}
