using GexVisor.Core;
using System.Text.Json;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for persisting and loading ToDoTask lists.
/// Handles serialization to JSON format.
/// </summary>
public class TaskPersistenceService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private const string TasksFileName = "MyTasks.json";
    private const string TasksDirectoryPath = "./Data/";

    public TaskPersistenceService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// Saves a list of ToDoTask objects to JSON file.
    /// </summary>
    /// <param name="tasks">The tasks to save.</param>
    public void SaveTasks(List<ToDoTask> tasks)
    {
        try
        {
            EnsureDirectoryExists(TasksDirectoryPath);
            string filePath = Path.Combine(TasksDirectoryPath, TasksFileName);
            string jsonString = JsonSerializer.Serialize(tasks, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save tasks to {TasksFileName}", ex);
        }
    }

    /// <summary>
    /// Loads a list of ToDoTask objects from JSON file.
    /// </summary>
    /// <returns>The loaded tasks, or an empty list if file doesn't exist.</returns>
    public List<ToDoTask> LoadTasks()
    {
        try
        {
            string filePath = Path.Combine(TasksDirectoryPath, TasksFileName);

            if (!File.Exists(filePath))
                return new List<ToDoTask>();

            string jsonString = File.ReadAllText(filePath);
            var tasks = JsonSerializer.Deserialize<List<ToDoTask>>(jsonString, _jsonOptions);
            return tasks ?? new List<ToDoTask>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load tasks from {TasksFileName}", ex);
        }
    }

    /// <summary>
    /// Ensures the data directory exists, creating it if necessary.
    /// </summary>
    private void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
