namespace GexVisor.Core;

/// <summary>
/// Real-time quote data for a symbol.
/// </summary>
public record Quote
{
    public required string Symbol { get; init; }
    public decimal Price { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePercent { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal PreviousClose { get; init; }
    public long Volume { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Source { get; init; }
}

/// <summary>
/// OHLCV bar data for charting.
/// </summary>
public record OhlcvBar
{
    public required string Symbol { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
}

/// <summary>
/// Time intervals for OHLCV data.
/// </summary>
public enum BarTimeframe
{
    Minute1,
    Minute5,
    Minute15,
    Minute30,
    Hour1,
    Hour4,
    Day,
    Week,
    Month
}

/// <summary>
/// Market data provider types.
/// </summary>
public enum MarketDataProvider
{
    Alpaca,
    Finnhub,
    Polygon,
    AlphaVantage
}

/// <summary>
/// Result wrapper for market data operations.
/// </summary>
public record MarketDataResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public MarketDataProvider? Source { get; init; }

    public static MarketDataResult<T> Ok(T data, MarketDataProvider source) =>
        new() { Success = true, Data = data, Source = source };

    public static MarketDataResult<T> Fail(string error) =>
        new() { Success = false, Error = error };
}
