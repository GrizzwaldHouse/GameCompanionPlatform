namespace GameCompanion.Core.Models;

/// <summary>
/// Represents a single step in a progression phase with detailed guidance.
/// </summary>
public sealed record Step(
    string Id,
    string Title,
    string WhyItMatters,
    IReadOnlyList<StepAction> Actions,
    IReadOnlyList<StepChecklistItem> Checklist,
    IReadOnlyList<string> Prerequisites);

/// <summary>
/// An in-game action to perform as part of a step.
/// </summary>
public sealed record StepAction(
    int Order,
    string Description,
    string? Hint = null);

/// <summary>
/// A checklist item within a step that can be marked complete.
/// </summary>
public sealed record StepChecklistItem(
    string Id,
    string Text,
    bool IsAutoDetectable = false);
