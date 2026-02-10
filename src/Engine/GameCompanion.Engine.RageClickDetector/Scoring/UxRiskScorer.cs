namespace GameCompanion.Engine.RageClickDetector.Scoring;

using GameCompanion.Engine.RageClickDetector.Models;

/// <summary>
/// Calculates rage intensity (0-100) and confidence (0.0-1.0) scores for detected patterns.
/// Scoring ranges: 30-50 mild confusion, 50-75 clear frustration, 75-100 high abandonment risk.
/// </summary>
public static class UxRiskScorer
{
    /// <summary>
    /// Calculate rage intensity for rapid repeat clicks.
    /// Factors: click count above threshold, time compression (faster = angrier).
    /// </summary>
    public static int CalculateRapidClickIntensity(
        int clickCount,
        int minRequired,
        TimeSpan maxWindow,
        TimeSpan actualDuration)
    {
        // Base score: 30 at threshold, scaling up with extra clicks
        double excessRatio = (double)(clickCount - minRequired) / minRequired;
        double baseScore = 30 + (excessRatio * 30);

        // Time compression bonus: faster clicks = more frustration
        double timeRatio = actualDuration.TotalMilliseconds / maxWindow.TotalMilliseconds;
        double timeBonus = (1.0 - timeRatio) * 20;

        return ClampIntensity((int)(baseScore + timeBonus));
    }

    /// <summary>
    /// Calculate rage intensity for oscillating navigation.
    /// Factors: number of cycles above threshold.
    /// </summary>
    public static int CalculateOscillationIntensity(int cycles, int minRequired)
    {
        double excessRatio = (double)(cycles - minRequired) / minRequired;
        double score = 40 + (excessRatio * 35);
        return ClampIntensity((int)score);
    }

    /// <summary>
    /// Calculate rage intensity for form submission failure loops.
    /// Factors: number of failed attempts above threshold.
    /// </summary>
    public static int CalculateFormFailureIntensity(int failedAttempts, int minRequired)
    {
        double excessRatio = (double)(failedAttempts - minRequired) / minRequired;
        double score = 50 + (excessRatio * 30);
        return ClampIntensity((int)score);
    }

    /// <summary>
    /// Calculate rage intensity for dead-end interactions.
    /// Factors: click count above threshold.
    /// </summary>
    public static int CalculateDeadEndIntensity(int clickCount, int minRequired)
    {
        double excessRatio = (double)(clickCount - minRequired) / minRequired;
        double score = 35 + (excessRatio * 30);
        return ClampIntensity((int)score);
    }

    /// <summary>
    /// Calculate confidence score based on frequency and pattern type.
    /// Returns 0.0-1.0 where higher values indicate stronger pattern match.
    /// </summary>
    public static double CalculateConfidence(
        int observedCount,
        int minRequired,
        RageClickPattern pattern)
    {
        // Base confidence: 0.5 at threshold
        double baseConfidence = 0.5;

        // Scale up with excess observations
        double excessRatio = (double)(observedCount - minRequired) / Math.Max(minRequired, 1);
        double scaledConfidence = baseConfidence + (excessRatio * 0.3);

        // Pattern-specific weight: form failures are higher confidence
        // because they have clear cause-effect
        double patternWeight = pattern switch
        {
            RageClickPattern.FormSubmissionFailureLoop => 1.1,
            RageClickPattern.RapidRepeatClick => 1.0,
            RageClickPattern.DeadEndInteraction => 0.95,
            RageClickPattern.OscillatingNavigation => 0.9,
            _ => 1.0
        };

        return Math.Clamp(scaledConfidence * patternWeight, 0.0, 1.0);
    }

    private static int ClampIntensity(int score) => Math.Clamp(score, 0, 100);
}
