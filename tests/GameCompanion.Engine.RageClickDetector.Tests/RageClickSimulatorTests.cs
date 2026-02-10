namespace GameCompanion.Engine.RageClickDetector.Tests;

using FluentAssertions;
using GameCompanion.Engine.RageClickDetector.Models;
using GameCompanion.Engine.RageClickDetector.Validation;

public class RageClickSimulatorTests
{
    [Fact]
    public void ApplyRemediationEffects_InlineFeedback_SetsCausedStateChange()
    {
        var interactions = new List<InteractionRecord>
        {
            new()
            {
                AnonymizedSessionId = "session1",
                UiElementId = "btn_save",
                InteractionType = InteractionType.Click,
                Timestamp = DateTimeOffset.UtcNow,
                ScreenName = "Settings",
                CausedStateChange = false
            }
        };

        var remediations = new List<RemediationAction>
        {
            new()
            {
                Type = RemediationType.AddInlineFeedback,
                TargetElementId = "btn_save",
                ScreenName = "Settings",
                Description = "Add loading indicator"
            }
        };

        var result = RageClickSimulator.ApplyRemediationEffects(interactions, remediations);

        result.Should().HaveCount(1);
        result[0].CausedStateChange.Should().BeTrue();
    }

    [Fact]
    public void ApplyRemediationEffects_MicroGuidance_SetsNewGuidanceShown()
    {
        var interactions = new List<InteractionRecord>
        {
            new()
            {
                AnonymizedSessionId = "session1",
                UiElementId = "btn_submit",
                InteractionType = InteractionType.Submit,
                Timestamp = DateTimeOffset.UtcNow,
                ScreenName = "Report",
                ResultedInValidationError = true,
                NewGuidanceShown = false
            }
        };

        var remediations = new List<RemediationAction>
        {
            new()
            {
                Type = RemediationType.IntroduceMicroGuidance,
                TargetElementId = "btn_submit",
                ScreenName = "Report",
                Description = "Add helper text"
            }
        };

        var result = RageClickSimulator.ApplyRemediationEffects(interactions, remediations);

        result[0].NewGuidanceShown.Should().BeTrue();
    }

    [Fact]
    public void ApplyRemediationEffects_UnmatchedElements_Unchanged()
    {
        var interactions = new List<InteractionRecord>
        {
            new()
            {
                AnonymizedSessionId = "session1",
                UiElementId = "btn_other",
                InteractionType = InteractionType.Click,
                Timestamp = DateTimeOffset.UtcNow,
                ScreenName = "Settings",
                CausedStateChange = false
            }
        };

        var remediations = new List<RemediationAction>
        {
            new()
            {
                Type = RemediationType.AddInlineFeedback,
                TargetElementId = "btn_save",
                ScreenName = "Settings",
                Description = "Add loading indicator"
            }
        };

        var result = RageClickSimulator.ApplyRemediationEffects(interactions, remediations);

        result[0].CausedStateChange.Should().BeFalse();
    }

    [Fact]
    public void ApplyRemediationEffects_VisualAffordance_SetsStateChangeForDisabled()
    {
        var interactions = new List<InteractionRecord>
        {
            new()
            {
                AnonymizedSessionId = "session1",
                UiElementId = "btn_export",
                InteractionType = InteractionType.Click,
                Timestamp = DateTimeOffset.UtcNow,
                ScreenName = "Export",
                TargetWasDisabled = true,
                CausedStateChange = false
            }
        };

        var remediations = new List<RemediationAction>
        {
            new()
            {
                Type = RemediationType.AddVisualAffordance,
                TargetElementId = "btn_export",
                ScreenName = "Export",
                Description = "Improve disabled styling"
            }
        };

        var result = RageClickSimulator.ApplyRemediationEffects(interactions, remediations);

        result[0].CausedStateChange.Should().BeTrue();
    }
}
