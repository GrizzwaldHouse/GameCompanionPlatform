namespace GameCompanion.Engine.Entitlements.Services;

using System.Security.Cryptography;

/// <summary>
/// Provides signing and encryption keys for the capability system.
/// Keys are derived from a machine-specific seed using HKDF, ensuring
/// they are unique per installation but deterministic for the same machine.
/// </summary>
public static class SigningKeyProvider
{
    private static readonly byte[] SigningContext = "ArcadiaTracker.Capability.Signing.v1"u8.ToArray();
    private static readonly byte[] EncryptionContext = "ArcadiaTracker.Capability.Encryption.v1"u8.ToArray();

    /// <summary>
    /// Derives a 256-bit signing key from a machine-specific seed.
    /// </summary>
    public static byte[] DeriveSigningKey(byte[] machineSeed)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, machineSeed, 32, SigningContext);
    }

    /// <summary>
    /// Derives a 256-bit encryption key from a machine-specific seed.
    /// </summary>
    public static byte[] DeriveEncryptionKey(byte[] machineSeed)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, machineSeed, 32, EncryptionContext);
    }

    /// <summary>
    /// Generates a machine-specific seed based on the machine name and user profile path.
    /// This provides a deterministic seed that varies per installation.
    /// </summary>
    public static byte[] GetMachineSeed()
    {
        var machineId = $"{Environment.MachineName}|{Environment.UserName}|ArcadiaTracker";
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(machineId));
    }
}
