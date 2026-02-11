namespace GameCompanion.Module.StarRupture.Services;

using GameCompanion.Core.Models;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Provides resource depletion forecasting and sustainability analysis.
/// </summary>
public sealed class DepletionForecastService
{
    private static readonly TimeSpan DefaultForecastHorizon = TimeSpan.FromHours(24);

    // Estimated consumption rates by building type (units per minute)
    private static readonly Dictionary<string, (string Resource, double Rate)> ConsumptionRates = new(StringComparer.OrdinalIgnoreCase)
    {
        { "smelter", ("iron_ore", 30) },
        { "furnace", ("coal", 10) },
        { "assembler", ("iron_plate", 20) },
        { "constructor", ("copper_plate", 15) },
        { "generator", ("coal", 5) }
    };

    /// <summary>
    /// Generates resource depletion forecast.
    /// </summary>
    public Result<DepletionForecast> GenerateForecast(StarRuptureSave save, TimeSpan? horizon = null)
    {
        try
        {
            var forecastHorizon = horizon ?? DefaultForecastHorizon;

            if (save.Spatial == null)
            {
                return Result<DepletionForecast>.Success(new DepletionForecast
                {
                    Forecasts = [],
                    Alerts = [],
                    Sustainability = new SustainabilityScore
                    {
                        OverallScore = 0,
                        SustainableResources = 0,
                        DepletingResources = 0,
                        CriticalResources = 0,
                        Assessment = "No data available",
                        Level = SustainabilityLevel.Critical
                    },
                    Mitigations = [],
                    ForecastHorizon = forecastHorizon
                });
            }

            var forecasts = CalculateResourceForecasts(save, forecastHorizon);
            var alerts = GenerateAlerts(forecasts);
            var sustainability = CalculateSustainability(forecasts);
            var mitigations = GenerateMitigations(forecasts);

            return Result<DepletionForecast>.Success(new DepletionForecast
            {
                Forecasts = forecasts,
                Alerts = alerts,
                Sustainability = sustainability,
                Mitigations = mitigations,
                ForecastHorizon = forecastHorizon
            });
        }
        catch (Exception ex)
        {
            return Result<DepletionForecast>.Failure($"Failed to generate forecast: {ex.Message}");
        }
    }

    private static List<ResourceForecast> CalculateResourceForecasts(StarRuptureSave save, TimeSpan horizon)
    {
        var forecasts = new List<ResourceForecast>();
        var resourceConsumption = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var resourceProduction = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Calculate consumption from buildings
        foreach (var entity in save.Spatial!.Entities.Where(e => e.IsBuilding && !e.IsDisabled))
        {
            foreach (var (buildingType, (resource, rate)) in ConsumptionRates)
            {
                if (entity.EntityType.Contains(buildingType, StringComparison.OrdinalIgnoreCase))
                {
                    if (!resourceConsumption.ContainsKey(resource))
                        resourceConsumption[resource] = 0;
                    resourceConsumption[resource] += rate;
                }
            }
        }

        // Estimate production (simplified - would need recipe tracking)
        foreach (var entity in save.Spatial.Entities.Where(e => e.IsBuilding && !e.IsDisabled))
        {
            var type = entity.EntityType.ToLowerInvariant();
            if (type.Contains("miner"))
            {
                if (!resourceProduction.ContainsKey("iron_ore"))
                    resourceProduction["iron_ore"] = 0;
                resourceProduction["iron_ore"] += 60;
            }
            if (type.Contains("smelter"))
            {
                if (!resourceProduction.ContainsKey("iron_plate"))
                    resourceProduction["iron_plate"] = 0;
                resourceProduction["iron_plate"] += 30;
            }
        }

        // Common resources to track
        var resourceTypes = new[] { "iron_ore", "copper_ore", "coal", "iron_plate", "copper_plate", "steel_plate" };

        foreach (var resource in resourceTypes)
        {
            var consumption = resourceConsumption.GetValueOrDefault(resource, 0);
            var production = resourceProduction.GetValueOrDefault(resource, 0);
            var currentAmount = 1000.0; // Placeholder - would need inventory data

            var netRate = production - consumption;
            var timeToDepletion = netRate >= 0
                ? TimeSpan.MaxValue
                : TimeSpan.FromMinutes(currentAmount / Math.Abs(netRate));

            if (timeToDepletion > horizon)
                timeToDepletion = horizon;

            var status = DetermineStatus(netRate, currentAmount, timeToDepletion);

            forecasts.Add(new ResourceForecast
            {
                ResourceType = FormatResourceName(resource),
                CurrentAmount = currentAmount,
                ConsumptionRate = consumption,
                ProductionRate = production,
                TimeUntilDepletion = timeToDepletion,
                Status = status
            });
        }

        return forecasts.OrderBy(f => f.TimeUntilDepletion).ToList();
    }

    private static DepletionStatus DetermineStatus(double netRate, double current, TimeSpan timeToDepletion)
    {
        if (current <= 0)
            return DepletionStatus.Depleted;
        if (netRate >= 0)
            return DepletionStatus.Sustainable;
        if (timeToDepletion.TotalMinutes < 30)
            return DepletionStatus.Critical;
        if (timeToDepletion.TotalHours < 2)
            return DepletionStatus.Declining;

        return DepletionStatus.Stable;
    }

    private static List<DepletionAlert> GenerateAlerts(List<ResourceForecast> forecasts)
    {
        var alerts = new List<DepletionAlert>();

        foreach (var forecast in forecasts)
        {
            if (forecast.Status == DepletionStatus.Critical)
            {
                alerts.Add(new DepletionAlert
                {
                    ResourceType = forecast.ResourceType,
                    Severity = AlertSeverity.Critical,
                    Message = $"{forecast.ResourceType} will deplete in {forecast.TimeDisplay}!",
                    TimeUntilCritical = forecast.TimeUntilDepletion,
                    ImpactedSystems = GetImpactedSystems(forecast.ResourceType)
                });
            }
            else if (forecast.Status == DepletionStatus.Declining)
            {
                alerts.Add(new DepletionAlert
                {
                    ResourceType = forecast.ResourceType,
                    Severity = AlertSeverity.Warning,
                    Message = $"{forecast.ResourceType} is declining - {forecast.TimeDisplay} remaining",
                    TimeUntilCritical = forecast.TimeUntilDepletion,
                    ImpactedSystems = GetImpactedSystems(forecast.ResourceType)
                });
            }
        }

        return alerts.OrderBy(a => a.Severity).ToList();
    }

    private static List<string> GetImpactedSystems(string resourceType)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "iron ore" or "iron plate" => ["Smelters", "Constructors", "Production chains"],
            "copper ore" or "copper plate" => ["Electronics", "Wire production"],
            "coal" => ["Power generators", "Steel production"],
            _ => ["Multiple systems"]
        };
    }

    private static SustainabilityScore CalculateSustainability(List<ResourceForecast> forecasts)
    {
        var sustainable = forecasts.Count(f => f.Status == DepletionStatus.Sustainable);
        var depleting = forecasts.Count(f => f.Status == DepletionStatus.Declining);
        var critical = forecasts.Count(f => f.Status == DepletionStatus.Critical || f.Status == DepletionStatus.Depleted);

        var score = forecasts.Count > 0
            ? (sustainable * 100.0 + depleting * 50.0) / forecasts.Count
            : 0;

        var level = score switch
        {
            >= 90 => SustainabilityLevel.Excellent,
            >= 70 => SustainabilityLevel.Good,
            >= 50 => SustainabilityLevel.Moderate,
            >= 25 => SustainabilityLevel.Poor,
            _ => SustainabilityLevel.Critical
        };

        return new SustainabilityScore
        {
            OverallScore = score,
            SustainableResources = sustainable,
            DepletingResources = depleting,
            CriticalResources = critical,
            Assessment = GetAssessment(level),
            Level = level
        };
    }

    private static string GetAssessment(SustainabilityLevel level)
    {
        return level switch
        {
            SustainabilityLevel.Excellent => "Resource production exceeds consumption. Well balanced!",
            SustainabilityLevel.Good => "Most resources are sustainable with minor concerns.",
            SustainabilityLevel.Moderate => "Some resources need attention to maintain balance.",
            SustainabilityLevel.Poor => "Multiple resources are depleting. Action needed.",
            SustainabilityLevel.Critical => "Critical resource shortage! Immediate action required.",
            _ => "Unknown sustainability state"
        };
    }

    private static List<ResourceMitigation> GenerateMitigations(List<ResourceForecast> forecasts)
    {
        var mitigations = new List<ResourceMitigation>();

        foreach (var forecast in forecasts.Where(f => f.Status == DepletionStatus.Critical || f.Status == DepletionStatus.Declining))
        {
            var strategy = GetMitigationStrategy(forecast);
            mitigations.Add(new ResourceMitigation
            {
                ResourceType = forecast.ResourceType,
                Strategy = strategy.Name,
                Description = strategy.Description,
                Effort = strategy.Effort,
                ExpectedImprovement = strategy.Improvement
            });
        }

        return mitigations;
    }

    private static (string Name, string Description, MitigationEffort Effort, double Improvement) GetMitigationStrategy(ResourceForecast forecast)
    {
        if (forecast.ProductionRate == 0)
        {
            return ("Add Production", $"Build miners or producers for {forecast.ResourceType}",
                MitigationEffort.Medium, 100);
        }

        if (forecast.ConsumptionRate > forecast.ProductionRate * 2)
        {
            return ("Reduce Consumption", $"Disable some {forecast.ResourceType} consumers or add more production",
                MitigationEffort.Low, 50);
        }

        return ("Balance Production", $"Add more production capacity for {forecast.ResourceType}",
            MitigationEffort.Medium, 75);
    }

    private static string FormatResourceName(string resource)
    {
        return string.Join(" ", resource.Split('_').Select(w =>
            char.ToUpper(w[0]) + w.Substring(1)));
    }
}
