namespace GexVisor.UI.Models;

/// <summary>
/// Emotional state for trade tracking and self-analysis.
/// </summary>
public enum EmotionalState
{
    /// <summary>Relaxed and clear-headed.</summary>
    Calm,

    /// <summary>Strong belief in trade thesis.</summary>
    Confident,

    /// <summary>Worried about potential loss.</summary>
    Anxious,

    /// <summary>Eager about opportunity.</summary>
    Excited,

    /// <summary>Scared of market movement.</summary>
    Fearful,

    /// <summary>Seeking excess gains.</summary>
    Greedy,

    /// <summary>Annoyed by market behavior.</summary>
    Frustrated,

    /// <summary>No strong emotional state.</summary>
    Neutral
}

/// <summary>
/// Time period for aggregating performance trends.
/// </summary>
public enum TrendPeriod
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Performance metrics calculated over a specific time period.
/// </summary>
public record PerformanceMetrics
{
    /// <summary>Start of the measurement period.</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>End of the measurement period.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>Total profit/loss for the period.</summary>
    public decimal TotalPnL { get; init; }

    /// <summary>Win rate as percentage (0-100).</summary>
    public decimal WinRate { get; init; }

    /// <summary>Total number of trades in period.</summary>
    public int TradeCount { get; init; }

    /// <summary>Number of profitable trades.</summary>
    public int WinningTrades { get; init; }

    /// <summary>Number of losing trades.</summary>
    public int LosingTrades { get; init; }

    /// <summary>Average P&L per trade.</summary>
    public decimal AveragePnL { get; init; }

    /// <summary>Best single trade P&L.</summary>
    public decimal BestTrade { get; init; }

    /// <summary>Worst single trade P&L.</summary>
    public decimal WorstTrade { get; init; }

    /// <summary>Profit factor (gross profit / gross loss). >1 is profitable.</summary>
    public decimal ProfitFactor { get; init; }

    /// <summary>Average trade hold duration in hours.</summary>
    public decimal AverageHoldDurationHours { get; init; }

    /// <summary>
    /// Creates empty metrics for a period with no trades.
    /// </summary>
    public static PerformanceMetrics Empty(DateOnly start, DateOnly end) => new()
    {
        StartDate = start,
        EndDate = end,
        TotalPnL = 0,
        WinRate = 0,
        TradeCount = 0,
        WinningTrades = 0,
        LosingTrades = 0,
        AveragePnL = 0,
        BestTrade = 0,
        WorstTrade = 0,
        ProfitFactor = 0,
        AverageHoldDurationHours = 0
    };
}

/// <summary>
/// Heatmap data point for time-of-day/day-of-week analysis.
/// </summary>
public record HeatmapCell
{
    /// <summary>Day of week (0 = Sunday, 6 = Saturday).</summary>
    public int DayOfWeek { get; init; }

    /// <summary>Hour of day (0-23).</summary>
    public int HourOfDay { get; init; }

    /// <summary>Average P&L for trades at this time.</summary>
    public decimal AveragePnL { get; init; }

    /// <summary>Number of trades at this time.</summary>
    public int TradeCount { get; init; }

    /// <summary>Win rate at this time (0-100).</summary>
    public decimal WinRate { get; init; }

    /// <summary>
    /// Intensity value for heatmap coloring (-1 to 1).
    /// Negative = losing, Positive = profitable.
    /// </summary>
    public decimal Intensity => TradeCount == 0 ? 0
        : Math.Clamp(AveragePnL / Math.Max(Math.Abs(AveragePnL), 100), -1, 1);
}

/// <summary>
/// Habit and pattern tracking metrics.
/// </summary>
public record HabitMetrics
{
    /// <summary>Average number of trades per trading day.</summary>
    public decimal AverageTradesPerDay { get; init; }

    /// <summary>Average number of trades per week.</summary>
    public decimal AverageTradesPerWeek { get; init; }

    /// <summary>Longest consecutive winning trade streak.</summary>
    public int LongestWinStreak { get; init; }

    /// <summary>Longest consecutive losing trade streak.</summary>
    public int LongestLossStreak { get; init; }

    /// <summary>Current streak count (positive = wins, negative = losses).</summary>
    public int CurrentStreak { get; init; }

    /// <summary>Whether current streak is winning (true) or losing (false).</summary>
    public bool IsCurrentStreakWinning { get; init; }

    /// <summary>Day of week with most trades.</summary>
    public DayOfWeek MostActiveDay { get; init; }

    /// <summary>Hour of day with most trades (0-23).</summary>
    public int MostActiveHour { get; init; }

    /// <summary>Most frequently used trading patterns/strategies.</summary>
    public List<string> TopPatterns { get; init; } = [];

    /// <summary>Total trading days analyzed.</summary>
    public int TotalTradingDays { get; init; }
}

/// <summary>
/// Analysis correlating emotional state with trading outcomes.
/// </summary>
public record EmotionalAnalysis
{
    /// <summary>The emotional state being analyzed.</summary>
    public EmotionalState State { get; init; }

    /// <summary>Number of trades with this emotional state.</summary>
    public int TradeCount { get; init; }

    /// <summary>Win rate for trades with this state (0-100).</summary>
    public decimal WinRate { get; init; }

    /// <summary>Average P&L for trades with this state.</summary>
    public decimal AveragePnL { get; init; }

    /// <summary>Total P&L for trades with this state.</summary>
    public decimal TotalPnL { get; init; }

    /// <summary>
    /// Performance indicator: positive = good outcomes, negative = poor outcomes.
    /// Based on comparison to overall average.
    /// </summary>
    public decimal PerformanceIndicator { get; init; }
}

/// <summary>
/// Analysis correlating confidence level with trading outcomes.
/// </summary>
public record ConfidenceAnalysis
{
    /// <summary>Confidence level (1-10 scale).</summary>
    public int ConfidenceLevel { get; init; }

    /// <summary>Number of trades at this confidence level.</summary>
    public int TradeCount { get; init; }

    /// <summary>Win rate for trades at this level (0-100).</summary>
    public decimal WinRate { get; init; }

    /// <summary>Average P&L for trades at this level.</summary>
    public decimal AveragePnL { get; init; }

    /// <summary>Total P&L for trades at this level.</summary>
    public decimal TotalPnL { get; init; }
}

/// <summary>
/// A lesson learned entry from trade reflection.
/// </summary>
public record LessonEntry
{
    /// <summary>When the lesson was recorded.</summary>
    public DateTime RecordedAt { get; init; }

    /// <summary>The lesson text.</summary>
    public required string Lesson { get; init; }

    /// <summary>P&L of the trade that prompted this lesson.</summary>
    public decimal? TradePnL { get; init; }

    /// <summary>Ticker of the associated trade.</summary>
    public string? Ticker { get; init; }

    /// <summary>Trade ID for linking back to the trade.</summary>
    public Guid? TradeId { get; init; }
}

/// <summary>
/// Extended trade tracking data for self-analysis.
/// Stored separately from OptionsLog to keep core model clean.
/// </summary>
public record TradeTrackingData
{
    /// <summary>Trade ID this tracking data belongs to.</summary>
    public required Guid TradeId { get; init; }

    /// <summary>Emotional state when entering the trade.</summary>
    public EmotionalState? Emotion { get; init; }

    /// <summary>Confidence level (1-10) when entering the trade.</summary>
    public int? ConfidenceLevel { get; init; }

    /// <summary>Post-trade reflection/lesson learned.</summary>
    public string? LessonLearned { get; init; }

    /// <summary>Custom tags for categorization.</summary>
    public List<string> TrackingTags { get; init; } = [];

    /// <summary>When this tracking data was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When this tracking data was last updated.</summary>
    public DateTime? UpdatedAt { get; init; }
}
