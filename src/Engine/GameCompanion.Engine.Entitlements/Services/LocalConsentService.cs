namespace GameCompanion.Engine.Entitlements.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// File-based consent tracking. Stores consent records as JSON locally.
/// No telemetry, no network calls — purely local accountability.
/// </summary>
public sealed class LocalConsentService : IConsentService
{
    private const int CurrentConsentVersion = 1;

    private static readonly string ConsentBody = string.Join(Environment.NewLine,
        "This tool modifies your local, single-player save file.",
        "",
        "You're always in control. Nothing is changed automatically,",
        "and a backup is created before any edits are applied.",
        "",
        "A few things to keep in mind:",
        "",
        "  - Use only with saves you own",
        "  - Some games may not support modified saves",
        "  - Online or competitive modes are not supported",
        "",
        "A backup will be created automatically before any changes are made.");

    private readonly string _consentFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LocalConsentService(string consentFilePath)
    {
        _consentFilePath = consentFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(consentFilePath)!);
    }

    public async Task<Result<bool>> HasConsentAsync(string gameScope, int consentVersion, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var records = await LoadRecordsAsync(ct);
            var hasConsent = records.Any(r =>
                r.GameScope == gameScope &&
                r.ConsentVersion >= consentVersion);
            return Result<bool>.Success(hasConsent);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check consent: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<Unit>> RecordConsentAsync(ConsentRecord record, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var records = await LoadRecordsAsync(ct);
            records.Add(record);
            await SaveRecordsAsync(records, ct);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to record consent: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public ConsentInfo GetConsentInfo(string gameScope)
    {
        return new ConsentInfo
        {
            Version = CurrentConsentVersion,
            Title = "Before You Continue",
            Body = ConsentBody,
            AcceptButtonText = "Continue",
            DeclineButtonText = "Cancel"
        };
    }

    /// <summary>
    /// Computes a SHA-256 hash of the consent text for the given game scope.
    /// Stored in the consent record to prove exactly which text was agreed to.
    /// </summary>
    public static string ComputeConsentHash(string gameScope)
    {
        var text = $"{gameScope}|v{CurrentConsentVersion}|{ConsentBody}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<List<ConsentRecord>> LoadRecordsAsync(CancellationToken ct)
    {
        if (!File.Exists(_consentFilePath))
            return [];

        var json = await File.ReadAllTextAsync(_consentFilePath, ct);
        return JsonSerializer.Deserialize<List<ConsentRecord>>(json) ?? [];
    }

    private async Task SaveRecordsAsync(List<ConsentRecord> records, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_consentFilePath, json, ct);
    }
}
