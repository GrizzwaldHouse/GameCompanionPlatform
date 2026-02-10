using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Progression;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class BadgesTests
{
    [Fact]
    public void AllBadges_ShouldHaveUniqueIds()
    {
        var ids = Badges.AllBadges.Select(b => b.Id).ToList();

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllBadges_ShouldHaveNonEmptyNames()
    {
        foreach (var badge in Badges.AllBadges)
        {
            badge.Name.Should().NotBeNullOrWhiteSpace($"Badge '{badge.Id}' should have a name");
            badge.Description.Should().NotBeNullOrWhiteSpace($"Badge '{badge.Id}' should have a description");
            badge.Icon.Should().NotBeNullOrWhiteSpace($"Badge '{badge.Id}' should have an icon");
        }
    }

    [Fact]
    public void SurvivalBadge_ShouldEarnAfter1Hour()
    {
        var save = CreateSaveWithPlaytime(TimeSpan.FromHours(1.5));

        var survivorBadge = Badges.AllBadges.FirstOrDefault(b => b.Id == "survivor_1h");
        survivorBadge.Should().NotBeNull("survivor_1h badge should exist");

        var earned = survivorBadge!.CheckCondition(save);
        earned.Should().BeTrue();
    }

    [Fact]
    public void SurvivalBadge_ShouldNotEarnBelow1Hour()
    {
        var save = CreateSaveWithPlaytime(TimeSpan.FromMinutes(30));

        var survivorBadge = Badges.AllBadges.FirstOrDefault(b => b.Id == "survivor_1h");
        survivorBadge.Should().NotBeNull();

        var earned = survivorBadge!.CheckCondition(save);
        earned.Should().BeFalse();
    }

    [Fact]
    public void CompletionistBadge_ShouldEarnWhenAllRecipesUnlocked()
    {
        var save = CreateSaveWithRecipes(lockedCount: 0);

        var badge = Badges.AllBadges.FirstOrDefault(b => b.Id == "blueprint_all");
        badge.Should().NotBeNull("blueprint_all badge should exist");

        var earned = badge!.CheckCondition(save);
        earned.Should().BeTrue();
    }

    [Fact]
    public void CompletionistBadge_ShouldNotEarnWithLockedRecipes()
    {
        var save = CreateSaveWithRecipes(lockedCount: 10);

        var badge = Badges.AllBadges.FirstOrDefault(b => b.Id == "blueprint_all");
        badge.Should().NotBeNull();

        var earned = badge!.CheckCondition(save);
        earned.Should().BeFalse();
    }

    [Fact]
    public void AllBadges_ShouldHaveValidRarity()
    {
        foreach (var badge in Badges.AllBadges)
        {
            badge.Rarity.Should().BeDefined($"Badge '{badge.Id}' should have a valid rarity");
        }
    }

    // --- Helpers ---

    private static StarRuptureSave CreateSaveWithPlaytime(TimeSpan playtime)
    {
        return new StarRuptureSave
        {
            FilePath = "test.sav",
            SessionName = "Test",
            SaveTimestamp = DateTime.UtcNow,
            PlayTime = playtime,
            GameState = new StarRuptureGameState
            {
                TutorialCompleted = true,
                PlaytimeDuration = playtime.TotalSeconds
            },
            Corporations = new CorporationsData
            {
                DataPoints = 0,
                UnlockedInventorySlots = 5,
                Corporations = []
            },
            Crafting = new CraftingData
            {
                LockedRecipes = Enumerable.Range(0, 150).Select(i => $"Recipe_{i}").ToList(),
                PickedUpItems = [],
                TotalRecipeCount = 180
            },
            EnviroWave = new EnviroWaveData { Wave = "", Stage = "", Progress = 0 },
            Spatial = null
        };
    }

    private static StarRuptureSave CreateSaveWithRecipes(int lockedCount)
    {
        return new StarRuptureSave
        {
            FilePath = "test.sav",
            SessionName = "Test",
            SaveTimestamp = DateTime.UtcNow,
            PlayTime = TimeSpan.FromHours(50),
            GameState = new StarRuptureGameState
            {
                TutorialCompleted = true,
                PlaytimeDuration = TimeSpan.FromHours(50).TotalSeconds
            },
            Corporations = new CorporationsData
            {
                DataPoints = 50000,
                UnlockedInventorySlots = 20,
                Corporations = []
            },
            Crafting = new CraftingData
            {
                LockedRecipes = Enumerable.Range(0, lockedCount).Select(i => $"Recipe_{i}").ToList(),
                PickedUpItems = Enumerable.Range(0, 180 - lockedCount).Select(i => $"Item_{i}").ToList(),
                TotalRecipeCount = 180
            },
            EnviroWave = new EnviroWaveData { Wave = "Wave 5", Stage = "Stage 3", Progress = 0.8 },
            Spatial = null
        };
    }
}
