namespace GameCompanion.Module.StarRupture.Progression;

using GameCompanion.Core.Interfaces;
using GameCompanion.Core.Models;

/// <summary>
/// Defines the progression structure for StarRupture.
/// </summary>
public sealed class StarRuptureProgressionMap : IProgressionMap
{
    public IReadOnlyList<Phase> Phases { get; } = CreatePhases();

    private static IReadOnlyList<Phase> CreatePhases()
    {
        return new List<Phase>
        {
            new Phase(
                Id: "early_game",
                Name: "Early Game",
                Description: "Establish your first base and survive the initial threats",
                Order: 1,
                Steps: CreateEarlyGameSteps()),

            new Phase(
                Id: "mid_game",
                Name: "Mid Game",
                Description: "Automate production and hunt for blueprints",
                Order: 2,
                Steps: CreateMidGameSteps()),

            new Phase(
                Id: "end_game",
                Name: "End Game",
                Description: "Expand your factory empire and master all technologies",
                Order: 3,
                Steps: CreateEndGameSteps()),

            new Phase(
                Id: "mastery",
                Name: "Mastery",
                Description: "Complete all objectives and unlock everything",
                Order: 4,
                Steps: CreateMasterySteps())
        };
    }

    private static IReadOnlyList<Step> CreateEarlyGameSteps()
    {
        return new List<Step>
        {
            new Step(
                Id: "build_first_base",
                Title: "Build Your First Base",
                WhyItMatters: "A base provides shelter from environmental hazards and a place to store resources.",
                Actions: new List<StepAction>
                {
                    new(1, "Gather basic resources (stone, wood)", "Look for resource nodes near spawn"),
                    new(2, "Craft a workbench"),
                    new(3, "Build walls and a roof"),
                    new(4, "Place storage containers")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("workbench", "Workbench built"),
                    new("walls", "Walls constructed"),
                    new("storage", "Storage placed")
                },
                Prerequisites: new List<string>()),

            new Step(
                Id: "power_setup",
                Title: "Set Up Power",
                WhyItMatters: "Power is essential for running machines and advanced crafting.",
                Actions: new List<StepAction>
                {
                    new(1, "Craft a generator"),
                    new(2, "Connect power cables"),
                    new(3, "Power your workbench")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("generator", "Generator built"),
                    new("cables", "Cables connected"),
                    new("powered_bench", "Workbench powered")
                },
                Prerequisites: new List<string> { "build_first_base" }),

            new Step(
                Id: "resource_extraction",
                Title: "Automate Resource Extraction",
                WhyItMatters: "Extractors automatically gather resources so you can focus on exploration.",
                Actions: new List<StepAction>
                {
                    new(1, "Find ore deposits"),
                    new(2, "Build extractors on deposits"),
                    new(3, "Connect extractors to storage")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("find_ore", "Ore deposits found"),
                    new("extractor", "Extractor built"),
                    new("connected", "Connected to storage")
                },
                Prerequisites: new List<string> { "power_setup" })
        };
    }

    private static IReadOnlyList<Step> CreateMidGameSteps()
    {
        return new List<Step>
        {
            new Step(
                Id: "moon_energy_rep",
                Title: "Build Moon Energy Reputation",
                WhyItMatters: "Moon Energy Level 3 unlocks the planetary map - essential for finding blueprints.",
                Actions: new List<StepAction>
                {
                    new(1, "Set up Calcium Ore extraction"),
                    new(2, "Ship Calcium Ore to Moon Energy"),
                    new(3, "Alternatively: Invest Data Points")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("calcium_extraction", "Calcium Ore extraction set up"),
                    new("shipping", "Shipping to Moon Energy"),
                    new("level_3", "Reached Level 3", true)
                },
                Prerequisites: new List<string> { "resource_extraction" }),

            new Step(
                Id: "blueprint_hunting",
                Title: "Hunt for Blueprints",
                WhyItMatters: "Blueprints unlock advanced recipes. Find them in blue chests at abandoned outposts.",
                Actions: new List<StepAction>
                {
                    new(1, "Use the map to locate POIs"),
                    new(2, "Explore abandoned outposts"),
                    new(3, "Loot blue storage chests (blueprints)"),
                    new(4, "Avoid red chests (supplies only)")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("rotor_tube", "Rotor and Tube (Purple Haze)"),
                    new("stator", "Stator (Lemon Souls)"),
                    new("stabilizer", "Stabilizer (Grey Owl)"),
                    new("synthetic_silicon", "Synthetic Silicon (Mythic Cry)"),
                    new("electronics", "Electronics (Starry Night/Copperfield)")
                },
                Prerequisites: new List<string> { "moon_energy_rep" }),

            new Step(
                Id: "factory_automation",
                Title: "Automate Your Factory",
                WhyItMatters: "Automation frees you to explore while resources are processed.",
                Actions: new List<StepAction>
                {
                    new(1, "Build conveyor belts"),
                    new(2, "Set up processing machines"),
                    new(3, "Create production chains"),
                    new(4, "Balance input/output rates")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("conveyors", "Conveyor system built"),
                    new("smelting", "Automated smelting"),
                    new("crafting", "Automated crafting"),
                    new("balanced", "Production balanced")
                },
                Prerequisites: new List<string> { "blueprint_hunting" })
        };
    }

    private static IReadOnlyList<Step> CreateEndGameSteps()
    {
        return new List<Step>
        {
            new Step(
                Id: "advanced_blueprints",
                Title: "Collect Advanced Blueprints",
                WhyItMatters: "Advanced blueprints enable end-game production.",
                Actions: new List<StepAction>
                {
                    new(1, "Explore dangerous zones"),
                    new(2, "Find Chemicals (Spored Rock)"),
                    new(3, "Find Hardening Agent (Next Step)"),
                    new(4, "Find Valve, Turbine, Electromagnetic Coil (Redleaf)")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("chemicals", "Chemicals blueprint"),
                    new("hardening_agent", "Hardening Agent blueprint"),
                    new("valve", "Valve blueprint"),
                    new("turbine", "Turbine blueprint"),
                    new("em_coil", "Electromagnetic Coil blueprint")
                },
                Prerequisites: new List<string> { "factory_automation" }),

            new Step(
                Id: "base_defense",
                Title: "Fortify Base Defense",
                WhyItMatters: "Environmental waves bring increasingly dangerous threats.",
                Actions: new List<StepAction>
                {
                    new(1, "Build defensive walls"),
                    new(2, "Set up turrets"),
                    new(3, "Create safe zones"),
                    new(4, "Prepare for wave attacks")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("walls", "Defensive walls"),
                    new("turrets", "Turrets deployed"),
                    new("safe_zones", "Safe zones established")
                },
                Prerequisites: new List<string> { "advanced_blueprints" }),

            new Step(
                Id: "expand_operations",
                Title: "Expand Operations",
                WhyItMatters: "Multiple outposts allow access to diverse resources.",
                Actions: new List<StepAction>
                {
                    new(1, "Establish outposts in new biomes"),
                    new(2, "Set up logistics networks"),
                    new(3, "Connect remote extractors")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("outpost_1", "First outpost established"),
                    new("outpost_2", "Second outpost established"),
                    new("logistics", "Logistics network connected")
                },
                Prerequisites: new List<string> { "base_defense" })
        };
    }

    private static IReadOnlyList<Step> CreateMasterySteps()
    {
        return new List<Step>
        {
            new Step(
                Id: "all_blueprints",
                Title: "Unlock All Blueprints",
                WhyItMatters: "Complete mastery of all technologies.",
                Actions: new List<StepAction>
                {
                    new(1, "Track missing blueprints"),
                    new(2, "Locate remaining POIs"),
                    new(3, "Collect final blueprints")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("all_unlocked", "All blueprints unlocked", true)
                },
                Prerequisites: new List<string> { "expand_operations" }),

            new Step(
                Id: "max_corporations",
                Title: "Max All Corporations",
                WhyItMatters: "Unlock all corporation benefits and features.",
                Actions: new List<StepAction>
                {
                    new(1, "Ship resources to each corporation"),
                    new(2, "Invest data points strategically"),
                    new(3, "Reach max level with all corporations")
                },
                Checklist: new List<StepChecklistItem>
                {
                    new("moon_max", "Moon Energy maxed"),
                    new("future_max", "Future Tech maxed"),
                    new("selenian_max", "Selenian maxed"),
                    new("griffiths_max", "Griffiths maxed"),
                    new("clever_max", "Clever Industries maxed")
                },
                Prerequisites: new List<string> { "all_blueprints" })
        };
    }

    public Phase GetCurrentPhase(IProgressionState state)
    {
        // Find the first phase with incomplete steps
        foreach (var phase in Phases)
        {
            var incompleteSteps = phase.Steps.Count(s => !state.IsStepCompleted(s.Id));
            if (incompleteSteps > 0)
                return phase;
        }

        // All complete, return mastery
        return Phases[^1];
    }

    public IReadOnlyList<Step> GetAvailableSteps(IProgressionState state)
    {
        var available = new List<Step>();

        foreach (var phase in Phases)
        {
            foreach (var step in phase.Steps)
            {
                if (state.IsStepCompleted(step.Id))
                    continue;

                // Check prerequisites
                var prereqsMet = step.Prerequisites.All(p => state.IsStepCompleted(p));
                if (prereqsMet)
                    available.Add(step);
            }
        }

        return available;
    }

    public Step? GetNextRecommendedStep(IProgressionState state)
    {
        var available = GetAvailableSteps(state);
        return available.FirstOrDefault();
    }

    public double GetProgressPercentage(IProgressionState state)
    {
        var totalSteps = Phases.Sum(p => p.Steps.Count);
        if (totalSteps == 0) return 0;

        var completedSteps = state.CompletedStepIds.Count;
        return (double)completedSteps / totalSteps;
    }
}
