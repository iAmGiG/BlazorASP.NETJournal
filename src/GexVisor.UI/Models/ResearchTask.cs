namespace GexVisor.UI.Models;

/// <summary>
/// Represents a research task for the Kanban-style task board.
/// </summary>
public record ResearchTask : BaseEntry
{
    /// <summary>Task title/summary.</summary>
    public required string Title { get; init; }

    /// <summary>Optional detailed description.</summary>
    public string? Description { get; init; }

    /// <summary>Task status: Backlog, InProgress, or Done.</summary>
    public string Status { get; init; } = ResearchTaskStatus.Backlog;

    /// <summary>Task priority: High, Medium, or Low.</summary>
    public string Priority { get; init; } = TaskPriority.Medium;

    /// <summary>Optional link to a related GitHub issue.</summary>
    public string? GitHubIssueUrl { get; init; }

    /// <summary>When the task was completed (moved to Done).</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Position within the column for ordering.</summary>
    public int Position { get; init; }

    /// <summary>Whether this task is archived (hidden from board).</summary>
    public bool IsArchived { get; init; }
}

/// <summary>
/// Task status constants for Kanban columns.
/// </summary>
public static class ResearchTaskStatus
{
    public const string Backlog = "Backlog";
    public const string InProgress = "InProgress";
    public const string Done = "Done";

    public static readonly string[] All = [Backlog, InProgress, Done];
}

/// <summary>
/// Task priority constants.
/// </summary>
public static class TaskPriority
{
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";

    public static readonly string[] All = [High, Medium, Low];
}
