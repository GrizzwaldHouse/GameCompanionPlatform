#r "nuget: Microsoft.Extensions.DependencyInjection, 8.0.0"
#r "../src/Modules/GameCompanion.Module.StarRupture/bin/Debug/net8.0-windows/GameCompanion.Module.StarRupture.dll"
#r "../src/Core/GameCompanion.Core/bin/Debug/net8.0/GameCompanion.Core.dll"

using GameCompanion.Module.StarRupture.Services;
using GameCompanion.Module.StarRupture.Progression;

var discovery = new SaveDiscoveryService();
var parser = new SaveParserService();
var analyzer = new ProgressionAnalyzerService(parser);

Console.WriteLine("=== StarRupture Save Parser Test ===\n");

// Discover saves
var sessionsResult = await discovery.DiscoverSessionsAsync();
if (sessionsResult.IsFailure)
{
    Console.WriteLine($"Error: {sessionsResult.Error}");
    return;
}

var sessions = sessionsResult.Value!;
Console.WriteLine($"Found {sessions.Count} session(s):\n");

foreach (var session in sessions)
{
    Console.WriteLine($"Session: {session.SessionName}");
    Console.WriteLine($"  Location: {session.Location}");
    Console.WriteLine($"  Slots: {session.Slots.Count}");
    Console.WriteLine($"  Last Modified: {session.LastModified}");
    Console.WriteLine($"  Total Size: {session.TotalSizeBytes / 1024:N0} KB");
    
    // Parse the newest save
    var newestSlot = session.Slots.FirstOrDefault();
    if (newestSlot != null)
    {
        Console.WriteLine($"\n  Parsing: {newestSlot.SlotName}...");
        var progressResult = await analyzer.AnalyzeAsync(newestSlot.SaveFilePath);
        if (progressResult.IsSuccess)
        {
            var p = progressResult.Value!;
            Console.WriteLine($"\n  === PLAYER PROGRESS ===");
            Console.WriteLine($"  Playtime: {p.TotalPlayTime.TotalHours:F1} hours");
            Console.WriteLine($"  Current Phase: {p.CurrentPhase}");
            Console.WriteLine($"  Overall Progress: {p.OverallProgress:P1}");
            Console.WriteLine($"  Blueprints: {p.BlueprintsUnlocked}/{p.BlueprintsTotal} ({p.BlueprintProgress:P0})");
            Console.WriteLine($"  Data Points: {p.DataPointsEarned:N0}");
            Console.WriteLine($"  Map Unlocked: {p.MapUnlocked}");
            Console.WriteLine($"  Current Wave: {p.CurrentWave} ({p.CurrentWaveStage})");
            Console.WriteLine($"  Items Discovered: {p.UniqueItemsDiscovered}");
            Console.WriteLine($"\n  === BADGES EARNED ({p.EarnedBadges.Count}) ===");
            foreach (var badge in p.EarnedBadges)
            {
                Console.WriteLine($"    {badge.Icon} {badge.Name} ({badge.Rarity})");
            }
        }
        else
        {
            Console.WriteLine($"  Error: {progressResult.Error}");
        }
    }
    Console.WriteLine();
}
