namespace GexVisor.UI.Models;

/// <summary>
/// Types of trading goals that can be tracked.
/// </summary>
public enum GoalType
{
    /// <summary>Target total P&L amount ($X profit).</summary>
    TotalPnL,

    /// <summary>Target win rate percentage.</summary>
    WinRate,

    /// <summary>Target number of trades.</summary>
    TradeCount,

    /// <summary>Target consecutive winning days.</summary>
    WinningDaysStreak,

    /// <summary>Target number of documented lessons.</summary>
    LessonsLearned,

    /// <summary>Target average P&L per trade.</summary>
    AveragePnL,

    /// <summary>Custom goal with manual progress tracking.</summary>
    Custom
}

/// <summary>
/// Status of a trading goal.
/// </summary>
public enum GoalStatus
{
    /// <summary>Goal is being actively tracked.</summary>
    Active,

    /// <summary>Goal has been achieved.</summary>
    Completed,

    /// <summary>Goal has been archived (hidden from active view).</summary>
    Archived,

    /// <summary>Goal was not achieved by due date.</summary>
    Failed
}

/// <summary>
/// Represents a trading goal that can be tracked over time.
/// Implements IEntry for consistent CRUD operations.
/// </summary>
public record TradingGoal : IEntry
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>When the goal was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When the goal was last updated.</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>Tags for categorization.</summary>
    public List<string> Tags { get; init; } = [];

    IReadOnlyList<string> IEntry.Tags => Tags;

    /// <summary>Goal title/name.</summary>
    public required string Title { get; init; }

    /// <summary>Optional detailed description.</summary>
    public string? Description { get; init; }

    /// <summary>Type of goal (determines how progress is calculated).</summary>
    public required GoalType Type { get; init; }

    /// <summary>Target value to achieve.</summary>
    public required decimal TargetValue { get; init; }

    /// <summary>Current progress value.</summary>
    public decimal CurrentValue { get; set; }

    /// <summary>Target completion date (optional).</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>Goal status.</summary>
    public GoalStatus Status { get; set; } = GoalStatus.Active;

    /// <summary>When the goal was completed (if applicable).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Start date for measuring progress (defaults to creation date).</summary>
    public DateOnly StartDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Progress as percentage (0-100).</summary>
    public decimal ProgressPercent => TargetValue != 0
        ? Math.Min(100, Math.Max(0, CurrentValue / TargetValue * 100))
        : 0;

    /// <summary>Whether the goal target has been achieved.</summary>
    public bool IsAchieved => CurrentValue >= TargetValue;

    /// <summary>Whether the goal is overdue (past due date and not completed).</summary>
    public bool IsOverdue => DueDate.HasValue
        && DateOnly.FromDateTime(DateTime.UtcNow) > DueDate.Value
        && Status == GoalStatus.Active;

    /// <summary>Days remaining until due date (negative if overdue).</summary>
    public int? DaysRemaining => DueDate.HasValue
        ? DueDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber
        : null;

    /// <summary>
    /// Gets a display string for the target value based on goal type.
    /// </summary>
    public string TargetDisplay => Type switch
    {
        GoalType.TotalPnL => $"${TargetValue:N0}",
        GoalType.WinRate => $"{TargetValue:N0}%",
        GoalType.TradeCount => $"{TargetValue:N0} trades",
        GoalType.WinningDaysStreak => $"{TargetValue:N0} days",
        GoalType.LessonsLearned => $"{TargetValue:N0} lessons",
        GoalType.AveragePnL => $"${TargetValue:N0}/trade",
        GoalType.Custom => $"{TargetValue:N0}",
        _ => TargetValue.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Gets a display string for the current value based on goal type.
    /// </summary>
    public string CurrentDisplay => Type switch
    {
        GoalType.TotalPnL => $"${CurrentValue:N0}",
        GoalType.WinRate => $"{CurrentValue:N1}%",
        GoalType.TradeCount => $"{CurrentValue:N0}",
        GoalType.WinningDaysStreak => $"{CurrentValue:N0}",
        GoalType.LessonsLearned => $"{CurrentValue:N0}",
        GoalType.AveragePnL => $"${CurrentValue:N0}",
        GoalType.Custom => $"{CurrentValue:N0}",
        _ => CurrentValue.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Creates a new goal with the specified parameters.
    /// </summary>
    public static TradingGoal Create(string title, GoalType type, decimal target, DateOnly? dueDate = null)
    {
        return new TradingGoal
        {
            Title = title,
            Type = type,
            TargetValue = target,
            DueDate = dueDate,
            Status = GoalStatus.Active
        };
    }
}
