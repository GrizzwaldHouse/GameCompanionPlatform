namespace GameCompanion.Engine.RageClickDetector;

using GameCompanion.Engine.RageClickDetector.Detection;
using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Remediation;
using GameCompanion.Engine.RageClickDetector.Reporting;
using GameCompanion.Engine.RageClickDetector.Telemetry;
using GameCompanion.Engine.RageClickDetector.Validation;

/// <summary>
/// Main orchestrator for the rage-click detection system.
/// Coordinates interaction recording, pattern detection, scoring, remediation,
/// reporting, and validation.
///
/// Supports two operating modes:
/// - MODE_A (ANALYSIS_ONLY): Detection + reporting, no code changes
/// - MODE_B (AUTO_CODE_CHANGE): Detection + remediation + validation
/// </summary>
public sealed class RageClickDetectorService
{
    private readonly PrivacySafeTelemetryCollector _telemetry;
    private readonly IReadOnlyList<IPatternDetector> _detectors;
    private readonly AutoRemediationEngine _remediationEngine;
    private readonly RageClickReportGenerator _reportGenerator;
    private readonly DetectorConfiguration _config;

    public RageClickDetectorService(DetectorConfiguration? config = null)
    {
        _config = config ?? new DetectorConfiguration();

        _telemetry = new PrivacySafeTelemetryCollector(_config.MaxBufferSize);

        _detectors =
        [
            new RapidRepeatClickDetector(),
            new OscillatingNavigationDetector(),
            new FormSubmissionFailureDetector(),
            new DeadEndInteractionDetector()
        ];

        _remediationEngine = new AutoRemediationEngine();
        _reportGenerator = new RageClickReportGenerator();
    }

    /// <summary>
    /// Records a user interaction through the privacy-safe telemetry collector.
    /// </summary>
    public InteractionRecord RecordInteraction(
        string rawElementId,
        InteractionType interactionType,
        string screenName,
        bool causedStateChange = false,
        bool targetWasDisabled = false,
        bool resultedInValidationError = false,
        bool newGuidanceShown = false,
        NavigationDirection? direction = null)
    {
        return _telemetry.Record(
            rawElementId,
            interactionType,
            screenName,
            causedStateChange,
            targetWasDisabled,
            resultedInValidationError,
            newGuidanceShown,
            direction);
    }

    /// <summary>
    /// Runs rage-click detection against all collected interactions.
    /// Returns detected events without generating remediations (MODE_A).
    /// </summary>
    public IReadOnlyList<RageClickEvent> DetectRageClicks()
    {
        var interactions = _telemetry.GetInteractions();
        var events = new List<RageClickEvent>();

        foreach (var detector in _detectors)
        {
            events.AddRange(detector.Detect(interactions, _config));
        }

        return events;
    }

    /// <summary>
    /// Runs detection against a provided set of interactions (for testing/simulation).
    /// </summary>
    public IReadOnlyList<RageClickEvent> DetectRageClicks(
        IReadOnlyList<InteractionRecord> interactions)
    {
        var events = new List<RageClickEvent>();

        foreach (var detector in _detectors)
        {
            events.AddRange(detector.Detect(interactions, _config));
        }

        return events;
    }

    /// <summary>
    /// Generates a complete rage-click analysis report (MODE_A: analysis only).
    /// </summary>
    public RageClickReport Analyze()
    {
        var interactions = _telemetry.GetInteractions();
        var events = DetectRageClicks(interactions);
        var remediations = _remediationEngine.GenerateRemediations(events);

        return new RageClickReport
        {
            Events = events,
            Remediations = remediations,
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalInteractionsAnalyzed = interactions.Count
        };
    }

    /// <summary>
    /// Generates a report with validation (MODE_B: auto-remediation).
    /// Runs detection, generates remediations, simulates their effect,
    /// and reports the delta in rage intensity.
    /// </summary>
    public RageClickReport AnalyzeAndValidate()
    {
        var interactions = _telemetry.GetInteractions();
        var events = DetectRageClicks(interactions);
        var remediations = _remediationEngine.GenerateRemediations(events);

        // Simulate remediation effects
        var remediatedInteractions =
            RageClickSimulator.ApplyRemediationEffects(interactions, remediations);

        var simulator = new RageClickSimulator(_detectors, _config);
        var delta = simulator.Validate(events, remediatedInteractions);

        return new RageClickReport
        {
            Events = events,
            Remediations = remediations,
            Validation = delta,
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalInteractionsAnalyzed = interactions.Count
        };
    }

    /// <summary>
    /// Generates a markdown-formatted report string.
    /// </summary>
    public string GenerateMarkdownReport(RageClickReport report)
    {
        return _reportGenerator.GenerateMarkdown(report);
    }

    /// <summary>
    /// Clears the interaction buffer.
    /// </summary>
    public void Reset()
    {
        _telemetry.Clear();
    }

    /// <summary>
    /// Gets the current number of recorded interactions.
    /// </summary>
    public int InteractionCount => _telemetry.Count;
}
