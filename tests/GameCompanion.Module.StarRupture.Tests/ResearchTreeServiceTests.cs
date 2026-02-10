using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class ResearchTreeServiceTests
{
    private readonly ResearchTreeService _service;

    public ResearchTreeServiceTests()
    {
        var wikiService = new WikiDataService();
        _service = new ResearchTreeService(wikiService);
    }

    [Fact]
    public async Task BuildTree_WithEmptyCraftingData_ShouldReturnEmptyTree()
    {
        var craftingData = new CraftingData
        {
            LockedRecipes = [],
            PickedUpItems = [],
            UnlockedRecipeCount = 0,
            TotalRecipeCount = 0
        };

        var result = await _service.BuildTreeAsync(craftingData);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Categories.Should().BeEmpty();
        result.Value.TotalRecipes.Should().Be(0);
        result.Value.UnlockedRecipes.Should().Be(0);
    }

    [Fact]
    public async Task BuildTree_ShouldMarkLockedRecipesCorrectly()
    {
        var craftingData = new CraftingData
        {
            LockedRecipes = [
                "/Game/Items/Production/AdvancedMachine",
                "/Game/Items/Resources/RareOre"
            ],
            PickedUpItems = [
                "/Game/Items/Production/BasicMachine",
                "/Game/Items/Resources/IronOre"
            ],
            UnlockedRecipeCount = 2,
            TotalRecipeCount = 4
        };

        var result = await _service.BuildTreeAsync(craftingData);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalRecipes.Should().Be(4);
        result.Value.UnlockedRecipes.Should().Be(2);

        var allNodes = result.Value.Categories.SelectMany(c => c.Nodes).ToList();
        allNodes.Should().HaveCount(4);

        var lockedNodes = allNodes.Where(n => n.Status == ResearchNodeStatus.Locked).ToList();
        lockedNodes.Should().HaveCount(2);
        lockedNodes.Should().Contain(n => n.Name.Contains("Advanced Machine"));
        lockedNodes.Should().Contain(n => n.Name.Contains("Rare Ore"));

        var unlockedNodes = allNodes.Where(n => n.Status == ResearchNodeStatus.Unlocked).ToList();
        unlockedNodes.Should().HaveCount(2);
        unlockedNodes.Should().Contain(n => n.Name.Contains("Basic Machine"));
        unlockedNodes.Should().Contain(n => n.Name.Contains("Iron Ore"));
    }

    [Fact]
    public async Task BuildTree_ShouldGroupByCategory()
    {
        var craftingData = new CraftingData
        {
            LockedRecipes = [
                "/Game/Items/Production/Machine1",
                "/Game/Items/Production/Machine2"
            ],
            PickedUpItems = [
                "/Game/Items/Resources/Ore1",
                "/Game/Items/Resources/Ore2",
                "/Game/Items/Power/Generator"
            ],
            UnlockedRecipeCount = 3,
            TotalRecipeCount = 5
        };

        var result = await _service.BuildTreeAsync(craftingData);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Categories.Should().HaveCountGreaterThan(0);

        var productionCategory = result.Value.Categories.FirstOrDefault(c => c.Name == "Production");
        productionCategory.Should().NotBeNull();
        productionCategory!.Nodes.Should().HaveCount(2);
        productionCategory.UnlockedCount.Should().Be(0); // Both locked

        var resourcesCategory = result.Value.Categories.FirstOrDefault(c => c.Name == "Resources");
        resourcesCategory.Should().NotBeNull();
        resourcesCategory!.Nodes.Should().HaveCount(2);
        resourcesCategory.UnlockedCount.Should().Be(2); // Both unlocked

        var powerCategory = result.Value.Categories.FirstOrDefault(c => c.Name == "Power");
        powerCategory.Should().NotBeNull();
        powerCategory!.Nodes.Should().HaveCount(1);
        powerCategory.UnlockedCount.Should().Be(1); // Unlocked
    }
}
