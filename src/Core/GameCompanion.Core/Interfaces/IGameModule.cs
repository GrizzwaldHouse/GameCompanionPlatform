namespace GameCompanion.Core.Interfaces;

using GameCompanion.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contract for a game-specific module. Each supported game (Minecraft, StarRupture, etc.)
/// implements this interface to provide its progression map, save handling, theming, and services.
/// </summary>
public interface IGameModule
{
    /// <summary>
    /// Unique identifier for this game (e.g., "minecraft", "starrupture").
    /// </summary>
    string GameId { get; }

    /// <summary>
    /// Display name for the game (e.g., "Minecraft", "StarRupture").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Version of this module.
    /// </summary>
    Version ModuleVersion { get; }

    /// <summary>
    /// Gets the progression map defining all phases and steps for this game.
    /// </summary>
    IProgressionMap GetProgressionMap();

    /// <summary>
    /// Gets all editable save fields with their definitions.
    /// </summary>
    IReadOnlyList<ISaveFieldDefinition> GetEditableFields();

    /// <summary>
    /// Gets risk level classifications for save fields.
    /// Key is field ID, value is the risk level.
    /// </summary>
    IReadOnlyDictionary<string, RiskLevel> GetFieldRiskClassifications();

    /// <summary>
    /// Gets the theme provider for this game's visual identity.
    /// </summary>
    IThemeProvider GetThemeProvider();

    /// <summary>
    /// Gets UI copy/text specific to this game.
    /// Key is the copy ID, value is the localized text.
    /// </summary>
    IReadOnlyDictionary<string, string> GetUICopy();

    /// <summary>
    /// Registers game-specific services with the DI container.
    /// </summary>
    void RegisterServices(IServiceCollection services);
}
