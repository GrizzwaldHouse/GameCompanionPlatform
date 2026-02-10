namespace GameCompanion.Engine.Entitlements.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Validates and redeems ARCA-format activation codes.
///
/// Code structure (10 bytes, hex-encoded to 20 chars, formatted as ARCA-XXXX-XXXX-XXXX-XXXX):
///   [0]     Bundle ID (ActivationBundle enum)
///   [1]     Flags (reserved, currently 0)
///   [2..5]  Random nonce (4 bytes, makes each code unique)
///   [6..9]  HMAC-SHA256 truncated to 4 bytes (authenticity tag)
///
/// Verification uses a fixed activation key derived via HKDF.
/// Once verified, the code's bundle is expanded into capability actions
/// and issued as machine-bound, HMAC-signed capabilities via the entitlement service.
/// </summary>
public sealed class ActivationCodeService : IActivationCodeService
{
    private const string CodePrefix = "ARCA-";
    private const int PayloadSize = 6; // bundle + flags + nonce
    private const int TagSize = 4;     // truncated HMAC
    private const int TotalSize = PayloadSize + TagSize; // 10 bytes = 20 hex chars

    private static readonly byte[] ActivationKeyMaterial =
        "ArcadiaTracker.Activation.Key.v1"u8.ToArray();
    private static readonly byte[] ActivationContext =
        "ArcadiaTracker.Activation.HMAC.v1"u8.ToArray();

    private readonly byte[] _activationKey;
    private readonly IEntitlementService _entitlementService;
    private readonly LocalAuditLogger _auditLogger;
    private readonly string _redeemedCodesPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ActivationCodeService(
        IEntitlementService entitlementService,
        LocalAuditLogger auditLogger,
        string redeemedCodesPath)
    {
        _entitlementService = entitlementService;
        _auditLogger = auditLogger;
        _redeemedCodesPath = redeemedCodesPath;

        // Derive activation key via HKDF from fixed material
        _activationKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ActivationKeyMaterial,
            32,
            ActivationContext);
    }

    public Result<ActivationCode> Validate(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<ActivationCode>.Failure("Activation code cannot be empty.");

        // Normalize: uppercase, remove dashes and prefix
        var normalized = code.Trim().ToUpperInvariant().Replace("-", "");
        if (normalized.StartsWith("ARCA"))
            normalized = normalized[4..];

        // Must be exactly 20 hex chars (10 bytes)
        if (normalized.Length != TotalSize * 2)
            return Result<ActivationCode>.Failure("Invalid activation code format.");

        byte[] bytes;
        try
        {
            bytes = Convert.FromHexString(normalized);
        }
        catch (FormatException)
        {
            return Result<ActivationCode>.Failure("Invalid activation code format.");
        }

        if (bytes.Length != TotalSize)
            return Result<ActivationCode>.Failure("Invalid activation code format.");

        var payload = bytes.AsSpan(0, PayloadSize);
        var providedTag = bytes.AsSpan(PayloadSize, TagSize);

        // Verify HMAC tag
        var expectedTag = ComputeTag(payload);
        if (!CryptographicOperations.FixedTimeEquals(providedTag, expectedTag.AsSpan(0, TagSize)))
            return Result<ActivationCode>.Failure("Invalid activation code.");

        var bundleByte = bytes[0];
        if (!Enum.IsDefined(typeof(ActivationBundle), bundleByte))
            return Result<ActivationCode>.Failure("Unknown feature bundle.");

        return Result<ActivationCode>.Success(new ActivationCode
        {
            Code = FormatCode(bytes),
            Bundle = (ActivationBundle)bundleByte,
            Nonce = bytes[2..6],
            Tag = bytes[6..10]
        });
    }

    public async Task<Result<IReadOnlyList<string>>> RedeemAsync(
        string code,
        string gameScope,
        CancellationToken ct = default)
    {
        var validateResult = Validate(code);
        if (validateResult.IsFailure)
            return Result<IReadOnlyList<string>>.Failure(validateResult.Error!);

        var activationCode = validateResult.Value!;

        // Check if already redeemed
        if (await IsRedeemedAsync(activationCode.Code, ct))
            return Result<IReadOnlyList<string>>.Failure("This code has already been activated.");

        // Expand bundle to capability actions
        var actions = GetBundleActions(activationCode.Bundle);
        var grantedActions = new List<string>();

        foreach (var action in actions)
        {
            var grantResult = await _entitlementService.GrantCapabilityAsync(action, gameScope, ct: ct);
            if (grantResult.IsSuccess)
            {
                grantedActions.Add(action);

                // Audit the grant
                await _auditLogger.LogAsync(new AuditEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Action = "activation.grant",
                    CapabilityId = grantResult.Value!.Id,
                    GameScope = gameScope,
                    Detail = $"Activated {action} via code {activationCode.Code[..9]}...",
                    Outcome = AuditOutcome.Success
                }, ct);
            }
        }

        if (grantedActions.Count == 0)
            return Result<IReadOnlyList<string>>.Failure("Failed to activate any features.");

        // Mark code as redeemed
        await MarkRedeemedAsync(activationCode.Code, ct);

        return Result<IReadOnlyList<string>>.Success(grantedActions);
    }

    public async Task<bool> IsRedeemedAsync(string code, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var redeemed = await LoadRedeemedCodesAsync(ct);
            var normalized = NormalizeCode(code);
            return redeemed.Contains(normalized);
        }
        finally
        {
            _lock.Release();
        }
    }

    public string GenerateCode(ActivationBundle bundle)
    {
        var payload = new byte[PayloadSize];
        payload[0] = (byte)bundle;
        payload[1] = 0; // flags reserved
        RandomNumberGenerator.Fill(payload.AsSpan(2, 4)); // random nonce

        var tag = ComputeTag(payload);
        var full = new byte[TotalSize];
        payload.CopyTo(full, 0);
        tag.AsSpan(0, TagSize).CopyTo(full.AsSpan(PayloadSize));

        return FormatCode(full);
    }

    private byte[] ComputeTag(ReadOnlySpan<byte> payload)
    {
        var payloadArray = payload.ToArray();
        return HMACSHA256.HashData(_activationKey, payloadArray);
    }

    private static string FormatCode(byte[] bytes)
    {
        var hex = Convert.ToHexString(bytes).ToUpperInvariant();
        // Format as ARCA-XXXX-XXXX-XXXX-XXXX-XXXX
        return $"ARCA-{hex[0..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}";
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant().Replace("-", "");
    }

    private async Task MarkRedeemedAsync(string code, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var redeemed = await LoadRedeemedCodesAsync(ct);
            redeemed.Add(NormalizeCode(code));

            var json = JsonSerializer.Serialize(redeemed);
            var dir = Path.GetDirectoryName(_redeemedCodesPath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            // Atomic write
            var tempPath = _redeemedCodesPath + $".{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, _redeemedCodesPath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<HashSet<string>> LoadRedeemedCodesAsync(CancellationToken ct)
    {
        if (!File.Exists(_redeemedCodesPath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(_redeemedCodesPath, ct);
            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Maps a bundle to the set of capability actions it grants.
    /// </summary>
    internal static IReadOnlyList<string> GetBundleActions(ActivationBundle bundle)
    {
        return bundle switch
        {
            ActivationBundle.Pro => CapabilityActions.GetProBundleActions(),
            ActivationBundle.SaveModifier => [CapabilityActions.SaveModify],
            ActivationBundle.SaveInspector => [CapabilityActions.SaveInspect],
            ActivationBundle.BackupManager => [CapabilityActions.BackupManage],
            ActivationBundle.ThemeCustomizer => [CapabilityActions.UiThemes],
            ActivationBundle.Optimizer => [CapabilityActions.AnalyticsOptimizer],
            ActivationBundle.Milestones => [CapabilityActions.AlertsMilestones],
            ActivationBundle.ExportPro => [CapabilityActions.ExportPro],
            _ => []
        };
    }
}
