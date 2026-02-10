namespace GameCompanion.Module.StarRupture.Services;

using System.Net.Http;
using System.Text.Json;
using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Progression;

/// <summary>
/// Cross-references local badge progress with Steam achievements.
/// Gracefully degrades if Steam API is unavailable.
/// </summary>
public sealed class SteamAchievementService
{
    private const string SteamAppId = "1631270"; // StarRupture Steam App ID
    private readonly ProgressionAnalyzerService _analyzer;
    private static readonly HttpClient HttpClient = new();

    public SteamAchievementService(ProgressionAnalyzerService analyzer)
    {
        _analyzer = analyzer;
    }

    /// <summary>
    /// Builds achievement summary combining local badges with Steam data (if available).
    /// </summary>
    public async Task<Result<AchievementSummary>> GetAchievementSummaryAsync(
        StarRuptureSave save,
        string? steamApiKey = null,
        string? steamUserId = null,
        CancellationToken ct = default)
    {
        try
        {
            var progress = _analyzer.AnalyzeSave(save);
            var allBadges = Badges.AllBadges;

            // Try to get Steam achievements if API key is available
            Dictionary<string, (bool unlocked, DateTime? unlockTime)>? steamData = null;
            Dictionary<string, double>? globalPercentages = null;
            bool steamAvailable = false;

            if (!string.IsNullOrEmpty(steamApiKey) && !string.IsNullOrEmpty(steamUserId))
            {
                try
                {
                    steamData = await FetchSteamAchievementsAsync(steamApiKey, steamUserId, ct);
                    globalPercentages = await FetchGlobalPercentagesAsync(steamApiKey, ct);
                    steamAvailable = true;
                }
                catch
                {
                    // Steam API unavailable, continue with local data only
                }
            }

            var achievements = allBadges.Select(badge =>
            {
                var isEarnedLocally = progress.EarnedBadges.Any(e => e.Id == badge.Id);
                bool? isUnlockedOnSteam = steamData?.ContainsKey(badge.Id) == true
                    ? steamData[badge.Id].unlocked
                    : null;
                DateTime? steamUnlockTime = steamData?.ContainsKey(badge.Id) == true
                    ? steamData[badge.Id].unlockTime
                    : null;
                double? globalPercent = globalPercentages?.ContainsKey(badge.Id) == true
                    ? globalPercentages[badge.Id]
                    : null;

                return new SteamAchievementStatus
                {
                    AchievementId = badge.Id,
                    Name = badge.Name,
                    Description = badge.Description,
                    Icon = badge.Icon,
                    IsEarnedLocally = isEarnedLocally,
                    IsUnlockedOnSteam = isUnlockedOnSteam,
                    GlobalUnlockPercentage = globalPercent,
                    SteamUnlockTime = steamUnlockTime,
                };
            }).ToList();

            var summary = new AchievementSummary
            {
                TotalAchievements = achievements.Count,
                EarnedLocally = achievements.Count(a => a.IsEarnedLocally),
                UnlockedOnSteam = achievements.Count(a => a.IsUnlockedOnSteam == true),
                Mismatches = achievements.Count(a => a.HasMismatch),
                SteamApiAvailable = steamAvailable,
                Achievements = achievements,
            };

            return Result<AchievementSummary>.Success(summary);
        }
        catch (Exception ex)
        {
            return Result<AchievementSummary>.Failure($"Failed to get achievement summary: {ex.Message}");
        }
    }

    private static async Task<Dictionary<string, (bool unlocked, DateTime? unlockTime)>> FetchSteamAchievementsAsync(
        string apiKey,
        string steamId,
        CancellationToken ct)
    {
        var url = $"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?appid={SteamAppId}&key={apiKey}&steamid={steamId}&format=json";
        var response = await HttpClient.GetStringAsync(url, ct);
        var doc = JsonDocument.Parse(response);

        var result = new Dictionary<string, (bool, DateTime?)>();

        if (doc.RootElement.TryGetProperty("playerstats", out var playerStats) &&
            playerStats.TryGetProperty("achievements", out var achievements))
        {
            foreach (var achievement in achievements.EnumerateArray())
            {
                var apiName = achievement.GetProperty("apiname").GetString() ?? "";
                var achieved = achievement.GetProperty("achieved").GetInt32() == 1;
                DateTime? unlockTime = null;
                if (achievement.TryGetProperty("unlocktime", out var ut) && ut.GetInt64() > 0)
                {
                    unlockTime = DateTimeOffset.FromUnixTimeSeconds(ut.GetInt64()).DateTime;
                }
                result[apiName] = (achieved, unlockTime);
            }
        }

        return result;
    }

    private static async Task<Dictionary<string, double>> FetchGlobalPercentagesAsync(
        string apiKey,
        CancellationToken ct)
    {
        var url = $"https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/?gameid={SteamAppId}&format=json";
        var response = await HttpClient.GetStringAsync(url, ct);
        var doc = JsonDocument.Parse(response);

        var result = new Dictionary<string, double>();

        if (doc.RootElement.TryGetProperty("achievementpercentages", out var percentages) &&
            percentages.TryGetProperty("achievements", out var achievements))
        {
            foreach (var achievement in achievements.EnumerateArray())
            {
                var name = achievement.GetProperty("name").GetString() ?? "";
                var percent = achievement.GetProperty("percent").GetDouble();
                result[name] = percent;
            }
        }

        return result;
    }
}
