using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing trading goals.
/// </summary>
public interface IGoalService
{
    /// <summary>
    /// Event fired when goals are added, updated, or deleted.
    /// </summary>
    event Action? OnGoalsChanged;

    /// <summary>
    /// All goals (active, completed, and archived).
    /// </summary>
    IReadOnlyList<TradingGoal> AllGoals { get; }

    /// <summary>
    /// Load goals from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Add a new goal.
    /// </summary>
    Task AddAsync(TradingGoal goal);

    /// <summary>
    /// Update an existing goal.
    /// </summary>
    Task UpdateAsync(TradingGoal goal);

    /// <summary>
    /// Delete a goal by ID.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Get a goal by ID.
    /// </summary>
    TradingGoal? GetById(Guid id);

    /// <summary>
    /// Get goals filtered by status.
    /// </summary>
    IEnumerable<TradingGoal> GetByStatus(GoalStatus status);

    /// <summary>
    /// Get active goals sorted by due date (closest first).
    /// </summary>
    IEnumerable<TradingGoal> GetActiveGoals();

    /// <summary>
    /// Recalculate progress for all active goals based on current trade data.
    /// Should be called after trade data changes.
    /// </summary>
    Task RecalculateProgressAsync();

    /// <summary>
    /// Mark a goal as completed.
    /// </summary>
    Task CompleteGoalAsync(Guid id);

    /// <summary>
    /// Archive a goal (hide from active view).
    /// </summary>
    Task ArchiveGoalAsync(Guid id);

    /// <summary>
    /// Reactivate an archived or failed goal.
    /// </summary>
    Task ReactivateGoalAsync(Guid id);

    /// <summary>
    /// Check and update goals that are past their due date.
    /// </summary>
    Task CheckOverdueGoalsAsync();
}
