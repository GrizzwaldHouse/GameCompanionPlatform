namespace GameCompanion.Module.StarRupture.Progression;

using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Defines all achievement badges for StarRupture.
/// </summary>
public static class Badges
{
    public static IReadOnlyList<BadgeDefinition> AllBadges { get; } = new List<BadgeDefinition>
    {
        // Early Game Badges
        new BadgeDefinition
        {
            Id = "first_steps",
            Name = "First Steps",
            Description = "Complete the tutorial",
            Icon = "🚀",
            Rarity = BadgeRarity.Common,
            CheckCondition = save => save.GameState.TutorialCompleted
        },
        new BadgeDefinition
        {
            Id = "survivor_1h",
            Name = "Survivor",
            Description = "Play for 1 hour",
            Icon = "⏱️",
            Rarity = BadgeRarity.Common,
            CheckCondition = save => save.PlayTime.TotalHours >= 1
        },
        new BadgeDefinition
        {
            Id = "survivor_10h",
            Name = "Seasoned Survivor",
            Description = "Play for 10 hours",
            Icon = "⏱️",
            Rarity = BadgeRarity.Uncommon,
            CheckCondition = save => save.PlayTime.TotalHours >= 10
        },
        new BadgeDefinition
        {
            Id = "survivor_25h",
            Name = "Veteran Survivor",
            Description = "Play for 25 hours",
            Icon = "⏱️",
            Rarity = BadgeRarity.Rare,
            CheckCondition = save => save.PlayTime.TotalHours >= 25
        },

        // Blueprint Badges
        new BadgeDefinition
        {
            Id = "blueprint_10",
            Name = "Blueprint Collector",
            Description = "Unlock 10 blueprints",
            Icon = "📘",
            Rarity = BadgeRarity.Common,
            CheckCondition = save => save.Crafting.UnlockedRecipeCount >= 10
        },
        new BadgeDefinition
        {
            Id = "blueprint_50",
            Name = "Blueprint Hunter",
            Description = "Unlock 50 blueprints",
            Icon = "📘",
            Rarity = BadgeRarity.Uncommon,
            CheckCondition = save => save.Crafting.UnlockedRecipeCount >= 50
        },
        new BadgeDefinition
        {
            Id = "blueprint_100",
            Name = "Blueprint Master",
            Description = "Unlock 100 blueprints",
            Icon = "📘",
            Rarity = BadgeRarity.Rare,
            CheckCondition = save => save.Crafting.UnlockedRecipeCount >= 100
        },
        new BadgeDefinition
        {
            Id = "blueprint_all",
            Name = "Completionist",
            Description = "Unlock all blueprints",
            Icon = "🏆",
            Rarity = BadgeRarity.Legendary,
            CheckCondition = save => save.Crafting.LockedRecipes.Count == 0
        },

        // Corporation Badges
        new BadgeDefinition
        {
            Id = "data_points_1000",
            Name = "Data Miner",
            Description = "Earn 1,000 Data Points",
            Icon = "💾",
            Rarity = BadgeRarity.Common,
            CheckCondition = save => save.Corporations.DataPoints >= 1000
        },
        new BadgeDefinition
        {
            Id = "data_points_10000",
            Name = "Data Hoarder",
            Description = "Earn 10,000 Data Points",
            Icon = "💾",
            Rarity = BadgeRarity.Uncommon,
            CheckCondition = save => save.Corporations.DataPoints >= 10000
        },
        new BadgeDefinition
        {
            Id = "data_points_25000",
            Name = "Data Baron",
            Description = "Earn 25,000 Data Points",
            Icon = "💾",
            Rarity = BadgeRarity.Rare,
            CheckCondition = save => save.Corporations.DataPoints >= 25000
        },
        new BadgeDefinition
        {
            Id = "map_unlocked",
            Name = "Cartographer",
            Description = "Unlock the planetary map (Moon Energy Level 3)",
            Icon = "🗺️",
            Rarity = BadgeRarity.Uncommon,
            CheckCondition = save => save.Corporations.Corporations
                .Any(c => c.Name == "MoonCorporation" && c.CurrentLevel >= 3)
        },
        new BadgeDefinition
        {
            Id = "corp_ally",
            Name = "Corporate Ally",
            Description = "Reach Level 3 with any corporation",
            Icon = "🤝",
            Rarity = BadgeRarity.Uncommon,
            CheckCondition = save => save.Corporations.Corporations.Any(c => c.CurrentLevel >= 3)
        },

        // Discovery Badges
        new BadgeDefinition
        {
            Id = "botanist",
            Name = "Botanist",
            Description = "Discover 5 unique plant species",
            Icon = "🌿",
            Rarity = BadgeRarity.Common,
            CheckCondition = save => save.Crafting.PickedUpItems.Count >= 5
        },

        // Wave Progression
        new BadgeDefinition
        {
            Id = "heat_wave",
            Name = "Heat Resistant",
            Description = "Survive the Heat Wave",
            Icon = "🔥",
            Rarity = BadgeRarity.Uncommon,
            CheckCondition = save => save.EnviroWave.Wave != "Heat" || save.EnviroWave.Stage == "PostWave"
        },

        // Inventory
        new BadgeDefinition
        {
            Id = "inventory_expanded",
            Name = "Pack Rat",
            Description = "Unlock additional inventory slots",
            Icon = "🎒",
            Rarity = BadgeRarity.Common,
            CheckCondition = save => save.Corporations.UnlockedInventorySlots > 0
        }
    };
}

/// <summary>
/// Defines a badge and its unlock condition.
/// </summary>
public sealed class BadgeDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required BadgeRarity Rarity { get; init; }
    public required Func<StarRuptureSave, bool> CheckCondition { get; init; }

    /// <summary>
    /// Converts to a Badge model if earned.
    /// </summary>
    public Badge? ToBadgeIfEarned(StarRuptureSave save)
    {
        if (!CheckCondition(save))
            return null;

        return new Badge
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Icon = Icon,
            Rarity = Rarity,
            EarnedAt = save.SaveTimestamp
        };
    }
}
