using FluentAssertions;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;
using Xunit;

namespace GameCompanion.Module.StarRupture.Tests;

public sealed class ProgressionAnalyzerServiceTests
{
    private readonly ProgressionAnalyzerService _analyzer;

    public ProgressionAnalyzerServiceTests()
    {
        _analyzer = new ProgressionAnalyzerService(new SaveParserService());
    }

    [Fact]
    public void AnalyzeSave_EarlyGame_ShouldDetectEarlyPhase()
    {
        var save = CreateSave(
            playTimeHours: 2,
            lockedRecipes: 170,
            totalRecipes: 180,
            dataPoints: 500,
            corporations: [("Moon Energy", 1, 100)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.CurrentPhase.Should().Be(ProgressionPhase.EarlyGame);
        progress.BlueprintsUnlocked.Should().Be(10);
        progress.OverallProgress.Should().BeLessThan(0.3);
    }

    [Fact]
    public void AnalyzeSave_MidGame_ShouldDetectMidPhase()
    {
        var save = CreateSave(
            playTimeHours: 12,
            lockedRecipes: 120,
            totalRecipes: 180,
            dataPoints: 8000,
            corporations: [("Moon Energy", 3, 5000), ("Quantum Dynamics", 2, 3000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.CurrentPhase.Should().Be(ProgressionPhase.MidGame);
        progress.BlueprintsUnlocked.Should().Be(60);
    }

    [Fact]
    public void AnalyzeSave_EndGame_ShouldDetectEndPhase()
    {
        var save = CreateSave(
            playTimeHours: 30,
            lockedRecipes: 50,
            totalRecipes: 180,
            dataPoints: 25000,
            corporations: [("Moon Energy", 5, 20000), ("Quantum Dynamics", 4, 15000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.CurrentPhase.Should().Be(ProgressionPhase.EndGame);
        progress.BlueprintsUnlocked.Should().Be(130);
    }

    [Fact]
    public void AnalyzeSave_Mastery_ShouldDetectMasteryPhase()
    {
        var save = CreateSave(
            playTimeHours: 80,
            lockedRecipes: 0,
            totalRecipes: 180,
            dataPoints: 50000,
            corporations: [("Moon Energy", 5, 50000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.CurrentPhase.Should().Be(ProgressionPhase.Mastery);
        progress.BlueprintsUnlocked.Should().Be(180);
        progress.OverallProgress.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void AnalyzeSave_MapUnlock_ShouldDetectMoonEnergyLevel3()
    {
        var save = CreateSave(
            playTimeHours: 10,
            lockedRecipes: 130,
            totalRecipes: 180,
            dataPoints: 5000,
            corporations: [("Moon Energy", 3, 5000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.MapUnlocked.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeSave_MapLocked_WhenMoonEnergyBelowLevel3()
    {
        var save = CreateSave(
            playTimeHours: 3,
            lockedRecipes: 160,
            totalRecipes: 180,
            dataPoints: 1000,
            corporations: [("Moon Energy", 2, 2000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.MapUnlocked.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeSave_ShouldCalculateBlueprintProgress()
    {
        var save = CreateSave(
            playTimeHours: 5,
            lockedRecipes: 90,
            totalRecipes: 180,
            dataPoints: 3000,
            corporations: []);

        var progress = _analyzer.AnalyzeSave(save);

        progress.BlueprintProgress.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public void AnalyzeSave_ShouldIdentifyHighestCorporation()
    {
        var save = CreateSave(
            playTimeHours: 15,
            lockedRecipes: 100,
            totalRecipes: 180,
            dataPoints: 10000,
            corporations: [
                ("Moon Energy", 4, 12000),
                ("Quantum Dynamics", 2, 3000),
                ("BioStar", 3, 7000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.HighestCorporationLevel.Should().Be(4);
        progress.HighestCorporationName.Should().Contain("Moon");
    }

    [Fact]
    public void AnalyzeSave_WithWaveData_ShouldReportWaveInfo()
    {
        var save = CreateSave(
            playTimeHours: 8,
            lockedRecipes: 140,
            totalRecipes: 180,
            dataPoints: 4000,
            corporations: [],
            wave: "Wave 3",
            waveStage: "Stage 2");

        var progress = _analyzer.AnalyzeSave(save);

        progress.CurrentWave.Should().Be("Wave 3");
        progress.CurrentWaveStage.Should().Be("Stage 2");
    }

    [Fact]
    public void AnalyzeSave_OverallProgress_ShouldBeNormalized()
    {
        var save = CreateSave(
            playTimeHours: 50,
            lockedRecipes: 0,
            totalRecipes: 180,
            dataPoints: 100000,
            corporations: [("Moon Energy", 5, 50000)]);

        var progress = _analyzer.AnalyzeSave(save);

        progress.OverallProgress.Should().BeInRange(0.0, 1.0);
    }

    // --- Helper ---

    private static readonly Dictionary<string, string> CorporationNameMap = new()
    {
        ["Moon Energy"] = "MoonCorporation",
        ["Quantum Dynamics"] = "QuantumDynamics",
        ["BioStar"] = "BioStar"
    };

    private static StarRuptureSave CreateSave(
        double playTimeHours,
        int lockedRecipes,
        int totalRecipes,
        int dataPoints,
        (string Name, int Level, int Xp)[] corporations,
        string wave = "",
        string waveStage = "")
    {
        var unlockedRecipes = totalRecipes - lockedRecipes;

        var corpInfos = corporations.Select(c => new CorporationInfo
        {
            Name = CorporationNameMap.GetValueOrDefault(c.Name, c.Name.Replace(" ", "")),
            DisplayName = c.Name,
            CurrentLevel = c.Level,
            CurrentXP = c.Xp
        }).ToList();

        var lockedRecipeList = new List<string>();
        for (var i = 0; i < lockedRecipes; i++)
            lockedRecipeList.Add($"Recipe_{i}");

        var pickedItems = new List<string>();
        for (var i = 0; i < unlockedRecipes; i++)
            pickedItems.Add($"Item_{i}");

        return new StarRuptureSave
        {
            FilePath = "test.sav",
            SessionName = "TestSession",
            SaveTimestamp = DateTime.UtcNow,
            PlayTime = TimeSpan.FromHours(playTimeHours),
            GameState = new StarRuptureGameState
            {
                TutorialCompleted = playTimeHours > 1,
                PlaytimeDuration = TimeSpan.FromHours(playTimeHours).TotalSeconds
            },
            Corporations = new CorporationsData
            {
                DataPoints = dataPoints,
                UnlockedInventorySlots = 10,
                Corporations = corpInfos
            },
            Crafting = new CraftingData
            {
                LockedRecipes = lockedRecipeList,
                PickedUpItems = pickedItems,
                UnlockedRecipeCount = unlockedRecipes,
                TotalRecipeCount = totalRecipes
            },
            EnviroWave = new EnviroWaveData
            {
                Wave = wave,
                Stage = waveStage,
                Progress = 0.5
            },
            Spatial = null
        };
    }
}
