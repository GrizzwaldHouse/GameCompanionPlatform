namespace GameCompanion.Core.Interfaces;

/// <summary>
/// Provides theming resources for a game module.
/// Each game has its own visual identity (Minecraft: blocky/green, StarRupture: sci-fi/neon).
/// </summary>
public interface IThemeProvider
{
    /// <summary>
    /// Unique identifier for this theme (matches GameId).
    /// </summary>
    string ThemeId { get; }

    /// <summary>
    /// Display name for the theme.
    /// </summary>
    string ThemeName { get; }

    /// <summary>
    /// Primary accent color as hex (e.g., "#5cb85c" for Minecraft green).
    /// </summary>
    string PrimaryColor { get; }

    /// <summary>
    /// Secondary accent color as hex.
    /// </summary>
    string SecondaryColor { get; }

    /// <summary>
    /// Background color as hex.
    /// </summary>
    string BackgroundColor { get; }

    /// <summary>
    /// Surface/card color as hex.
    /// </summary>
    string SurfaceColor { get; }

    /// <summary>
    /// Error/warning color as hex.
    /// </summary>
    string ErrorColor { get; }

    /// <summary>
    /// URI to the app icon resource.
    /// </summary>
    Uri GetAppIcon();

    /// <summary>
    /// URI to the splash screen image resource.
    /// </summary>
    Uri? GetSplashImage();
}
