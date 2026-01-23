using GexVisor.Core;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for fetching OHLCV price data for chart visualization.
/// </summary>
public interface IPriceDataService
{
    /// <summary>
    /// Get candlestick bars for a symbol.
    /// </summary>
    /// <param name="symbol">Stock symbol (e.g., SPY)</param>
    /// <param name="timeframe">Bar timeframe (1m, 5m, 15m, 30m, 1h, 4h, 1d, 1w, 1mo)</param>
    /// <param name="limit">Number of bars to fetch (default 100)</param>
    Task<PriceDataResult> GetCandlesAsync(string symbol, string timeframe = "1d", int limit = 100);

    /// <summary>
    /// Get candlestick bars around a trade's entry/exit dates.
    /// </summary>
    /// <param name="symbol">Stock symbol</param>
    /// <param name="entryDate">Trade entry date</param>
    /// <param name="exitDate">Trade exit date (null for open trades)</param>
    /// <param name="paddingDays">Days to include before entry and after exit</param>
    /// <param name="timeframe">Bar timeframe</param>
    Task<PriceDataResult> GetCandlesForTradeAsync(
        string symbol,
        DateTime entryDate,
        DateTime? exitDate,
        int paddingDays = 5,
        string timeframe = "1d");
}

/// <summary>
/// Result wrapper for price data operations.
/// </summary>
public record PriceDataResult
{
    public bool Success { get; init; }
    public List<OhlcvBar> Bars { get; init; } = new();
    public string? Error { get; init; }

    public static PriceDataResult Ok(List<OhlcvBar> bars) =>
        new() { Success = true, Bars = bars };

    public static PriceDataResult Fail(string error) =>
        new() { Success = false, Error = error };
}
