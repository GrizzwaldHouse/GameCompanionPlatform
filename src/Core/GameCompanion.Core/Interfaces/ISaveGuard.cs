namespace GameCompanion.Core.Interfaces;

using GameCompanion.Core.Enums;
using GameCompanion.Core.Models;

/// <summary>
/// Guards save file operations by enforcing backup policies and validating edits
/// based on risk level classifications.
/// </summary>
public interface ISaveGuard
{
    /// <summary>
    /// Ensures a backup exists before performing an operation at the given risk level.
    /// Creates a backup if necessary based on the risk matrix.
    /// </summary>
    Task<Result<Unit>> EnsureBackupAsync(string saveId, RiskLevel minimumRisk, CancellationToken ct = default);

    /// <summary>
    /// Validates whether an edit can be performed on a field.
    /// Returns failure if the field is read-only or requires advanced mode.
    /// </summary>
    Task<Result<bool>> ValidateEditAsync(string fieldId, object newValue, CancellationToken ct = default);

    /// <summary>
    /// Whether advanced mode is currently enabled, allowing HIGH risk field edits.
    /// </summary>
    bool IsAdvancedModeEnabled { get; }

    /// <summary>
    /// Enables advanced mode after user confirms with the expected confirmation code.
    /// </summary>
    void EnableAdvancedMode(string confirmationCode);

    /// <summary>
    /// Disables advanced mode, re-locking HIGH risk fields.
    /// </summary>
    void DisableAdvancedMode();
}
