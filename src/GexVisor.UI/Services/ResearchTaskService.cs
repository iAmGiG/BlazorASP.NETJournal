using System.Text;
using System.Text.Json;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing research tasks in a Kanban-style board.
/// </summary>
public class ResearchTaskService : BaseEntryService<ResearchTask>
{
    public ResearchTaskService(LocalStorageService storage)
        : base(storage, StorageKeys.TaskBoard)
    {
    }

    /// <summary>
    /// Search tasks by title, description, or tags.
    /// </summary>
    public override IEnumerable<ResearchTask> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Entries;

        return Entries.Where(t =>
            t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (t.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Get tasks for a specific column, ordered by position.
    /// </summary>
    public IEnumerable<ResearchTask> GetByStatus(string status)
    {
        return Entries
            .Where(t => t.Status == status && !t.IsArchived)
            .OrderBy(t => t.Position);
    }

    /// <summary>
    /// Get count for each column.
    /// </summary>
    public Dictionary<string, int> GetColumnCounts()
    {
        return new Dictionary<string, int>
        {
            [ResearchTaskStatus.Backlog] = Entries.Count(t => t.Status == ResearchTaskStatus.Backlog && !t.IsArchived),
            [ResearchTaskStatus.InProgress] = Entries.Count(t => t.Status == ResearchTaskStatus.InProgress && !t.IsArchived),
            [ResearchTaskStatus.Done] = Entries.Count(t => t.Status == ResearchTaskStatus.Done && !t.IsArchived)
        };
    }

    /// <summary>
    /// Move a task to a different column.
    /// </summary>
    public async Task MoveToStatusAsync(Guid id, string newStatus)
    {
        var task = GetById(id);
        if (task == null)
            return;

        var nextPosition = GetNextPosition(newStatus);
        var updated = task with
        {
            Status = newStatus,
            Position = nextPosition,
            UpdatedAt = DateTime.UtcNow,
            CompletedAt = newStatus == ResearchTaskStatus.Done ? DateTime.UtcNow : null
        };

        await UpdateAsync(updated);
    }

    /// <summary>
    /// Quick add a task to backlog with just a title.
    /// </summary>
    public async Task<ResearchTask> QuickAddAsync(string title, IEnumerable<string>? tags = null)
    {
        var task = new ResearchTask
        {
            Title = title,
            Status = ResearchTaskStatus.Backlog,
            Priority = TaskPriority.Medium,
            Position = GetNextPosition(ResearchTaskStatus.Backlog),
            Tags = tags?.ToList() ?? []
        };

        await AddAsync(task);
        return task;
    }

    /// <summary>
    /// Archive a completed task.
    /// </summary>
    public async Task ArchiveAsync(Guid id)
    {
        var task = GetById(id);
        if (task == null)
            return;

        var updated = task with { IsArchived = true, UpdatedAt = DateTime.UtcNow };
        await UpdateAsync(updated);
    }

    /// <summary>
    /// Unarchive a task.
    /// </summary>
    public async Task UnarchiveAsync(Guid id)
    {
        var task = GetById(id);
        if (task == null)
            return;

        var updated = task with { IsArchived = false, UpdatedAt = DateTime.UtcNow };
        await UpdateAsync(updated);
    }

    /// <summary>
    /// Get archived tasks.
    /// </summary>
    public IEnumerable<ResearchTask> GetArchived()
    {
        return Entries.Where(t => t.IsArchived).OrderByDescending(t => t.CompletedAt);
    }

    /// <summary>
    /// Archive all completed tasks.
    /// </summary>
    public async Task ArchiveAllCompletedAsync()
    {
        var completed = Entries
            .Where(t => t.Status == ResearchTaskStatus.Done && !t.IsArchived)
            .ToList();

        foreach (var task in completed)
        {
            var index = Entries.FindIndex(e => e.Id == task.Id);
            if (index >= 0)
            {
                Entries[index] = task with { IsArchived = true, UpdatedAt = DateTime.UtcNow };
            }
        }

        await SaveAsync();
    }

    /// <summary>
    /// Get task statistics.
    /// </summary>
    public TaskBoardSummary GetSummary()
    {
        var active = Entries.Where(t => !t.IsArchived).ToList();

        return new TaskBoardSummary
        {
            TotalActive = active.Count,
            BacklogCount = active.Count(t => t.Status == ResearchTaskStatus.Backlog),
            InProgressCount = active.Count(t => t.Status == ResearchTaskStatus.InProgress),
            DoneCount = active.Count(t => t.Status == ResearchTaskStatus.Done),
            ArchivedCount = Entries.Count(t => t.IsArchived),
            HighPriorityCount = active.Count(t => t.Priority == TaskPriority.High),
            CompletedThisWeek = active.Count(t =>
                t.Status == ResearchTaskStatus.Done &&
                t.CompletedAt.HasValue &&
                t.CompletedAt.Value >= DateTime.UtcNow.AddDays(-7))
        };
    }

    /// <summary>
    /// Update task priority.
    /// </summary>
    public async Task SetPriorityAsync(Guid id, string priority)
    {
        var task = GetById(id);
        if (task == null)
            return;

        var updated = task with { Priority = priority, UpdatedAt = DateTime.UtcNow };
        await UpdateAsync(updated);
    }

    /// <summary>
    /// Get the next position number for a column.
    /// </summary>
    private int GetNextPosition(string status)
    {
        var maxPos = Entries
            .Where(t => t.Status == status && !t.IsArchived)
            .Select(t => t.Position)
            .DefaultIfEmpty(-1)
            .Max();
        return maxPos + 1;
    }

    // === Export ===

    /// <summary>
    /// Export tasks as JSON.
    /// </summary>
    public string ExportAsJson()
    {
        return JsonSerializer.Serialize(Entries, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Export tasks as CSV.
    /// </summary>
    public string ExportAsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAt,Title,Description,Status,Priority,Tags,CompletedAt,GitHubIssueUrl");

        foreach (var t in Entries.Where(t => !t.IsArchived))
        {
            var tags = string.Join(";", t.Tags);
            var desc = t.Description?.Replace("\"", "\"\"") ?? "";
            sb.AppendLine($"{t.Id},{t.CreatedAt:O},\"{t.Title}\",\"{desc}\",{t.Status},{t.Priority},\"{tags}\",{t.CompletedAt:O},{t.GitHubIssueUrl}");
        }

        return sb.ToString();
    }
}

// === DTOs ===

public record TaskBoardSummary
{
    public int TotalActive { get; init; }
    public int BacklogCount { get; init; }
    public int InProgressCount { get; init; }
    public int DoneCount { get; init; }
    public int ArchivedCount { get; init; }
    public int HighPriorityCount { get; init; }
    public int CompletedThisWeek { get; init; }
}
