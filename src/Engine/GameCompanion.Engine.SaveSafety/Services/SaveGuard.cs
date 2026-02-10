namespace GameCompanion.Engine.SaveSafety.Services;

using GameCompanion.Core.Enums;
using GameCompanion.Core.Interfaces;
using GameCompanion.Core.Models;
using GameCompanion.Engine.SaveSafety.Interfaces;

/// <summary>
/// Implementation of ISaveGuard that enforces backup policies and validates edits
/// based on the game module's risk classifications.
/// </summary>
public sealed class SaveGuard : ISaveGuard
{
    private const string AdvancedModeConfirmationCode = "I UNDERSTAND THE RISKS";

    private readonly IBackupService _backupService;
    private readonly IGameModule _gameModule;
    private bool _advancedModeEnabled;

    public SaveGuard(IBackupService backupService, IGameModule gameModule)
    {
        _backupService = backupService;
        _gameModule = gameModule;
    }

    public bool IsAdvancedModeEnabled => _advancedModeEnabled;

    public async Task<Result<Unit>> EnsureBackupAsync(
        string saveId,
        RiskLevel minimumRisk,
        CancellationToken ct = default)
    {
        // LOW risk: backup optional
        if (minimumRisk == RiskLevel.Low)
        {
            return Result<Unit>.Success(Unit.Value);
        }

        // MEDIUM and above: mandatory backup
        var backupResult = await _backupService.CreateBackupAsync(
            saveId,
            $"Pre-edit backup (Risk: {minimumRisk})",
            BackupSource.PreEdit,
            ct);

        return backupResult.Match(
            _ => Result<Unit>.Success(Unit.Value),
            error => Result<Unit>.Failure($"Failed to create backup: {error}"));
    }

    public Task<Result<bool>> ValidateEditAsync(
        string fieldId,
        object newValue,
        CancellationToken ct = default)
    {
        var risks = _gameModule.GetFieldRiskClassifications();

        // Unknown field
        if (!risks.TryGetValue(fieldId, out var risk))
        {
            return Task.FromResult(Result<bool>.Failure($"Unknown field: {fieldId}"));
        }

        // CRITICAL fields are always read-only
        if (risk == RiskLevel.Critical)
        {
            return Task.FromResult(Result<bool>.Failure(
                $"Field '{fieldId}' is read-only and cannot be edited. " +
                "This field is critical to save file integrity."));
        }

        // HIGH fields require advanced mode
        if (risk == RiskLevel.High && !_advancedModeEnabled)
        {
            return Task.FromResult(Result<bool>.Failure(
                $"Field '{fieldId}' is a high-risk field. " +
                "Enable Advanced Mode to edit this field."));
        }

        // Run game-specific validation if available
        var field = _gameModule.GetEditableFields()
            .FirstOrDefault(f => f.FieldId == fieldId);

        if (field?.Validator != null)
        {
            var validation = field.Validator(newValue);
            if (!validation.IsValid)
            {
                return Task.FromResult(Result<bool>.Failure(validation.ErrorMessage!));
            }
        }

        return Task.FromResult(Result<bool>.Success(true));
    }

    public void EnableAdvancedMode(string confirmationCode)
    {
        if (confirmationCode == AdvancedModeConfirmationCode)
        {
            _advancedModeEnabled = true;
        }
    }

    public void DisableAdvancedMode()
    {
        _advancedModeEnabled = false;
    }
}
