namespace GexVisor.UI.Models;

/// <summary>
/// Represents a backtest result for tracking strategy performance.
/// </summary>
public record BacktestResult : BaseEntry
{
    /// <summary>Name of the strategy being tested.</summary>
    public required string StrategyName { get; init; }

    /// <summary>Description of the strategy or test parameters.</summary>
    public string? Description { get; init; }

    /// <summary>Start date of the test period (YYYY-MM-DD).</summary>
    public required string StartDate { get; init; }

    /// <summary>End date of the test period (YYYY-MM-DD).</summary>
    public required string EndDate { get; init; }

    /// <summary>Entry rules or conditions for the strategy.</summary>
    public string? EntryRules { get; init; }

    /// <summary>Exit rules or conditions for the strategy.</summary>
    public string? ExitRules { get; init; }

    // Performance Metrics
    /// <summary>Total number of trades executed.</summary>
    public int TotalTrades { get; init; }

    /// <summary>Number of winning trades.</summary>
    public int WinningTrades { get; init; }

    /// <summary>Win rate as a percentage.</summary>
    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100 : 0;

    /// <summary>Total return as a percentage.</summary>
    public decimal TotalReturn { get; init; }

    /// <summary>Maximum drawdown as a percentage.</summary>
    public decimal MaxDrawdown { get; init; }

    /// <summary>Sharpe ratio (risk-adjusted return).</summary>
    public decimal? SharpeRatio { get; init; }

    /// <summary>Profit factor (gross profit / gross loss).</summary>
    public decimal? ProfitFactor { get; init; }

    /// <summary>Average trade return as a percentage.</summary>
    public decimal? AvgTradeReturn { get; init; }

    // Regime-Conditional Metrics
    /// <summary>Return during positive gamma periods.</summary>
    public decimal? ReturnInPositiveGamma { get; init; }

    /// <summary>Return during negative gamma periods.</summary>
    public decimal? ReturnInNegativeGamma { get; init; }

    /// <summary>Number of trades in positive gamma.</summary>
    public int? TradesInPositiveGamma { get; init; }

    /// <summary>Number of trades in negative gamma.</summary>
    public int? TradesInNegativeGamma { get; init; }

    // Notes
    /// <summary>Additional notes about the backtest.</summary>
    public string? Notes { get; init; }

    /// <summary>Whether this result is selected for comparison.</summary>
    public bool IsSelected { get; init; }
}

/// <summary>
/// Comparison result for multiple backtest strategies.
/// </summary>
public record BacktestComparison
{
    public required List<BacktestResult> Results { get; init; }

    public decimal AvgWinRate => Results.Count > 0 ? Results.Average(r => r.WinRate) : 0;
    public decimal AvgReturn => Results.Count > 0 ? Results.Average(r => r.TotalReturn) : 0;
    public decimal BestReturn => Results.Count > 0 ? Results.Max(r => r.TotalReturn) : 0;
    public decimal WorstReturn => Results.Count > 0 ? Results.Min(r => r.TotalReturn) : 0;

    public decimal AvgPositiveGammaReturn => Results.Count > 0
        ? Results.Where(r => r.ReturnInPositiveGamma.HasValue).Average(r => r.ReturnInPositiveGamma!.Value)
        : 0;

    public decimal AvgNegativeGammaReturn => Results.Count > 0
        ? Results.Where(r => r.ReturnInNegativeGamma.HasValue).Average(r => r.ReturnInNegativeGamma!.Value)
        : 0;
}
