namespace GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Steam achievement status combining local badge data with Steam achievement state.
/// </summary>
public sealed class SteamAchievementStatus
{
    public required string AchievementId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }

    /// <summary>Whether the badge is earned locally (from save analysis).</summary>
    public bool IsEarnedLocally { get; init; }

    /// <summary>Whether the achievement is unlocked on Steam (if API available).</summary>
    public bool? IsUnlockedOnSteam { get; init; }

    /// <summary>Global unlock percentage from Steam (if available).</summary>
    public double? GlobalUnlockPercentage { get; init; }

    /// <summary>When the achievement was unlocked on Steam.</summary>
    public DateTime? SteamUnlockTime { get; init; }

    /// <summary>Whether there's a mismatch between local and Steam state.</summary>
    public bool HasMismatch => IsEarnedLocally && IsUnlockedOnSteam == false;

    public string StatusDisplay
    {
        get
        {
            return (IsEarnedLocally, IsUnlockedOnSteam) switch
            {
                (true, true) => "✅ Unlocked",
                (true, false) => "⚠️ Earned locally (not on Steam)",
                (true, null) => "🏆 Earned",
                (false, true) => "🔓 Unlocked on Steam",
                (false, _) => "🔒 Locked",
            };
        }
    }
}

/// <summary>
/// Summary of all achievement progress.
/// </summary>
public sealed class AchievementSummary
{
    public required int TotalAchievements { get; init; }
    public required int EarnedLocally { get; init; }
    public required int UnlockedOnSteam { get; init; }
    public required int Mismatches { get; init; }
    public required bool SteamApiAvailable { get; init; }
    public IReadOnlyList<SteamAchievementStatus> Achievements { get; init; } = [];

    public double CompletionPercentage => TotalAchievements > 0 ? (double)EarnedLocally / TotalAchievements : 0;
}
