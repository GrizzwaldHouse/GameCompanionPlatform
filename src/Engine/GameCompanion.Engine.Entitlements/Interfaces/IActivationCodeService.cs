namespace GameCompanion.Engine.Entitlements.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Validates and redeems activation codes to grant capability-based feature access.
/// Codes are verified using HMAC and then converted to machine-bound capabilities.
/// </summary>
public interface IActivationCodeService
{
    /// <summary>
    /// Validates an activation code without redeeming it.
    /// Returns the parsed code if valid, or a failure with an explanation.
    /// </summary>
    Result<ActivationCode> Validate(string code);

    /// <summary>
    /// Redeems an activation code: validates it, checks it hasn't been used before,
    /// then issues the corresponding capabilities for the given game scope.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> RedeemAsync(
        string code,
        string gameScope,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether a code has already been redeemed on this machine.
    /// </summary>
    Task<bool> IsRedeemedAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Generates a valid activation code for a given bundle (admin/seller tool).
    /// This would typically run server-side or in a separate admin utility.
    /// </summary>
    string GenerateCode(ActivationBundle bundle);
}
