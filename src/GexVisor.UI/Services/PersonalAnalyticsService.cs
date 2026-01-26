using System.Text.Json;
using GexVisor.Core;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for calculating personal trading analytics and metrics.
/// </summary>
public class PersonalAnalyticsService : IPersonalAnalyticsService
{
    private readonly TradeLogService _tradeLogService;
    private readonly ILocalStorageService _storage;
    private Dictionary<Guid, TradeTrackingData> _trackingData = [];

    public event Action? OnAnalyticsChanged;

    public PersonalAnalyticsService(TradeLogService tradeLogService, ILocalStorageService storage)
    {
        _tradeLogService = tradeLogService;
        _storage = storage;

        // Subscribe to trade changes to notify analytics consumers
        _tradeLogService.OnTradesChanged += () => OnAnalyticsChanged?.Invoke();
    }

    public async Task LoadTrackingDataAsync()
    {
        try
        {
            var stored = await _storage.GetAsync<List<TradeTrackingData>>(AppConstants.Storage.TradeTrackingData);
            _trackingData = stored?.ToDictionary(t => t.TradeId) ?? [];
        }
        catch (JsonException)
        {
            _trackingData = [];
        }
    }

    public TradeTrackingData? GetTrackingData(Guid tradeId)
    {
        return _trackingData.TryGetValue(tradeId, out var data) ? data : null;
    }

    public async Task SaveTrackingDataAsync(TradeTrackingData data)
    {
        _trackingData[data.TradeId] = data with { UpdatedAt = DateTime.UtcNow };
        await _storage.SetAsync(AppConstants.Storage.TradeTrackingData, _trackingData.Values.ToList());
        OnAnalyticsChanged?.Invoke();
    }

    public PerformanceMetrics CalculatePerformance(DateOnly start, DateOnly end)
    {
        var trades = GetClosedTradesInRange(start, end);

        if (!trades.Any())
        {
            return PerformanceMetrics.Empty(start, end);
        }

        var pnlValues = trades.Select(t => t.CalculatePnL() ?? 0).ToList();
        var winningTrades = pnlValues.Where(p => p > 0).ToList();
        var losingTrades = pnlValues.Where(p => p < 0).ToList();

        var grossProfit = winningTrades.Sum();
        var grossLoss = Math.Abs(losingTrades.Sum());

        return new PerformanceMetrics
        {
            StartDate = start,
            EndDate = end,
            TotalPnL = pnlValues.Sum(),
            WinRate = trades.Count > 0 ? (decimal)winningTrades.Count / trades.Count * 100 : 0,
            TradeCount = trades.Count,
            WinningTrades = winningTrades.Count,
            LosingTrades = losingTrades.Count,
            AveragePnL = trades.Count > 0 ? pnlValues.Sum() / trades.Count : 0,
            BestTrade = pnlValues.Any() ? pnlValues.Max() : 0,
            WorstTrade = pnlValues.Any() ? pnlValues.Min() : 0,
            ProfitFactor = grossLoss > 0 ? grossProfit / grossLoss : grossProfit > 0 ? 999 : 0,
            AverageHoldDurationHours = CalculateAverageHoldDuration(trades)
        };
    }

    public List<PerformanceMetrics> GetPerformanceTrend(DateOnly start, DateOnly end, TrendPeriod period)
    {
        var result = new List<PerformanceMetrics>();
        var current = start;

        while (current <= end)
        {
            var periodEnd = period switch
            {
                TrendPeriod.Daily => current,
                TrendPeriod.Weekly => current.AddDays(6),
                TrendPeriod.Monthly => current.AddMonths(1).AddDays(-1),
                _ => current
            };

            if (periodEnd > end)
            {
                periodEnd = end;
            }

            result.Add(CalculatePerformance(current, periodEnd));

            current = period switch
            {
                TrendPeriod.Daily => current.AddDays(1),
                TrendPeriod.Weekly => current.AddDays(7),
                TrendPeriod.Monthly => current.AddMonths(1),
                _ => current.AddDays(1)
            };
        }

        return result;
    }

    public List<HeatmapCell> GenerateHeatmap(int weeksToAnalyze = 12)
    {
        var cutoff = DateTime.UtcNow.AddDays(-weeksToAnalyze * 7);
        var trades = _tradeLogService.GetClosedTrades()
            .Where(t => t.CreatedDate >= cutoff)
            .ToList();

        // Group by day of week and hour
        var grouped = trades
            .Where(t => t.CreatedDate.HasValue)
            .GroupBy(t => new
            {
                DayOfWeek = (int)t.CreatedDate!.Value.DayOfWeek,
                Hour = t.CreatedDate!.Value.Hour
            });

        var cells = new List<HeatmapCell>();

        foreach (var group in grouped)
        {
            var pnlValues = group.Select(t => t.CalculatePnL() ?? 0).ToList();
            var wins = pnlValues.Count(p => p > 0);

            cells.Add(new HeatmapCell
            {
                DayOfWeek = group.Key.DayOfWeek,
                HourOfDay = group.Key.Hour,
                AveragePnL = pnlValues.Average(),
                TradeCount = group.Count(),
                WinRate = pnlValues.Count > 0 ? (decimal)wins / pnlValues.Count * 100 : 0
            });
        }

        return cells;
    }

    public HabitMetrics CalculateHabitMetrics()
    {
        var allTrades = _tradeLogService.GetClosedTrades().ToList();

        if (!allTrades.Any())
        {
            return new HabitMetrics();
        }

        // Calculate streaks
        var orderedTrades = allTrades
            .Where(t => t.CreatedDate.HasValue)
            .OrderBy(t => t.CreatedDate)
            .ToList();

        var (longestWin, longestLoss, currentStreak, isWinning) = CalculateStreaks(orderedTrades);

        // Calculate trading frequency
        var tradingDays = orderedTrades
            .Select(t => t.CreatedDate!.Value.Date)
            .Distinct()
            .ToList();

        var dayOfWeekCounts = orderedTrades
            .GroupBy(t => t.CreatedDate!.Value.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var hourCounts = orderedTrades
            .GroupBy(t => t.CreatedDate!.Value.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        // Get top patterns from tags
        var topPatterns = GetTopPatterns(5).Select(p => p.Pattern).ToList();

        var totalDays = tradingDays.Any()
            ? (tradingDays.Max() - tradingDays.Min()).Days + 1
            : 1;

        var weeks = Math.Max(1, totalDays / 7.0m);

        return new HabitMetrics
        {
            AverageTradesPerDay = tradingDays.Count > 0 ? (decimal)allTrades.Count / tradingDays.Count : 0,
            AverageTradesPerWeek = allTrades.Count / weeks,
            LongestWinStreak = longestWin,
            LongestLossStreak = longestLoss,
            CurrentStreak = Math.Abs(currentStreak),
            IsCurrentStreakWinning = isWinning,
            MostActiveDay = dayOfWeekCounts?.Key ?? DayOfWeek.Monday,
            MostActiveHour = hourCounts?.Key ?? 10,
            TopPatterns = topPatterns,
            TotalTradingDays = tradingDays.Count
        };
    }

    public List<EmotionalAnalysis> AnalyzeEmotionalCorrelation()
    {
        var tradesWithEmotion = _tradeLogService.GetClosedTrades()
            .Select(t => new { Trade = t, Tracking = GetTrackingData(t.Id) })
            .Where(x => x.Tracking?.Emotion != null)
            .ToList();

        if (!tradesWithEmotion.Any())
        {
            return [];
        }

        var overallAvgPnL = tradesWithEmotion.Average(x => x.Trade.CalculatePnL() ?? 0);

        return tradesWithEmotion
            .GroupBy(x => x.Tracking!.Emotion!.Value)
            .Select(g =>
            {
                var pnlValues = g.Select(x => x.Trade.CalculatePnL() ?? 0).ToList();
                var avgPnL = pnlValues.Average();
                var wins = pnlValues.Count(p => p > 0);

                return new EmotionalAnalysis
                {
                    State = g.Key,
                    TradeCount = g.Count(),
                    WinRate = pnlValues.Count > 0 ? (decimal)wins / pnlValues.Count * 100 : 0,
                    AveragePnL = avgPnL,
                    TotalPnL = pnlValues.Sum(),
                    PerformanceIndicator = overallAvgPnL != 0
                        ? (avgPnL - overallAvgPnL) / Math.Abs(overallAvgPnL)
                        : 0
                };
            })
            .OrderByDescending(e => e.WinRate)
            .ToList();
    }

    public List<ConfidenceAnalysis> AnalyzeConfidenceCorrelation()
    {
        var tradesWithConfidence = _tradeLogService.GetClosedTrades()
            .Select(t => new { Trade = t, Tracking = GetTrackingData(t.Id) })
            .Where(x => x.Tracking?.ConfidenceLevel != null)
            .ToList();

        if (!tradesWithConfidence.Any())
        {
            return [];
        }

        return tradesWithConfidence
            .GroupBy(x => x.Tracking!.ConfidenceLevel!.Value)
            .Select(g =>
            {
                var pnlValues = g.Select(x => x.Trade.CalculatePnL() ?? 0).ToList();
                var wins = pnlValues.Count(p => p > 0);

                return new ConfidenceAnalysis
                {
                    ConfidenceLevel = g.Key,
                    TradeCount = g.Count(),
                    WinRate = pnlValues.Count > 0 ? (decimal)wins / pnlValues.Count * 100 : 0,
                    AveragePnL = pnlValues.Average(),
                    TotalPnL = pnlValues.Sum()
                };
            })
            .OrderBy(c => c.ConfidenceLevel)
            .ToList();
    }

    public List<LessonEntry> GetRecentLessons(int count = 10)
    {
        return _trackingData.Values
            .Where(t => !string.IsNullOrWhiteSpace(t.LessonLearned))
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .Select(t =>
            {
                var trade = _tradeLogService.GetById(t.TradeId);
                return new LessonEntry
                {
                    RecordedAt = t.CreatedAt,
                    Lesson = t.LessonLearned!,
                    TradePnL = trade?.CalculatePnL(),
                    Ticker = trade?.Ticker,
                    TradeId = t.TradeId
                };
            })
            .ToList();
    }

    public List<(string Pattern, decimal WinRate, int Count, decimal TotalPnL)> GetTopPatterns(int count = 5)
    {
        var tradesWithTags = _tradeLogService.GetClosedTrades()
            .Select(t => new { Trade = t, Tracking = GetTrackingData(t.Id) })
            .Where(x => x.Tracking?.TrackingTags.Any() == true)
            .ToList();

        if (!tradesWithTags.Any())
        {
            return [];
        }

        // Flatten tags and group
        return tradesWithTags
            .SelectMany(x => x.Tracking!.TrackingTags.Select(tag => new { Tag = tag, x.Trade }))
            .GroupBy(x => x.Tag)
            .Where(g => g.Count() >= 2) // Need at least 2 trades to be meaningful
            .Select(g =>
            {
                var pnlValues = g.Select(x => x.Trade.CalculatePnL() ?? 0).ToList();
                var wins = pnlValues.Count(p => p > 0);
                var winRate = pnlValues.Count > 0 ? (decimal)wins / pnlValues.Count * 100 : 0;
                return (Pattern: g.Key, WinRate: winRate, Count: g.Count(), TotalPnL: pnlValues.Sum());
            })
            .OrderByDescending(x => x.WinRate)
            .ThenByDescending(x => x.Count)
            .Take(count)
            .ToList();
    }

    // === Helper Methods ===

    private List<OptionsLog> GetClosedTradesInRange(DateOnly start, DateOnly end)
    {
        var startDate = start.ToDateTime(TimeOnly.MinValue);
        var endDate = end.ToDateTime(TimeOnly.MaxValue);

        return _tradeLogService.GetClosedTrades()
            .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
            .ToList();
    }

    private static decimal CalculateAverageHoldDuration(List<OptionsLog> _)
    {
        // For options, we could calculate from creation to exit if we had exit dates
        // For now, return 0 as we don't track exit timestamps separately
        return 0;
    }

    private static (int LongestWin, int LongestLoss, int CurrentStreak, bool IsWinning) CalculateStreaks(
        List<OptionsLog> orderedTrades)
    {
        if (!orderedTrades.Any())
        {
            return (0, 0, 0, true);
        }

        int longestWin = 0, longestLoss = 0;
        int currentWin = 0, currentLoss = 0;

        foreach (var trade in orderedTrades)
        {
            var pnl = trade.CalculatePnL() ?? 0;

            if (pnl > 0)
            {
                currentWin++;
                currentLoss = 0;
                longestWin = Math.Max(longestWin, currentWin);
            }
            else if (pnl < 0)
            {
                currentLoss++;
                currentWin = 0;
                longestLoss = Math.Max(longestLoss, currentLoss);
            }
            // Breakeven trades don't affect streaks
        }

        var isWinning = currentWin > 0;
        var currentStreak = isWinning ? currentWin : currentLoss;

        return (longestWin, longestLoss, currentStreak, isWinning);
    }
}
