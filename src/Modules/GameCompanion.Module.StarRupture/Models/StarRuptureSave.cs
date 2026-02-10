namespace GameCompanion.Module.StarRupture.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a parsed StarRupture save file.
/// </summary>
public sealed class StarRuptureSave
{
    public required string FilePath { get; init; }
    public required string SessionName { get; init; }
    public required DateTime SaveTimestamp { get; init; }
    public required TimeSpan PlayTime { get; init; }
    public required StarRuptureGameState GameState { get; init; }
    public required CorporationsData Corporations { get; init; }
    public required CraftingData Crafting { get; init; }
    public required EnviroWaveData EnviroWave { get; init; }
    public SpatialData? Spatial { get; init; }
}

/// <summary>
/// Game state data from save.
/// </summary>
public sealed class StarRuptureGameState
{
    public bool TutorialCompleted { get; init; }
    public double PlaytimeDuration { get; init; }
}

/// <summary>
/// Corporation reputation and unlocks.
/// </summary>
public sealed class CorporationsData
{
    public int DataPoints { get; init; }
    public int UnlockedInventorySlots { get; init; }
    public int UnlockedFeaturesFlags { get; init; }
    public IReadOnlyList<CorporationInfo> Corporations { get; init; } = [];
}

/// <summary>
/// Individual corporation info.
/// </summary>
public sealed class CorporationInfo
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public int CurrentLevel { get; init; }
    public int CurrentXP { get; init; }
}

/// <summary>
/// Crafting recipes and blueprints.
/// </summary>
public sealed class CraftingData
{
    public IReadOnlyList<string> LockedRecipes { get; init; } = [];
    public IReadOnlyList<string> PickedUpItems { get; init; } = [];

    /// <summary>
    /// Number of unlocked recipes (total recipes minus locked).
    /// </summary>
    public int UnlockedRecipeCount { get; init; }

    /// <summary>
    /// Total number of recipes in the game.
    /// </summary>
    public int TotalRecipeCount { get; init; }
}

/// <summary>
/// Environmental wave progression.
/// </summary>
public sealed class EnviroWaveData
{
    public string Wave { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public double Progress { get; init; }
}
