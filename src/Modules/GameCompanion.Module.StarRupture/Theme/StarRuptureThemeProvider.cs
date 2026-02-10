namespace GameCompanion.Module.StarRupture.Theme;

using GameCompanion.Core.Interfaces;

/// <summary>
/// Provides sci-fi themed colors and resources for StarRupture.
/// </summary>
public sealed class StarRuptureThemeProvider : IThemeProvider
{
    public string ThemeId => "starrupture";
    public string ThemeName => "StarRupture Sci-Fi";

    // Sci-fi color palette
    public string PrimaryColor => "#00D4FF";      // Neon cyan / plasma blue
    public string SecondaryColor => "#8B5CF6";    // Purple / violet
    public string BackgroundColor => "#0A0A0F";   // Deep space black
    public string SurfaceColor => "#1A1A2E";      // Dark surface
    public string ErrorColor => "#FF4757";        // Alert red

    // Additional theme colors (not in interface but useful)
    public string AccentColor => "#00FF88";       // Matrix green
    public string WarningColor => "#FF6B35";      // Warning orange
    public string SuccessColor => "#2ED573";      // Success green
    public string HologramColor => "#3D9BF0";     // Hologram blue
    public string TextPrimaryColor => "#FFFFFF";  // White text
    public string TextSecondaryColor => "#A0A0B0";// Muted text

    public Uri GetAppIcon()
    {
        // Pack URI for embedded resource
        return new Uri("pack://application:,,,/GameCompanion.Module.StarRupture;component/Assets/icon.png", UriKind.Absolute);
    }

    public Uri? GetSplashImage()
    {
        return new Uri("pack://application:,,,/GameCompanion.Module.StarRupture;component/Assets/splash.png", UriKind.Absolute);
    }
}
