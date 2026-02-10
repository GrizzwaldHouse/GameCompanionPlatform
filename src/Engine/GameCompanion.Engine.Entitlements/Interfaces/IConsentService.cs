namespace GameCompanion.Engine.Entitlements.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// Manages user consent for save modification features.
/// Consent is required before first use and is logged locally.
/// </summary>
public interface IConsentService
{
    /// <summary>
    /// Checks whether consent has been given for a specific game scope and consent version.
    /// </summary>
    Task<Result<bool>> HasConsentAsync(string gameScope, int consentVersion, CancellationToken ct = default);

    /// <summary>
    /// Records that the user has accepted the consent screen.
    /// </summary>
    Task<Result<Unit>> RecordConsentAsync(ConsentRecord record, CancellationToken ct = default);

    /// <summary>
    /// Gets the current consent text and version for display.
    /// </summary>
    ConsentInfo GetConsentInfo(string gameScope);
}

/// <summary>
/// The consent text and metadata to display to users.
/// </summary>
public sealed class ConsentInfo
{
    public required int Version { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string AcceptButtonText { get; init; }
    public required string DeclineButtonText { get; init; }
}
