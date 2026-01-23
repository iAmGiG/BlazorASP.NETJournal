using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Specialized cache wrapper for market data with domain-aware TTL.
/// Implements smart expiration: 24hr for recent data, 10yr for historical.
/// Ported from gex-llm-patterns Python smart TTL logic.
/// </summary>
public class MarketDataCacheService
{
    private readonly ICacheService _cache;
    private readonly ILogger<MarketDataCacheService>? _logger;

    // Domain-specific TTL rules
    private static readonly TimeSpan RecentDataTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan HistoricalDataTtl = TimeSpan.FromDays(3650); // 10 years
    private static readonly TimeSpan QuoteStaleThreshold = TimeSpan.FromMinutes(15);

    public MarketDataCacheService(ICacheService cache, ILogger<MarketDataCacheService>? logger = null)
    {
        _cache = cache;
        _logger = logger;
    }

    #region Quote Caching

    /// <summary>
    /// Get TTL for a quote based on its age.
    /// Recent quotes (< 2 days): 24hr TTL (needs refresh)
    /// Historical quotes: 10yr TTL (immutable)
    /// </summary>
    public static TimeSpan GetTtlForQuote(DateTime quoteTimestamp)
    {
        var age = DateTime.UtcNow - quoteTimestamp;
        return age.TotalDays > 2
            ? HistoricalDataTtl  // Historical: never expires
            : RecentDataTtl;     // Recent: refresh daily
    }

    /// <summary>
    /// Check if a cached quote is stale (older than 15 minutes during market hours).
    /// </summary>
    public static bool IsQuoteStale(Quote quote)
    {
        var age = DateTime.UtcNow - quote.Timestamp;
        return age > QuoteStaleThreshold;
    }

    /// <summary>Get cached quote for a symbol.</summary>
    public async Task<Quote?> GetQuoteAsync(string symbol)
        => await _cache.GetAsync<Quote>($"quote:{symbol.ToUpperInvariant()}");

    /// <summary>Cache a quote with smart TTL.</summary>
    public async Task SetQuoteAsync(Quote quote)
    {
        var ttl = GetTtlForQuote(quote.Timestamp);
        await _cache.SetAsync($"quote:{quote.Symbol.ToUpperInvariant()}", quote, ttl);
        _logger?.LogDebug("Cached quote for {Symbol} with TTL {Ttl}", quote.Symbol, ttl);
    }

    /// <summary>Get or fetch a quote with caching.</summary>
    public async Task<Quote> GetOrFetchQuoteAsync(string symbol, Func<Task<Quote>> fetcher)
    {
        var key = $"quote:{symbol.ToUpperInvariant()}";
        var cached = await _cache.GetAsync<Quote>(key);

        // Return cached if fresh enough
        if (cached != null && !IsQuoteStale(cached))
        {
            return cached;
        }

        // Fetch and cache
        var quote = await fetcher();
        var ttl = GetTtlForQuote(quote.Timestamp);
        await _cache.SetAsync(key, quote, ttl);
        return quote;
    }

    #endregion

    #region OHLCV Bars Caching

    /// <summary>
    /// Get TTL for OHLCV bars based on timeframe and most recent bar.
    /// </summary>
    public static TimeSpan GetTtlForBars(BarTimeframe timeframe, DateTime mostRecentBarTime)
    {
        var age = DateTime.UtcNow - mostRecentBarTime;

        // Historical data (older than 2 days): very long TTL
        if (age.TotalDays > 2)
        {
            return HistoricalDataTtl;
        }

        // Recent data TTL depends on timeframe
        return timeframe switch
        {
            BarTimeframe.Minute1 => TimeSpan.FromMinutes(5),
            BarTimeframe.Minute5 => TimeSpan.FromMinutes(15),
            BarTimeframe.Minute15 => TimeSpan.FromMinutes(30),
            BarTimeframe.Minute30 => TimeSpan.FromHours(1),
            BarTimeframe.Hour1 => TimeSpan.FromHours(2),
            BarTimeframe.Hour4 => TimeSpan.FromHours(6),
            BarTimeframe.Day => TimeSpan.FromHours(24),
            BarTimeframe.Week => TimeSpan.FromDays(2),
            BarTimeframe.Month => TimeSpan.FromDays(7),
            _ => RecentDataTtl
        };
    }

    /// <summary>Get cached OHLCV bars for a symbol and timeframe.</summary>
    public async Task<List<OhlcvBar>?> GetBarsAsync(string symbol, BarTimeframe timeframe)
        => await _cache.GetAsync<List<OhlcvBar>>($"bars:{symbol.ToUpperInvariant()}:{timeframe}");

    /// <summary>Cache OHLCV bars with smart TTL.</summary>
    public async Task SetBarsAsync(string symbol, BarTimeframe timeframe, List<OhlcvBar> bars)
    {
        if (bars.Count == 0)
        {
            return;
        }

        var mostRecent = bars.Max(b => b.Timestamp);
        var ttl = GetTtlForBars(timeframe, mostRecent);
        await _cache.SetAsync($"bars:{symbol.ToUpperInvariant()}:{timeframe}", bars, ttl);
        _logger?.LogDebug("Cached {Count} bars for {Symbol}/{Timeframe} with TTL {Ttl}",
            bars.Count, symbol, timeframe, ttl);
    }

    /// <summary>Get or fetch OHLCV bars with caching.</summary>
    public async Task<List<OhlcvBar>> GetOrFetchBarsAsync(
        string symbol,
        BarTimeframe timeframe,
        Func<Task<List<OhlcvBar>>> fetcher)
    {
        var key = $"bars:{symbol.ToUpperInvariant()}:{timeframe}";
        var cached = await _cache.GetAsync<List<OhlcvBar>>(key);

        if (cached != null && cached.Count > 0)
        {
            var mostRecent = cached.Max(b => b.Timestamp);
            var expectedTtl = GetTtlForBars(timeframe, mostRecent);

            // If data is still fresh for this timeframe, return cached
            if (DateTime.UtcNow - mostRecent < expectedTtl)
            {
                return cached;
            }
        }

        // Fetch and cache
        var bars = await fetcher();
        if (bars.Count > 0)
        {
            var mostRecent = bars.Max(b => b.Timestamp);
            var ttl = GetTtlForBars(timeframe, mostRecent);
            await _cache.SetAsync(key, bars, ttl);
        }
        return bars;
    }

    #endregion

    #region Multiple Quotes Caching

    /// <summary>Get cached quotes for multiple symbols.</summary>
    public async Task<Dictionary<string, Quote>> GetMultipleQuotesAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, Quote>();
        foreach (var symbol in symbols)
        {
            var quote = await GetQuoteAsync(symbol);
            if (quote != null)
            {
                result[symbol.ToUpperInvariant()] = quote;
            }
        }
        return result;
    }

    /// <summary>Cache multiple quotes.</summary>
    public async Task SetMultipleQuotesAsync(IEnumerable<Quote> quotes)
    {
        foreach (var quote in quotes)
        {
            await SetQuoteAsync(quote);
        }
    }

    #endregion

    #region Cache Management

    /// <summary>Invalidate all cached data for a symbol.</summary>
    public async Task<int> InvalidateSymbolAsync(string symbol)
        => await _cache.InvalidateAsync($"*:{symbol.ToUpperInvariant()}*");

    /// <summary>Invalidate all quotes.</summary>
    public async Task<int> InvalidateAllQuotesAsync()
        => await _cache.InvalidateAsync("quote:*");

    /// <summary>Invalidate all bars.</summary>
    public async Task<int> InvalidateAllBarsAsync()
        => await _cache.InvalidateAsync("bars:*");

    /// <summary>Get cache statistics.</summary>
    public Task<CacheStats> GetStatsAsync()
        => _cache.GetStatsAsync();

    /// <summary>Clean up expired entries.</summary>
    public Task<int> CleanupAsync()
        => _cache.CleanupExpiredAsync();

    #endregion
}
