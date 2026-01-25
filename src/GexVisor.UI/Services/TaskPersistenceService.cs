using GexVisor.Core;
using GexVisor.UI.Configuration;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for persisting and loading ToDoTask lists.
/// Handles serialization to JSON format.
/// </summary>
public class TaskPersistenceService
{
    private readonly ILocalStorageService _storage;

    public TaskPersistenceService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Saves a list of ToDoTask objects to local storage.
    /// </summary>
    /// <param name="tasks">The tasks to save.</param>
    public async Task SaveTasksAsync(List<ToDoTask> tasks)
    {
        await _storage.SetAsync(AppConstants.Storage.TasksKey, tasks);
    }

    /// <summary>
    /// Loads a list of ToDoTask objects from local storage.
    /// </summary>
    /// <returns>The loaded tasks, or an empty list if none exist.</returns>
    public async Task<List<ToDoTask>> LoadTasksAsync()
    {
        var tasks = await _storage.GetAsync<List<ToDoTask>>(AppConstants.Storage.TasksKey);
        return tasks ?? new List<ToDoTask>();
    }
}
