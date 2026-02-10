namespace GameCompanion.Module.SaveModifier.Services;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;
using GameCompanion.Engine.Entitlements.Services;
using GameCompanion.Engine.SaveSafety.Interfaces;
using GameCompanion.Module.SaveModifier.Interfaces;
using GameCompanion.Module.SaveModifier.Models;

/// <summary>
/// Orchestrates the full save modification flow with safety guarantees:
/// 1. Validate capability entitlement
/// 2. Verify consent has been given
/// 3. Validate save file integrity
/// 4. Generate preview (read-only)
/// 5. Require explicit user confirmation
/// 6. Create backup before writing
/// 7. Apply modifications atomically
/// 8. Audit log the action
///
/// Never auto-applies. Never writes partial saves. Never modifies without confirmation.
/// </summary>
public sealed class SaveModificationOrchestrator
{
    private readonly IEntitlementService _entitlementService;
    private readonly IConsentService _consentService;
    private readonly IBackupService _backupService;
    private readonly LocalAuditLogger _auditLogger;
    private readonly Dictionary<string, ISaveModifierAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);

    public SaveModificationOrchestrator(
        IEntitlementService entitlementService,
        IConsentService consentService,
        IBackupService backupService,
        LocalAuditLogger auditLogger)
    {
        _entitlementService = entitlementService;
        _consentService = consentService;
        _backupService = backupService;
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Registers a game-specific adapter.
    /// </summary>
    public void RegisterAdapter(ISaveModifierAdapter adapter)
    {
        _adapters[adapter.GameId] = adapter;
    }

    /// <summary>
    /// Step 1: Verify the user has entitlement and consent for save modification.
    /// </summary>
    public async Task<Result<Unit>> VerifyAccessAsync(string gameId, CancellationToken ct = default)
    {
        // Check capability
        var capResult = await _entitlementService.CheckEntitlementAsync(
            CapabilityActions.SaveModify, gameId, ct);
        if (capResult.IsFailure)
            return Result<Unit>.Failure(capResult.Error!);

        // Check consent
        var consentInfo = _consentService.GetConsentInfo(gameId);
        var consentResult = await _consentService.HasConsentAsync(gameId, consentInfo.Version, ct);
        if (consentResult.IsFailure)
            return Result<Unit>.Failure(consentResult.Error!);

        if (!consentResult.Value!)
            return Result<Unit>.Failure("CONSENT_REQUIRED");

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Step 2: Get all modifiable fields for a save (read-only).
    /// </summary>
    public async Task<Result<IReadOnlyList<ModifiableField>>> GetModifiableFieldsAsync(
        string gameId,
        string savePath,
        CancellationToken ct = default)
    {
        var accessResult = await VerifyAccessAsync(gameId, ct);
        if (accessResult.IsFailure)
            return Result<IReadOnlyList<ModifiableField>>.Failure(accessResult.Error!);

        if (!_adapters.TryGetValue(gameId, out var adapter))
            return Result<IReadOnlyList<ModifiableField>>.Failure($"No save modifier adapter for game: {gameId}");

        return await adapter.GetModifiableFieldsAsync(savePath, ct);
    }

    /// <summary>
    /// Step 3: Preview modifications (read-only diff visualization).
    /// </summary>
    public async Task<Result<SaveModificationPreview>> PreviewAsync(
        string gameId,
        string savePath,
        IReadOnlyList<FieldModification> modifications,
        CancellationToken ct = default)
    {
        var accessResult = await VerifyAccessAsync(gameId, ct);
        if (accessResult.IsFailure)
            return Result<SaveModificationPreview>.Failure(accessResult.Error!);

        if (!_adapters.TryGetValue(gameId, out var adapter))
            return Result<SaveModificationPreview>.Failure($"No save modifier adapter for game: {gameId}");

        // Validate save is in modifiable state
        var validResult = await adapter.ValidateSaveForModificationAsync(savePath, ct);
        if (validResult.IsFailure)
            return Result<SaveModificationPreview>.Failure(validResult.Error!);

        return await adapter.PreviewModificationsAsync(savePath, modifications, ct);
    }

    /// <summary>
    /// Step 4: Apply modifications with full safety guarantees.
    /// Caller must have already called PreviewAsync and received user confirmation.
    /// </summary>
    public async Task<Result<SaveModificationResult>> ApplyAsync(
        string gameId,
        string savePath,
        IReadOnlyList<FieldModification> modifications,
        bool userConfirmed,
        CancellationToken ct = default)
    {
        if (!userConfirmed)
            return Result<SaveModificationResult>.Failure("User confirmation is required before applying modifications.");

        var accessResult = await VerifyAccessAsync(gameId, ct);
        if (accessResult.IsFailure)
            return Result<SaveModificationResult>.Failure(accessResult.Error!);

        if (!_adapters.TryGetValue(gameId, out var adapter))
            return Result<SaveModificationResult>.Failure($"No save modifier adapter for game: {gameId}");

        // Validate save again immediately before write
        var validResult = await adapter.ValidateSaveForModificationAsync(savePath, ct);
        if (validResult.IsFailure)
        {
            await LogAuditAsync(gameId, "apply_modifications", AuditOutcome.Denied,
                $"Save validation failed: {validResult.Error}", ct);
            return Result<SaveModificationResult>.Failure(validResult.Error!);
        }

        // Create mandatory backup before any write
        var saveId = Path.GetFileName(savePath);
        var backupResult = await _backupService.CreateBackupAsync(
            saveId,
            $"Pre-modification backup ({modifications.Count} field(s))",
            BackupSource.PreEdit,
            ct);

        if (backupResult.IsFailure)
        {
            await LogAuditAsync(gameId, "apply_modifications", AuditOutcome.Denied,
                $"Backup creation failed: {backupResult.Error}", ct);
            return Result<SaveModificationResult>.Failure($"Cannot proceed without backup: {backupResult.Error}");
        }

        // Apply modifications atomically
        var applyResult = await adapter.ApplyModificationsAsync(savePath, modifications, ct);

        if (applyResult.IsFailure)
        {
            // Attempt rollback from backup
            await _backupService.RestoreBackupAsync(backupResult.Value!.Id, ct);

            await LogAuditAsync(gameId, "apply_modifications", AuditOutcome.Denied,
                $"Modification failed, rolled back: {applyResult.Error}", ct);
            return Result<SaveModificationResult>.Failure($"Modification failed and was rolled back: {applyResult.Error}");
        }

        await LogAuditAsync(gameId, "apply_modifications", AuditOutcome.Success,
            $"Applied {modifications.Count} modification(s) to {saveId}", ct);

        return applyResult;
    }

    private async Task LogAuditAsync(
        string gameScope,
        string action,
        AuditOutcome outcome,
        string detail,
        CancellationToken ct)
    {
        var entry = new AuditEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = action,
            CapabilityId = CapabilityActions.SaveModify,
            GameScope = gameScope,
            Detail = detail,
            Outcome = outcome
        };
        await _auditLogger.LogAsync(entry, ct);
    }
}
