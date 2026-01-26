using System.Text.Json;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing trading goals with persistence and auto-calculation.
/// </summary>
public class GoalService : IGoalService
{
    private readonly ILocalStorageService _storage;
    private readonly TradeLogService _tradeLogService;
    private readonly IPersonalAnalyticsService _analyticsService;
    private List<TradingGoal> _goals = [];

    public event Action? OnGoalsChanged;

    public IReadOnlyList<TradingGoal> AllGoals => _goals;

    public GoalService(
        ILocalStorageService storage,
        TradeLogService tradeLogService,
        IPersonalAnalyticsService analyticsService)
    {
        _storage = storage;
        _tradeLogService = tradeLogService;
        _analyticsService = analyticsService;

        // Subscribe to trade changes to auto-update goal progress
        _tradeLogService.OnTradesChanged += async () => await RecalculateProgressAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            var stored = await _storage.GetAsync<List<TradingGoal>>(AppConstants.Storage.TradingGoals);
            _goals = stored ?? [];
        }
        catch (JsonException)
        {
            _goals = [];
        }
        OnGoalsChanged?.Invoke();
    }

    private async Task SaveAsync()
    {
        await _storage.SetAsync(AppConstants.Storage.TradingGoals, _goals);
        OnGoalsChanged?.Invoke();
    }

    public async Task AddAsync(TradingGoal goal)
    {
        _goals.Insert(0, goal);
        await SaveAsync();
    }

    public async Task UpdateAsync(TradingGoal goal)
    {
        var index = _goals.FindIndex(g => g.Id == goal.Id);
        if (index >= 0)
        {
            _goals[index] = goal with { UpdatedAt = DateTime.UtcNow };
            await SaveAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        _goals.RemoveAll(g => g.Id == id);
        await SaveAsync();
    }

    public TradingGoal? GetById(Guid id)
    {
        return _goals.FirstOrDefault(g => g.Id == id);
    }

    public IEnumerable<TradingGoal> GetByStatus(GoalStatus status)
    {
        return _goals.Where(g => g.Status == status);
    }

    public IEnumerable<TradingGoal> GetActiveGoals()
    {
        return _goals
            .Where(g => g.Status == GoalStatus.Active)
            .OrderBy(g => g.DueDate ?? DateOnly.MaxValue)
            .ThenBy(g => g.CreatedAt);
    }

    public async Task RecalculateProgressAsync()
    {
        var activeGoals = _goals.Where(g => g.Status == GoalStatus.Active).ToList();
        var hasChanges = false;

        foreach (var goal in activeGoals)
        {
            var newValue = CalculateCurrentValue(goal);
            if (newValue != goal.CurrentValue)
            {
                goal.CurrentValue = newValue;
                hasChanges = true;

                // Auto-complete if target reached
                if (goal.IsAchieved)
                {
                    goal.Status = GoalStatus.Completed;
                    goal.CompletedAt = DateTime.UtcNow;
                }
            }
        }

        if (hasChanges)
        {
            await SaveAsync();
        }
    }

    public async Task CompleteGoalAsync(Guid id)
    {
        var goal = GetById(id);
        if (goal != null && goal.Status == GoalStatus.Active)
        {
            var index = _goals.FindIndex(g => g.Id == id);
            _goals[index] = goal with
            {
                Status = GoalStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await SaveAsync();
        }
    }

    public async Task ArchiveGoalAsync(Guid id)
    {
        var goal = GetById(id);
        if (goal != null)
        {
            var index = _goals.FindIndex(g => g.Id == id);
            _goals[index] = goal with
            {
                Status = GoalStatus.Archived,
                UpdatedAt = DateTime.UtcNow
            };
            await SaveAsync();
        }
    }

    public async Task ReactivateGoalAsync(Guid id)
    {
        var goal = GetById(id);
        if (goal != null && goal.Status != GoalStatus.Active)
        {
            var index = _goals.FindIndex(g => g.Id == id);
            _goals[index] = goal with
            {
                Status = GoalStatus.Active,
                CompletedAt = null,
                UpdatedAt = DateTime.UtcNow
            };
            await SaveAsync();
        }
    }

    public async Task CheckOverdueGoalsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueGoals = _goals
            .Where(g => g.Status == GoalStatus.Active
                && g.DueDate.HasValue
                && g.DueDate.Value < today
                && !g.IsAchieved)
            .ToList();

        if (overdueGoals.Any())
        {
            foreach (var goal in overdueGoals)
            {
                var index = _goals.FindIndex(g => g.Id == goal.Id);
                _goals[index] = goal with
                {
                    Status = GoalStatus.Failed,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            await SaveAsync();
        }
    }

    /// <summary>
    /// Calculate the current value for a goal based on trade data.
    /// </summary>
    private decimal CalculateCurrentValue(TradingGoal goal)
    {
        var startDate = goal.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = DateTime.UtcNow;

        var trades = _tradeLogService.GetClosedTrades()
            .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
            .ToList();

        return goal.Type switch
        {
            GoalType.TotalPnL => trades.Sum(t => t.CalculatePnL() ?? 0),

            GoalType.WinRate => trades.Count > 0
                ? (decimal)trades.Count(t => (t.CalculatePnL() ?? 0) > 0) / trades.Count * 100
                : 0,

            GoalType.TradeCount => trades.Count,

            GoalType.WinningDaysStreak => CalculateWinningDaysStreak(trades),

            GoalType.LessonsLearned => CountLessonsLearned(startDate),

            GoalType.AveragePnL => trades.Count > 0
                ? trades.Sum(t => t.CalculatePnL() ?? 0) / trades.Count
                : 0,

            GoalType.Custom => goal.CurrentValue, // Manual tracking

            _ => goal.CurrentValue
        };
    }

    /// <summary>
    /// Calculate the longest streak of consecutive profitable trading days.
    /// </summary>
    private static int CalculateWinningDaysStreak(List<GexVisor.Core.OptionsLog> trades)
    {
        if (!trades.Any())
        {
            return 0;
        }

        // Group trades by date and calculate daily P&L
        var dailyPnL = trades
            .Where(t => t.CreatedDate.HasValue)
            .GroupBy(t => t.CreatedDate!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                PnL = g.Sum(t => t.CalculatePnL() ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        int maxStreak = 0;
        int currentStreak = 0;

        foreach (var day in dailyPnL)
        {
            if (day.PnL > 0)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak;
    }

    /// <summary>
    /// Count lessons learned since a start date.
    /// </summary>
    private int CountLessonsLearned(DateTime startDate)
    {
        return _analyticsService.GetRecentLessons(int.MaxValue)
            .Count(l => l.RecordedAt >= startDate);
    }
}
