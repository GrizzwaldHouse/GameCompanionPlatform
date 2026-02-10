namespace GameCompanion.Engine.Tasks.Interfaces;

using GameCompanion.Core.Models;
using GameCompanion.Engine.Tasks.Models;

/// <summary>
/// Repository for persisting task lists.
/// </summary>
public interface ITaskRepository
{
    Task<Result<Unit>> SaveAsync(TaskList taskList, CancellationToken ct = default);
    Task<Result<TaskList>> GetByIdAsync(string taskListId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskList>>> GetBySaveIdAsync(string saveId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskList>>> GetByGameIdAsync(string gameId, CancellationToken ct = default);
    Task<Result<Unit>> DeleteAsync(string taskListId, CancellationToken ct = default);
}
