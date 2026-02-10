namespace GameCompanion.Engine.RageClickDetector.Reporting;

using System.Text;
using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Generates markdown-formatted rage-click analysis reports.
/// Produces the required output table:
/// | Screen | Element | Rage Pattern | Intensity | Confidence | Suggested Fix |
/// </summary>
public sealed class RageClickReportGenerator
{
    /// <summary>
    /// Generates a complete markdown report from a rage-click analysis.
    /// </summary>
    public string GenerateMarkdown(RageClickReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Rage-Click Analysis Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine($"**Interactions Analyzed:** {report.TotalInteractionsAnalyzed}");
        sb.AppendLine($"**Rage Events Detected:** {report.Events.Count}");
        sb.AppendLine($"**Remediations Suggested:** {report.Remediations.Count}");
        sb.AppendLine();

        // Summary statistics
        if (report.Events.Count > 0)
        {
            var avgIntensity = report.Events.Average(e => e.RageIntensity);
            var avgConfidence = report.Events.Average(e => e.Confidence);
            var highRisk = report.Events.Count(e => e.RageIntensity >= 75);

            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine($"- **Average Rage Intensity:** {avgIntensity:F1}/100");
            sb.AppendLine($"- **Average Confidence:** {avgConfidence:F2}");
            sb.AppendLine($"- **High Abandonment Risk Events:** {highRisk}");
            sb.AppendLine();
        }

        // Required output table
        sb.AppendLine("## Detected Rage-Click Events");
        sb.AppendLine();
        sb.AppendLine("| Screen | Element | Rage Pattern | Intensity | Confidence | Suggested Fix |");
        sb.AppendLine("|--------|---------|--------------|-----------|------------|---------------|");

        foreach (var evt in report.Events.OrderByDescending(e => e.RageIntensity))
        {
            var suggestedFix = GetSuggestedFix(evt, report.Remediations);
            sb.AppendLine(
                $"| {evt.ScreenName} " +
                $"| {evt.UiElementId} " +
                $"| {FormatPattern(evt.Pattern)} " +
                $"| {evt.RageIntensity} ({GetIntensityLabel(evt.RageIntensity)}) " +
                $"| {evt.Confidence:F2} " +
                $"| {suggestedFix} |");
        }

        sb.AppendLine();

        // Remediation details
        if (report.Remediations.Count > 0)
        {
            sb.AppendLine("## Remediation Actions");
            sb.AppendLine();

            foreach (var group in report.Remediations.GroupBy(r => r.ScreenName))
            {
                sb.AppendLine($"### {group.Key}");
                sb.AppendLine();

                foreach (var remediation in group)
                {
                    var status = remediation.WasApplied ? "APPLIED" : "SUGGESTED";
                    sb.AppendLine($"- **[{status}]** [{remediation.Type}] {remediation.Description}");
                }

                sb.AppendLine();
            }
        }

        // Validation delta
        if (report.Validation is not null)
        {
            sb.AppendLine("## Validation Results");
            sb.AppendLine();
            sb.AppendLine($"- **Intensity Before:** {report.Validation.AverageIntensityBefore:F1}");
            sb.AppendLine($"- **Intensity After:** {report.Validation.AverageIntensityAfter:F1}");
            sb.AppendLine($"- **Delta:** {report.Validation.IntensityDelta:F1}");
            sb.AppendLine();

            if (report.Validation.RemainingHighConfidenceEvents.Count > 0)
            {
                sb.AppendLine("### Remaining High-Confidence Frustration Points");
                sb.AppendLine();

                foreach (var remaining in report.Validation.RemainingHighConfidenceEvents)
                {
                    sb.AppendLine(
                        $"- **{remaining.ScreenName}** / {remaining.UiElementId}: " +
                        $"Intensity={remaining.RageIntensity}, " +
                        $"Confidence={remaining.Confidence:F2}, " +
                        $"Cause={remaining.RootCause}");
                }

                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("No remaining high-confidence frustration points.");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string FormatPattern(RageClickPattern pattern) => pattern switch
    {
        RageClickPattern.RapidRepeatClick => "Rapid Repeat Click",
        RageClickPattern.OscillatingNavigation => "Oscillating Navigation",
        RageClickPattern.FormSubmissionFailureLoop => "Form Failure Loop",
        RageClickPattern.DeadEndInteraction => "Dead-End Interaction",
        _ => pattern.ToString()
    };

    private static string GetIntensityLabel(int intensity) => intensity switch
    {
        >= 75 => "High Abandonment Risk",
        >= 50 => "Clear Frustration",
        >= 30 => "Mild Confusion",
        _ => "Low"
    };

    private static string GetSuggestedFix(
        RageClickEvent evt,
        IReadOnlyList<RemediationAction> remediations)
    {
        var matching = remediations
            .Where(r => r.ScreenName == evt.ScreenName && r.TargetElementId == evt.UiElementId)
            .ToList();

        if (matching.Count == 0)
            return GetDefaultFix(evt.RootCause);

        return string.Join("; ", matching.Select(r => r.Description.Split(':')[0].Trim()));
    }

    private static string GetDefaultFix(LikelyRootCause cause) => cause switch
    {
        LikelyRootCause.UnclearCopy => "Clarify button/label text",
        LikelyRootCause.MissingFeedback => "Add inline loading/success/error feedback",
        LikelyRootCause.DisabledStateAmbiguity => "Improve disabled state visual clarity",
        LikelyRootCause.ValidationOpacity => "Add field-level validation messages",
        LikelyRootCause.NavigationAmbiguity => "Clarify navigation labels/breadcrumbs",
        _ => "Review UX design"
    };
}
