namespace GameCompanion.Engine.Tasks.Models;

/// <summary>
/// A list of tasks representing a player's journey through a progression phase.
/// Supports persistence and resume via TaskListId.
/// </summary>
public sealed class TaskList
{
    public required string Id { get; init; }
    public required string GameId { get; init; }
    public required string PhaseId { get; init; }
    public required string SaveId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public required IReadOnlyList<TaskItem> Items { get; init; }
    public TaskListStatus Status { get; set; } = TaskListStatus.InProgress;
}

/// <summary>
/// Status of a task list.
/// </summary>
public enum TaskListStatus
{
    InProgress,
    Completed,
    Abandoned
}

/// <summary>
/// A single task item within a task list.
/// </summary>
public sealed class TaskItem
{
    public required string Id { get; init; }
    public required string StepId { get; init; }
    public required string Title { get; init; }
    public required int Order { get; init; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyList<TaskChecklistItem> ChecklistItems { get; init; } = [];
}

/// <summary>
/// Status of a task item.
/// </summary>
public enum TaskItemStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped
}

/// <summary>
/// A checklist item within a task.
/// </summary>
public sealed class TaskChecklistItem
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public bool IsCompleted { get; set; }
    public bool IsAutoDetected { get; set; }
}
