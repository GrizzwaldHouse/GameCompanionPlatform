namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Provides expansion recommendations and analysis.
/// </summary>
public sealed class ExpansionAdvisorService
{
    private const double ExpansionRadius = 5000; // Search radius for expansion sites
    private const double MinDistanceBetweenBases = 3000;

    /// <summary>
    /// Generates expansion recommendations.
    /// </summary>
    public Result<ExpansionPlan> AnalyzeExpansion(StarRuptureSave save)
    {
        try
        {
            if (save.Spatial == null)
            {
                return Result<ExpansionPlan>.Success(new ExpansionPlan
                {
                    RecommendedSites = [],
                    NearbyResources = [],
                    Readiness = new ExpansionReadiness
                    {
                        HasRequiredResearch = false,
                        HasSufficientResources = false,
                        HasLogisticsCapacity = false,
                        HasPowerCapacity = false,
                        MissingRequirements = ["No spatial data available"]
                    },
                    Warnings = [],
                    CurrentBaseStats = new BaseStatistics
                    {
                        TotalBuildings = 0,
                        BaseArea = 0,
                        ResourceTypes = 0,
                        AverageEfficiency = 0
                    }
                });
            }

            var buildings = save.Spatial.Entities.Where(e => e.IsBuilding).ToList();
            var baseStats = CalculateBaseStatistics(buildings);
            var baseCenter = CalculateBaseCenter(buildings);

            var recommendedSites = FindExpansionSites(save.Spatial, baseCenter);
            var nearbyResources = FindNearbyResources(save.Spatial, baseCenter);
            var readiness = EvaluateReadiness(save);
            var warnings = GenerateWarnings(save, baseStats);

            return Result<ExpansionPlan>.Success(new ExpansionPlan
            {
                RecommendedSites = recommendedSites,
                NearbyResources = nearbyResources,
                Readiness = readiness,
                Warnings = warnings,
                CurrentBaseStats = baseStats
            });
        }
        catch (Exception ex)
        {
            return Result<ExpansionPlan>.Failure($"Failed to analyze expansion: {ex.Message}");
        }
    }

    private static BaseStatistics CalculateBaseStatistics(List<PlacedEntity> buildings)
    {
        if (buildings.Count == 0)
        {
            return new BaseStatistics
            {
                TotalBuildings = 0,
                BaseArea = 0,
                ResourceTypes = 0,
                AverageEfficiency = 0
            };
        }

        var minX = buildings.Min(b => b.Position.X);
        var maxX = buildings.Max(b => b.Position.X);
        var minY = buildings.Min(b => b.Position.Y);
        var maxY = buildings.Max(b => b.Position.Y);

        var area = (maxX - minX) * (maxY - minY) / 1_000_000; // Convert to km²

        var operationalCount = buildings.Count(b => !b.IsDisabled && !b.HasMalfunction);
        var efficiency = buildings.Count > 0 ? (double)operationalCount / buildings.Count * 100 : 0;

        return new BaseStatistics
        {
            TotalBuildings = buildings.Count,
            BaseArea = area,
            ResourceTypes = buildings.Select(b => ExtractBuildingType(b.EntityType)).Distinct().Count(),
            AverageEfficiency = efficiency
        };
    }

    private static WorldPosition CalculateBaseCenter(List<PlacedEntity> buildings)
    {
        if (buildings.Count == 0)
            return new WorldPosition { X = 0, Y = 0, Z = 0 };

        return new WorldPosition
        {
            X = buildings.Average(b => b.Position.X),
            Y = buildings.Average(b => b.Position.Y),
            Z = buildings.Average(b => b.Position.Z)
        };
    }

    private static List<ExpansionSite> FindExpansionSites(SpatialData spatial, WorldPosition baseCenter)
    {
        var sites = new List<ExpansionSite>();

        // Generate candidate positions in cardinal directions
        var directions = new[]
        {
            (1.0, 0.0, "East"),
            (-1.0, 0.0, "West"),
            (0.0, 1.0, "North"),
            (0.0, -1.0, "South"),
            (0.707, 0.707, "Northeast"),
            (-0.707, 0.707, "Northwest"),
            (0.707, -0.707, "Southeast"),
            (-0.707, -0.707, "Southwest")
        };

        foreach (var (dx, dy, direction) in directions)
        {
            var candidatePos = new WorldPosition
            {
                X = baseCenter.X + dx * ExpansionRadius,
                Y = baseCenter.Y + dy * ExpansionRadius,
                Z = baseCenter.Z
            };

            // Check if position is clear of existing buildings
            var nearbyBuildings = spatial.Entities
                .Where(e => e.IsBuilding)
                .Count(e => Distance(e.Position, candidatePos) < MinDistanceBetweenBases);

            if (nearbyBuildings > 0)
                continue;

            // Score the site
            var score = CalculateSiteScore(spatial, candidatePos, baseCenter);

            sites.Add(new ExpansionSite
            {
                Position = candidatePos,
                Score = score,
                Reason = $"Clear area to the {direction}",
                NearbyResources = FindResourcesNear(spatial, candidatePos),
                DistanceFromMainBase = Distance(baseCenter, candidatePos),
                RecommendedPurpose = DeterminePurpose(spatial, candidatePos),
                Advantages = GetSiteAdvantages(spatial, candidatePos, direction),
                Challenges = GetSiteChallenges(spatial, candidatePos)
            });
        }

        return sites.OrderByDescending(s => s.Score).Take(4).ToList();
    }

    private static double CalculateSiteScore(SpatialData spatial, WorldPosition pos, WorldPosition baseCenter)
    {
        var score = 50.0;

        // Bonus for resources nearby
        var resourceCount = FindResourcesNear(spatial, pos).Count;
        score += resourceCount * 10;

        // Penalty for being too far
        var distance = Distance(pos, baseCenter);
        if (distance > ExpansionRadius * 1.5)
            score -= 20;

        return Math.Max(0, Math.Min(100, score));
    }

    private static List<string> FindResourcesNear(SpatialData spatial, WorldPosition pos)
    {
        // In a real implementation, this would query actual resource deposits
        return ["Iron", "Copper"]; // Placeholder
    }

    private static List<ResourceDeposit> FindNearbyResources(SpatialData spatial, WorldPosition baseCenter)
    {
        // Placeholder - would need actual resource deposit data
        return
        [
            new ResourceDeposit
            {
                ResourceType = "Iron Ore",
                Position = new WorldPosition { X = baseCenter.X + 3000, Y = baseCenter.Y + 1000, Z = 0 },
                IsExploited = false,
                DistanceFromBase = 3162,
                Richness = ResourceRichness.Normal
            }
        ];
    }

    private static ExpansionPurpose DeterminePurpose(SpatialData spatial, WorldPosition pos)
    {
        var nearbyResources = FindResourcesNear(spatial, pos);
        if (nearbyResources.Count > 2)
            return ExpansionPurpose.ResourceExtraction;

        return ExpansionPurpose.Production;
    }

    private static List<string> GetSiteAdvantages(SpatialData spatial, WorldPosition pos, string direction)
    {
        return
        [
            $"Open space to the {direction}",
            "Good terrain for building",
            "Away from enemy spawn points"
        ];
    }

    private static List<string> GetSiteChallenges(SpatialData spatial, WorldPosition pos)
    {
        return
        [
            "Requires logistics connection",
            "Power infrastructure needed"
        ];
    }

    private static ExpansionReadiness EvaluateReadiness(StarRuptureSave save)
    {
        var missing = new List<string>();

        // Check research - compare unlocked vs locked recipe counts
        var hasResearch = save.Crafting.UnlockedRecipeCount > save.Crafting.LockedRecipes.Count;
        if (!hasResearch)
            missing.Add("Unlock more research for expansion buildings");

        // Check logistics - evaluate based on active logistics requests (lower is better capacity)
        var activeRequests = save.Spatial?.Logistics.Requests.Count(r => !r.IsAborted) ?? 0;
        var hasLogistics = activeRequests < 100; // Not overloaded
        if (!hasLogistics)
            missing.Add("Logistics network is overloaded - add more capacity");

        // Check power (simplified)
        var hasPower = save.Spatial?.ElectricityNetwork.Nodes.Count > 10;
        if (!hasPower)
            missing.Add("Expand power generation capacity");

        return new ExpansionReadiness
        {
            HasRequiredResearch = hasResearch,
            HasSufficientResources = true, // Would need inventory check
            HasLogisticsCapacity = hasLogistics,
            HasPowerCapacity = hasPower,
            MissingRequirements = missing
        };
    }

    private static List<ExpansionWarning> GenerateWarnings(StarRuptureSave save, BaseStatistics stats)
    {
        var warnings = new List<ExpansionWarning>();

        if (stats.AverageEfficiency < 70)
        {
            warnings.Add(new ExpansionWarning
            {
                Title = "Low Base Efficiency",
                Description = "Current base efficiency is below optimal. Consider optimizing before expanding.",
                Severity = WarningSeverity.Warning,
                Mitigation = "Repair malfunctioning machines and enable disabled buildings"
            });
        }

        if (stats.TotalBuildings < 20)
        {
            warnings.Add(new ExpansionWarning
            {
                Title = "Small Base",
                Description = "Your base is relatively small. Consider growing current base first.",
                Severity = WarningSeverity.Info,
                Mitigation = "Focus on essential production buildings before expanding"
            });
        }

        return warnings;
    }

    private static double Distance(WorldPosition a, WorldPosition b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static string ExtractBuildingType(string entityType)
    {
        var parts = entityType.Split('/');
        return parts.LastOrDefault() ?? entityType;
    }
}
