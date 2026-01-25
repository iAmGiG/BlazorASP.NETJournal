using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for calculating personal trading analytics and metrics.
/// Analyzes trade history to provide insights on performance, habits, and patterns.
/// </summary>
public interface IPersonalAnalyticsService
{
    /// <summary>
    /// Event fired when analytics data changes (e.g., after trade updates).
    /// </summary>
    event Action? OnAnalyticsChanged;

    /// <summary>
    /// Calculate performance metrics for a specific date range.
    /// </summary>
    /// <param name="start">Start date (inclusive).</param>
    /// <param name="end">End date (inclusive).</param>
    /// <returns>Performance metrics for the period.</returns>
    PerformanceMetrics CalculatePerformance(DateOnly start, DateOnly end);

    /// <summary>
    /// Get performance trend data broken down by period.
    /// </summary>
    /// <param name="start">Start date for the trend analysis.</param>
    /// <param name="end">End date for the trend analysis.</param>
    /// <param name="period">Granularity of the trend (daily, weekly, monthly).</param>
    /// <returns>List of metrics for each period.</returns>
    List<PerformanceMetrics> GetPerformanceTrend(DateOnly start, DateOnly end, TrendPeriod period);

    /// <summary>
    /// Generate time-of-day/day-of-week heatmap data.
    /// </summary>
    /// <param name="weeksToAnalyze">Number of weeks to include (default 12).</param>
    /// <returns>Heatmap cells for each hour/day combination with trades.</returns>
    List<HeatmapCell> GenerateHeatmap(int weeksToAnalyze = 12);

    /// <summary>
    /// Calculate habit and pattern metrics.
    /// </summary>
    /// <returns>Habit metrics including streaks, frequency, patterns.</returns>
    HabitMetrics CalculateHabitMetrics();

    /// <summary>
    /// Analyze trading outcomes by emotional state.
    /// </summary>
    /// <returns>Analysis for each emotional state that has trades.</returns>
    List<EmotionalAnalysis> AnalyzeEmotionalCorrelation();

    /// <summary>
    /// Analyze trading outcomes by confidence level.
    /// </summary>
    /// <returns>Analysis for each confidence level (1-10) that has trades.</returns>
    List<ConfidenceAnalysis> AnalyzeConfidenceCorrelation();

    /// <summary>
    /// Get recent lessons learned from trade reflections.
    /// </summary>
    /// <param name="count">Maximum number of lessons to return.</param>
    /// <returns>Recent lesson entries ordered by date descending.</returns>
    List<LessonEntry> GetRecentLessons(int count = 10);

    /// <summary>
    /// Get top performing patterns/strategies.
    /// </summary>
    /// <param name="count">Maximum number of patterns to return.</param>
    /// <returns>Patterns with win rate and trade count, ordered by performance.</returns>
    List<(string Pattern, decimal WinRate, int Count, decimal TotalPnL)> GetTopPatterns(int count = 5);

    /// <summary>
    /// Get or set tracking data for a specific trade.
    /// </summary>
    /// <param name="tradeId">The trade ID.</param>
    /// <returns>Tracking data if exists, null otherwise.</returns>
    TradeTrackingData? GetTrackingData(Guid tradeId);

    /// <summary>
    /// Save tracking data for a trade.
    /// </summary>
    /// <param name="data">The tracking data to save.</param>
    Task SaveTrackingDataAsync(TradeTrackingData data);

    /// <summary>
    /// Load all tracking data from storage.
    /// </summary>
    Task LoadTrackingDataAsync();
}
