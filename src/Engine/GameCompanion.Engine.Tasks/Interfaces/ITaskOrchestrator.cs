namespace GameCompanion.Engine.Tasks.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Tasks.Models;

/// <summary>
/// Orchestrates task lists for progression tracking.
/// Supports creating, resuming, and completing task lists and their items.
/// </summary>
public interface ITaskOrchestrator
{
    /// <summary>
    /// The currently active task list ID, if any.
    /// </summary>
    string? CurrentTaskListId { get; }

    /// <summary>
    /// The current state of the active task list.
    /// </summary>
    TaskList? CurrentTaskList { get; }

    /// <summary>
    /// Creates a new task list for a progression phase.
    /// </summary>
    Task<Result<TaskList>> CreateTaskListAsync(
        string gameId,
        string phaseId,
        string saveId,
        CancellationToken ct = default);

    /// <summary>
    /// Resumes an existing task list by ID.
    /// </summary>
    Task<Result<TaskList>> ResumeTaskListAsync(
        string taskListId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all task lists for a save.
    /// </summary>
    Task<Result<IReadOnlyList<TaskList>>> GetTaskListsForSaveAsync(
        string saveId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a step as completed.
    /// </summary>
    Task<Result<Unit>> CompleteStepAsync(
        string taskListId,
        string stepId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a step as skipped.
    /// </summary>
    Task<Result<Unit>> SkipStepAsync(
        string taskListId,
        string stepId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a checklist item as completed or uncompleted.
    /// </summary>
    Task<Result<Unit>> MarkChecklistItemAsync(
        string taskListId,
        string stepId,
        string itemId,
        bool completed,
        CancellationToken ct = default);

    /// <summary>
    /// Abandons a task list.
    /// </summary>
    Task<Result<Unit>> AbandonTaskListAsync(
        string taskListId,
        CancellationToken ct = default);

    /// <summary>
    /// Observable for task list state changes.
    /// </summary>
    event EventHandler<TaskListChangedEventArgs>? TaskListChanged;
}

/// <summary>
/// Event args for task list changes.
/// </summary>
public sealed class TaskListChangedEventArgs : EventArgs
{
    public required TaskList TaskList { get; init; }
    public required TaskListChangeType ChangeType { get; init; }
    public string? AffectedStepId { get; init; }
    public string? AffectedItemId { get; init; }
}

/// <summary>
/// Type of change to a task list.
/// </summary>
public enum TaskListChangeType
{
    Created,
    Resumed,
    StepCompleted,
    StepSkipped,
    ChecklistItemChanged,
    Completed,
    Abandoned
}
