namespace GameCompanion.Engine.Entitlements.Capabilities;

using System.Security.Cryptography;

/// <summary>
/// Issues signed capability tokens. In production, issuance would be server-side only.
/// For local/dev scenarios, this can issue capabilities using a local signing key.
/// </summary>
public sealed class CapabilityIssuer
{
    private readonly CapabilityValidator _validator;

    public CapabilityIssuer(CapabilityValidator validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Issues a new signed capability for the given action and game scope.
    /// </summary>
    public Capability Issue(string action, string gameScope, TimeSpan? lifetime = null)
    {
        var now = DateTimeOffset.UtcNow;
        var capability = new Capability
        {
            Id = GenerateCapabilityId(),
            Action = action,
            GameScope = gameScope,
            IssuedAt = now,
            ExpiresAt = lifetime.HasValue ? now.Add(lifetime.Value) : null,
            Signature = "" // Placeholder — will be replaced after signing
        };

        // Sign the capability
        var signature = _validator.ComputeSignature(capability);

        return new Capability
        {
            Id = capability.Id,
            Action = capability.Action,
            GameScope = capability.GameScope,
            IssuedAt = capability.IssuedAt,
            ExpiresAt = capability.ExpiresAt,
            Signature = signature
        };
    }

    private static string GenerateCapabilityId()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
