namespace GameCompanion.Engine.Entitlements.Capabilities;

using System.Security.Cryptography;
using System.Text;
using GameCompanion.Core.Models;

/// <summary>
/// Validates capability tokens by verifying HMAC-SHA256 signatures and checking expiry.
/// This is the enforcement point — all capability checks must flow through here.
/// </summary>
public sealed class CapabilityValidator
{
    private readonly byte[] _signingKey;

    public CapabilityValidator(byte[] signingKey)
    {
        if (signingKey.Length < 32)
            throw new ArgumentException("Signing key must be at least 256 bits (32 bytes).", nameof(signingKey));

        _signingKey = signingKey;
    }

    /// <summary>
    /// Validates a capability token for a specific action and game scope.
    /// Returns success only if the signature is valid, the capability is not expired,
    /// and the action/game scope match.
    /// </summary>
    public Result<Capability> Validate(Capability capability, string requiredAction, string gameScope)
    {
        // Check signature integrity first
        if (!VerifySignature(capability))
            return Result<Capability>.Failure("Invalid capability signature.");

        // Check expiry
        if (capability.IsExpired)
            return Result<Capability>.Failure("Capability has expired.");

        // Check action match
        if (!string.Equals(capability.Action, requiredAction, StringComparison.Ordinal))
            return Result<Capability>.Failure("Capability action mismatch.");

        // Check game scope (wildcard "*" matches all games)
        if (capability.GameScope != "*" &&
            !string.Equals(capability.GameScope, gameScope, StringComparison.OrdinalIgnoreCase))
            return Result<Capability>.Failure("Capability game scope mismatch.");

        return Result<Capability>.Success(capability);
    }

    /// <summary>
    /// Computes the expected HMAC-SHA256 signature for a capability.
    /// </summary>
    internal string ComputeSignature(Capability capability)
    {
        var canonical = capability.ToCanonicalString();
        var payload = Encoding.UTF8.GetBytes(canonical);
        var hash = HMACSHA256.HashData(_signingKey, payload);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies that a capability's signature matches the expected HMAC.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    private bool VerifySignature(Capability capability)
    {
        var expected = ComputeSignature(capability);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(capability.Signature));
    }
}
