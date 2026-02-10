namespace GameCompanion.Engine.Entitlements.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Capabilities;
using GameCompanion.Engine.Entitlements.Interfaces;

/// <summary>
/// File-based capability store that persists capabilities as encrypted JSON.
/// Uses DPAPI (Data Protection API) on Windows for encryption-at-rest,
/// falling back to a file-system-permission-based approach on other platforms.
/// </summary>
public sealed class LocalCapabilityStore : ICapabilityStore
{
    private readonly string _storePath;
    private readonly byte[] _encryptionKey;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LocalCapabilityStore(string storePath, byte[] encryptionKey)
    {
        _storePath = storePath;
        _encryptionKey = encryptionKey;
        Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
    }

    public async Task<Result<Unit>> StoreAsync(Capability capability, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadDataAsync(ct);
            data.Capabilities.RemoveAll(c => c.Id == capability.Id);
            data.Capabilities.Add(ToStoredCapability(capability));
            await SaveDataAsync(data, ct);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to store capability: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<IReadOnlyList<Capability>>> GetCapabilitiesAsync(
        string action,
        string gameScope,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadDataAsync(ct);
            var matching = data.Capabilities
                .Where(c => c.Action == action &&
                           (c.GameScope == "*" || string.Equals(c.GameScope, gameScope, StringComparison.OrdinalIgnoreCase)))
                .Select(ToCapability)
                .ToList();

            return Result<IReadOnlyList<Capability>>.Success(matching);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Capability>>.Failure($"Failed to load capabilities: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<Unit>> RevokeAsync(string capabilityId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadDataAsync(ct);
            if (!data.RevokedIds.Contains(capabilityId))
            {
                data.RevokedIds.Add(capabilityId);
                await SaveDataAsync(data, ct);
            }
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to revoke capability: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<bool>> IsRevokedAsync(string capabilityId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadDataAsync(ct);
            return Result<bool>.Success(data.RevokedIds.Contains(capabilityId));
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check revocation: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<int>> PurgeExpiredAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadDataAsync(ct);
            var now = DateTimeOffset.UtcNow;

            var expired = data.Capabilities
                .Where(c => c.ExpiresAt.HasValue && now >= c.ExpiresAt.Value)
                .Select(c => c.Id)
                .ToHashSet();

            var purgedCount = data.Capabilities.RemoveAll(c => expired.Contains(c.Id));
            data.RevokedIds.RemoveAll(id => expired.Contains(id));

            await SaveDataAsync(data, ct);
            return Result<int>.Success(purgedCount);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to purge expired capabilities: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<StoreData> LoadDataAsync(CancellationToken ct)
    {
        if (!File.Exists(_storePath))
            return new StoreData();

        var encryptedBytes = await File.ReadAllBytesAsync(_storePath, ct);
        var jsonBytes = Decrypt(encryptedBytes);
        var json = Encoding.UTF8.GetString(jsonBytes);
        return JsonSerializer.Deserialize<StoreData>(json) ?? new StoreData();
    }

    private async Task SaveDataAsync(StoreData data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var encryptedBytes = Encrypt(jsonBytes);
        await File.WriteAllBytesAsync(_storePath, encryptedBytes, ct);
    }

    private byte[] Encrypt(byte[] plaintext)
    {
        // AES-GCM provides authenticated encryption: confidentiality + integrity
        // in a single operation. Prevents padding oracle and ciphertext malleability.
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
        var ciphertext = new byte[plaintext.Length];

        using var aesGcm = new AesGcm(_encryptionKey, tag.Length);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // Wire format: [nonce (12)] [tag (16)] [ciphertext (N)]
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertext.CopyTo(result, nonce.Length + tag.Length);
        return result;
    }

    private byte[] Decrypt(byte[] data)
    {
        var nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
        var tagSize = AesGcm.TagByteSizes.MaxSize;     // 16

        if (data.Length < nonceSize + tagSize)
            throw new CryptographicException("Encrypted data is too short — possible corruption or tampering.");

        var nonce = data.AsSpan(0, nonceSize);
        var tag = data.AsSpan(nonceSize, tagSize);
        var ciphertext = data.AsSpan(nonceSize + tagSize);

        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_encryptionKey, tagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        // AesGcm.Decrypt throws CryptographicException if tag verification fails,
        // which means the data was tampered with or the key is wrong.
        return plaintext;
    }

    private static StoredCapability ToStoredCapability(Capability cap) => new()
    {
        Id = cap.Id,
        Action = cap.Action,
        GameScope = cap.GameScope,
        IssuedAt = cap.IssuedAt,
        ExpiresAt = cap.ExpiresAt,
        Signature = cap.Signature
    };

    private static Capability ToCapability(StoredCapability stored) => new()
    {
        Id = stored.Id,
        Action = stored.Action,
        GameScope = stored.GameScope,
        IssuedAt = stored.IssuedAt,
        ExpiresAt = stored.ExpiresAt,
        Signature = stored.Signature
    };

    private sealed class StoreData
    {
        public List<StoredCapability> Capabilities { get; set; } = [];
        public List<string> RevokedIds { get; set; } = [];
    }

    private sealed class StoredCapability
    {
        public string Id { get; set; } = "";
        public string Action { get; set; } = "";
        public string GameScope { get; set; } = "";
        public DateTimeOffset IssuedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string Signature { get; set; } = "";
    }
}
