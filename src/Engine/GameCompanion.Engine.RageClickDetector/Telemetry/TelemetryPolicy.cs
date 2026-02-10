namespace GameCompanion.Engine.RageClickDetector.Telemetry;

/// <summary>
/// Enforces privacy-safe telemetry policy. Defines what data capture is strictly allowed
/// and explicitly disallowed per the rage-click detector specification.
///
/// ALLOWED:
/// - anonymized_session_id (ephemeral)
/// - ui_element_id (hashed)
/// - interaction_type (click, submit, nav)
/// - timestamp
/// - screen_name
///
/// EXPLICITLY DISALLOWED:
/// - User identity
/// - Save file contents
/// - Input text
/// - IP address
/// - Device fingerprinting
/// </summary>
public static class TelemetryPolicy
{
    /// <summary>
    /// Maximum length for anonymized session IDs to prevent accidental data leakage.
    /// </summary>
    public const int MaxSessionIdLength = 64;

    /// <summary>
    /// Maximum length for UI element IDs.
    /// </summary>
    public const int MaxElementIdLength = 128;

    /// <summary>
    /// Maximum length for screen names.
    /// </summary>
    public const int MaxScreenNameLength = 128;

    /// <summary>
    /// Validates that a telemetry field conforms to privacy policy.
    /// Returns true if the field is allowed.
    /// </summary>
    public static bool ValidateSessionId(string sessionId)
        => !string.IsNullOrWhiteSpace(sessionId) && sessionId.Length <= MaxSessionIdLength;

    /// <summary>
    /// Validates that a UI element ID is a hashed/safe identifier.
    /// </summary>
    public static bool ValidateElementId(string elementId)
        => !string.IsNullOrWhiteSpace(elementId) && elementId.Length <= MaxElementIdLength;

    /// <summary>
    /// Validates that a screen name is safe to capture.
    /// </summary>
    public static bool ValidateScreenName(string screenName)
        => !string.IsNullOrWhiteSpace(screenName) && screenName.Length <= MaxScreenNameLength;
}
