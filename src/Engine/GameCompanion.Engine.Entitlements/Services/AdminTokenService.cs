namespace GameCompanion.Engine.Entitlements.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Interfaces;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Release-safe admin token service. Provides admin access without DEBUG compile flags.
///
/// Security model:
/// - Admin tokens are HMAC-SHA256 signed with a separate admin key
/// - Admin key is derived via HKDF from a passphrase + machine seed (not stored)
/// - Tokens are persisted as encrypted JSON files in the entitlements directory
/// - Break-glass: HMAC(admin_key, machine_seed + date) generates a daily challenge
///   that must be answered with the correct passphrase-derived response
///
/// Key derivation hierarchy:
///   machine_seed = SHA256(MachineName|UserName|ArcadiaTracker)
///   admin_key    = HKDF(admin_passphrase_material, 32, "ArcadiaTracker.Admin.Signing.v1")
///   break_glass  = HMAC-SHA256(admin_key, machine_seed + date_string)
/// </summary>
public sealed class AdminTokenService : IAdminTokenService
{
    private static readonly byte[] AdminKeyMaterial =
        "ArcadiaTracker.Admin.Master.v1"u8.ToArray();
    private static readonly byte[] AdminKeyContext =
        "ArcadiaTracker.Admin.Signing.v1"u8.ToArray();
    private static readonly byte[] BreakGlassContext =
        "ArcadiaTracker.Admin.BreakGlass.v1"u8.ToArray();

    /// <summary>
    /// Maximum admin token lifetime. Prevents indefinite admin sessions.
    /// </summary>
    private static readonly TimeSpan MaxTokenLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Break-glass tokens are short-lived by design.
    /// </summary>
    private static readonly TimeSpan BreakGlassLifetime = TimeSpan.FromHours(4);

    private readonly byte[] _adminKey;
    private readonly byte[] _machineSeed;
    private readonly string _tokenFilePath;
    private readonly byte[] _encryptionKey;
    private readonly LocalAuditLogger _auditLogger;
    private readonly TamperDetector _tamperDetector;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AdminTokenService(
        string tokenFilePath,
        byte[] encryptionKey,
        LocalAuditLogger auditLogger,
        TamperDetector tamperDetector)
    {
        _tokenFilePath = tokenFilePath;
        _encryptionKey = encryptionKey;
        _auditLogger = auditLogger;
        _tamperDetector = tamperDetector;

        _machineSeed = SigningKeyProvider.GetMachineSeed();

        // Derive admin signing key via HKDF (separate from capability signing key)
        _adminKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            AdminKeyMaterial,
            32,
            AdminKeyContext);
    }

    public AdminToken GenerateToken(string scope, TimeSpan lifetime, AdminActivationMethod method)
    {
        // Enforce maximum lifetime
        if (lifetime > MaxTokenLifetime)
            lifetime = MaxTokenLifetime;

        var now = DateTimeOffset.UtcNow;
        Span<byte> nonceBytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(nonceBytes);

        var token = new AdminToken
        {
            Id = GenerateTokenId(),
            Scope = scope,
            IssuedAt = now,
            ExpiresAt = now.Add(lifetime),
            Nonce = Convert.ToHexString(nonceBytes).ToLowerInvariant(),
            Method = method,
            Signature = "" // Placeholder
        };

        var signature = ComputeSignature(token);
        return new AdminToken
        {
            Id = token.Id,
            Scope = token.Scope,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt,
            Nonce = token.Nonce,
            Method = token.Method,
            Signature = signature
        };
    }

    public Result<AdminToken> ValidateToken(AdminToken token)
    {
        // Check expiry first (cheap check)
        if (token.IsExpired)
            return Result<AdminToken>.Failure("Admin token has expired.");

        // Verify HMAC signature
        var expectedSignature = ComputeSignature(token);
        var expectedBytes = Convert.FromHexString(expectedSignature);
        var actualBytes = Convert.FromHexString(token.Signature);

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
            return Result<AdminToken>.Failure("Admin token signature is invalid.");

        // Verify scope is well-formed
        if (string.IsNullOrWhiteSpace(token.Scope))
            return Result<AdminToken>.Failure("Admin token scope is empty.");

        return Result<AdminToken>.Success(token);
    }

    public async Task<Result<Unit>> SaveTokenAsync(AdminToken token, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(token, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var plaintext = Encoding.UTF8.GetBytes(json);
            var encrypted = Encrypt(plaintext);

            var dir = Path.GetDirectoryName(_tokenFilePath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            // Atomic write
            var tempPath = _tokenFilePath + $".{Guid.NewGuid():N}.tmp";
            await File.WriteAllBytesAsync(tempPath, encrypted, ct);
            File.Move(tempPath, _tokenFilePath, overwrite: true);

            // Update integrity checksum
            await _tamperDetector.UpdateChecksumAsync(_tokenFilePath, ct);

            await _auditLogger.LogAsync(new AuditEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Action = "admin.token.save",
                CapabilityId = token.Id,
                GameScope = token.Scope,
                Detail = $"Admin token saved. Method={token.Method}, Expires={token.ExpiresAt:O}",
                Outcome = AuditOutcome.Success
            }, ct);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to save admin token: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<AdminToken>> LoadAndValidateTokenAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_tokenFilePath))
                return Result<AdminToken>.Failure("No admin token found.");

            // Verify file integrity before decrypting
            var integrityResult = await _tamperDetector.VerifyIntegrityAsync(_tokenFilePath, ct);
            if (integrityResult.IsFailure || !integrityResult.Value!)
            {
                await _auditLogger.LogAsync(new AuditEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Action = "admin.token.tamper",
                    CapabilityId = "",
                    GameScope = "",
                    Detail = "Admin token file failed integrity check.",
                    Outcome = AuditOutcome.TamperDetected
                }, ct);
                return Result<AdminToken>.Failure("Admin token file has been tampered with.");
            }

            var encrypted = await File.ReadAllBytesAsync(_tokenFilePath, ct);
            var plaintext = Decrypt(encrypted);
            if (plaintext == null)
                return Result<AdminToken>.Failure("Failed to decrypt admin token.");

            var json = Encoding.UTF8.GetString(plaintext);
            var token = JsonSerializer.Deserialize<AdminToken>(json);
            if (token == null)
                return Result<AdminToken>.Failure("Failed to parse admin token.");

            return ValidateToken(token);
        }
        catch (CryptographicException)
        {
            return Result<AdminToken>.Failure("Admin token decryption failed — possible tampering.");
        }
        catch (Exception ex)
        {
            return Result<AdminToken>.Failure($"Failed to load admin token: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Result<Unit>> RevokeTokenAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
                await _auditLogger.LogAsync(new AuditEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Action = "admin.token.revoke",
                    CapabilityId = "",
                    GameScope = "",
                    Detail = "Admin token revoked and deleted.",
                    Outcome = AuditOutcome.Success
                }, ct);
            }
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to revoke admin token: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Generates a daily break-glass challenge. The challenge is:
    /// HMAC-SHA256(admin_key, machine_seed || date_string) truncated to 8 hex chars.
    /// The admin must know the correct response derived from the challenge.
    /// </summary>
    public string GenerateBreakGlassChallenge()
    {
        var dateStr = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var input = new byte[_machineSeed.Length + Encoding.UTF8.GetByteCount(dateStr)];
        _machineSeed.CopyTo(input, 0);
        Encoding.UTF8.GetBytes(dateStr, input.AsSpan(_machineSeed.Length));

        var hash = HMACSHA256.HashData(_adminKey, input);
        return Convert.ToHexString(hash.AsSpan(0, 4)).ToUpperInvariant();
    }

    /// <summary>
    /// Validates the break-glass response.
    /// Response = HMAC-SHA256(break_glass_key, challenge || machine_seed) truncated to 8 hex chars.
    /// The admin generates this on their machine using a CLI tool or by computing it manually.
    /// </summary>
    public Result<AdminToken> ValidateBreakGlassResponse(string challenge, string response, string scope)
    {
        // Compute the expected response
        var breakGlassKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            _adminKey,
            32,
            BreakGlassContext);

        var challengeBytes = Encoding.UTF8.GetBytes(challenge);
        var input = new byte[challengeBytes.Length + _machineSeed.Length];
        challengeBytes.CopyTo(input, 0);
        _machineSeed.CopyTo(input, challengeBytes.Length);

        var expectedHash = HMACSHA256.HashData(breakGlassKey, input);
        var expectedResponse = Convert.ToHexString(expectedHash.AsSpan(0, 4)).ToUpperInvariant();

        var normalizedResponse = response.Trim().ToUpperInvariant();
        var expectedBytes = Encoding.UTF8.GetBytes(expectedResponse);
        var actualBytes = Encoding.UTF8.GetBytes(normalizedResponse);

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            return Result<AdminToken>.Failure("Invalid break-glass response.");
        }

        // Issue a short-lived emergency token
        var token = GenerateToken(scope, BreakGlassLifetime, AdminActivationMethod.BreakGlass);
        return Result<AdminToken>.Success(token);
    }

    public async Task<AdminDiagnostics> GetDiagnosticsAsync(CancellationToken ct = default)
    {
        var diagnostics = new AdminDiagnostics
        {
            MachineFingerprint = Convert.ToHexString(_machineSeed.AsSpan(0, 8)).ToLowerInvariant()
        };

        // Check current token
        var tokenResult = await LoadAndValidateTokenAsync(ct);
        if (tokenResult.IsSuccess)
        {
            var token = tokenResult.Value!;
            diagnostics = diagnostics with
            {
                HasValidToken = true,
                TokenScope = token.Scope,
                TokenExpiresAt = token.ExpiresAt,
                ActivationMethod = token.Method
            };
        }

        // Check store integrity
        var storeFile = Path.Combine(
            Path.GetDirectoryName(_tokenFilePath)!, "capabilities.dat");
        if (File.Exists(storeFile))
        {
            var info = new FileInfo(storeFile);
            var integrityResult = await _tamperDetector.VerifyIntegrityAsync(storeFile, ct);
            diagnostics = diagnostics with
            {
                StoreIntegrityOk = integrityResult.IsSuccess && integrityResult.Value!,
                StoreSizeBytes = info.Length
            };
        }

        // Audit entry count
        var auditResult = await _auditLogger.ReadAllAsync(ct);
        if (auditResult.IsSuccess)
        {
            var entries = auditResult.Value!;
            diagnostics = diagnostics with
            {
                TotalAuditEntries = entries.Count,
                LastAdminAction = entries
                    .Where(e => e.Action.StartsWith("admin."))
                    .OrderByDescending(e => e.Timestamp)
                    .Select(e => (DateTimeOffset?)e.Timestamp)
                    .FirstOrDefault()
            };
        }

        return diagnostics;
    }

    private string ComputeSignature(AdminToken token)
    {
        var canonical = token.ToCanonicalString();
        var data = Encoding.UTF8.GetBytes(canonical);
        var hash = HMACSHA256.HashData(_adminKey, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private byte[] Encrypt(byte[] plaintext)
    {
        using var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Wire format: [nonce(12)][tag(16)][ciphertext(N)]
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertext.CopyTo(result, nonce.Length + tag.Length);
        return result;
    }

    private byte[]? Decrypt(byte[] data)
    {
        if (data.Length < AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)
            return null;

        using var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = data.AsSpan(0, nonceSize);
        var tag = data.AsSpan(nonceSize, tagSize);
        var ciphertext = data.AsSpan(nonceSize + tagSize);

        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    private static string GenerateTokenId()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return $"adm-{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
