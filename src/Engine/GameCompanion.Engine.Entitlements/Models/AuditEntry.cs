namespace GameCompanion.Engine.Entitlements.Models;

/// <summary>
/// An immutable audit log entry recording a capability-related action.
/// Stored locally for accountability without requiring telemetry.
/// </summary>
public sealed class AuditEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Action { get; init; }
    public required string CapabilityId { get; init; }
    public required string GameScope { get; init; }
    public required string Detail { get; init; }
    public required AuditOutcome Outcome { get; init; }
}

public enum AuditOutcome
{
    Success,
    Denied,
    Revoked,
    Expired,
    TamperDetected
}
