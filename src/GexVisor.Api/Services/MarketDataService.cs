using System.Net.Http.Headers;
using System.Text.Json;
using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for fetching real-time market data from multiple providers.
/// </summary>
public interface IMarketDataService
{
    Task<MarketDataResult<Quote>> GetQuoteAsync(string symbol);
    Task<MarketDataResult<List<OhlcvBar>>> GetBarsAsync(string symbol, BarTimeframe timeframe, int limit = 100);
    Task<MarketDataResult<List<Quote>>> GetMultipleQuotesAsync(IEnumerable<string> symbols);
}

public class MarketDataService : IMarketDataService
{
    private readonly IApiConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MarketDataService> _logger;
    private readonly MarketDataCacheService _cache;

    // Provider priority order for fallback
    private readonly List<MarketDataProvider> _providerOrder = new()
    {
        MarketDataProvider.Alpaca,
        MarketDataProvider.Finnhub,
        MarketDataProvider.Polygon
    };

    public MarketDataService(
        IApiConfigService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<MarketDataService> logger,
        MarketDataCacheService cache)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<MarketDataResult<Quote>> GetQuoteAsync(string symbol)
    {
        // Check cache first
        var cached = await _cache.GetQuoteAsync(symbol);
        if (cached != null && !MarketDataCacheService.IsQuoteStale(cached))
        {
            _logger.LogDebug("Cache hit for {Symbol}", symbol);
            return MarketDataResult<Quote>.Ok(cached, MarketDataProvider.Alpaca); // Source preserved in cached data
        }

        // Fetch from providers
        foreach (var provider in _providerOrder)
        {
            if (!IsProviderConfigured(provider)) continue;

            try
            {
                var result = provider switch
                {
                    MarketDataProvider.Alpaca => await GetAlpacaQuoteAsync(symbol),
                    MarketDataProvider.Finnhub => await GetFinnhubQuoteAsync(symbol),
                    _ => null
                };

                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogDebug("Got quote for {Symbol} from {Provider}", symbol, provider);
                    // Cache the result
                    await _cache.SetQuoteAsync(result.Data);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get quote from {Provider} for {Symbol}", provider, symbol);
            }
        }

        return MarketDataResult<Quote>.Fail($"Failed to get quote for {symbol} from all configured providers");
    }

    public async Task<MarketDataResult<List<Quote>>> GetMultipleQuotesAsync(IEnumerable<string> symbols)
    {
        var symbolList = symbols.ToList();
        var quotes = new List<Quote>();
        var missingSymbols = new List<string>();

        // Check cache for each symbol
        var cachedQuotes = await _cache.GetMultipleQuotesAsync(symbolList);
        foreach (var symbol in symbolList)
        {
            if (cachedQuotes.TryGetValue(symbol.ToUpperInvariant(), out var cached)
                && !MarketDataCacheService.IsQuoteStale(cached))
            {
                quotes.Add(cached);
            }
            else
            {
                missingSymbols.Add(symbol);
            }
        }

        // If all cached, return early
        if (missingSymbols.Count == 0 && quotes.Count > 0)
        {
            _logger.LogDebug("All {Count} quotes served from cache", quotes.Count);
            var source = quotes[0].Source;
            var provider = !string.IsNullOrEmpty(source) && Enum.TryParse<MarketDataProvider>(source, out var parsed)
                ? parsed
                : MarketDataProvider.Alpaca;
            return MarketDataResult<List<Quote>>.Ok(quotes, provider);
        }

        // Fetch missing symbols from batch API first (more efficient)
        if (missingSymbols.Count > 0 && IsProviderConfigured(MarketDataProvider.Alpaca))
        {
            try
            {
                var result = await GetAlpacaMultipleQuotesAsync(missingSymbols);
                if (result.Success && result.Data != null)
                {
                    // Cache all fetched quotes
                    await _cache.SetMultipleQuotesAsync(result.Data);
                    quotes.AddRange(result.Data);
                    missingSymbols.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch quote fetch from Alpaca failed, falling back to individual requests");
            }
        }

        // Fallback to individual requests for any remaining symbols
        foreach (var symbol in missingSymbols)
        {
            var result = await GetQuoteAsync(symbol); // This will also cache
            if (result.Success && result.Data != null)
                quotes.Add(result.Data);
        }

        if (quotes.Count == 0)
            return MarketDataResult<List<Quote>>.Fail("Failed to get any quotes");

        var finalSource = quotes[0].Source;
        var finalProvider = !string.IsNullOrEmpty(finalSource) && Enum.TryParse<MarketDataProvider>(finalSource, out var p)
            ? p
            : MarketDataProvider.Alpaca;
        return MarketDataResult<List<Quote>>.Ok(quotes, finalProvider);
    }

    public async Task<MarketDataResult<List<OhlcvBar>>> GetBarsAsync(string symbol, BarTimeframe timeframe, int limit = 100)
    {
        // Check cache first
        var cached = await _cache.GetBarsAsync(symbol, timeframe);
        if (cached != null && cached.Count >= limit)
        {
            _logger.LogDebug("Cache hit for {Symbol} bars ({Timeframe})", symbol, timeframe);
            return MarketDataResult<List<OhlcvBar>>.Ok(cached.Take(limit).ToList(), MarketDataProvider.Alpaca);
        }

        // Fetch from providers
        foreach (var provider in _providerOrder)
        {
            if (!IsProviderConfigured(provider)) continue;

            try
            {
                var result = provider switch
                {
                    MarketDataProvider.Alpaca => await GetAlpacaBarsAsync(symbol, timeframe, limit),
                    MarketDataProvider.Finnhub => await GetFinnhubBarsAsync(symbol, timeframe, limit),
                    _ => null
                };

                if (result?.Success == true && result.Data != null && result.Data.Count > 0)
                {
                    _logger.LogDebug("Got {Count} bars for {Symbol} from {Provider}",
                        result.Data.Count, symbol, provider);
                    // Cache the result
                    await _cache.SetBarsAsync(symbol, timeframe, result.Data);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get bars from {Provider} for {Symbol}", provider, symbol);
            }
        }

        return MarketDataResult<List<OhlcvBar>>.Fail($"Failed to get bars for {symbol} from all configured providers");
    }

    private bool IsProviderConfigured(MarketDataProvider provider) => provider switch
    {
        MarketDataProvider.Alpaca => _configService.Configuration.HasAlpaca,
        MarketDataProvider.Finnhub => _configService.Configuration.HasFinnhub,
        MarketDataProvider.Polygon => _configService.Configuration.HasPolygon,
        _ => false
    };

    #region Alpaca Implementation

    private async Task<MarketDataResult<Quote>?> GetAlpacaQuoteAsync(string symbol)
    {
        var config = _configService.Configuration;
        var client = _httpClientFactory.CreateClient();

        // Alpaca Market Data API v2
        var endpoint = config.AlpacaEndpoint?.Replace("/v2", "") ?? "https://data.alpaca.markets";
        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", config.AlpacaApiKey);
        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", config.AlpacaSecret);

        var response = await client.GetAsync($"{endpoint}/v2/stocks/{symbol}/quotes/latest");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("quote", out var quote))
            return null;

        var askPrice = quote.GetProperty("ap").GetDecimal();
        var bidPrice = quote.GetProperty("bp").GetDecimal();
        var midPrice = (askPrice + bidPrice) / 2;

        // Get previous close from snapshot for change calculation
        var snapshotResponse = await client.GetAsync($"{endpoint}/v2/stocks/{symbol}/snapshot");
        decimal prevClose = midPrice;
        decimal open = midPrice;
        decimal high = midPrice;
        decimal low = midPrice;
        long volume = 0;

        if (snapshotResponse.IsSuccessStatusCode)
        {
            var snapshotJson = await snapshotResponse.Content.ReadAsStringAsync();
            using var snapshotDoc = JsonDocument.Parse(snapshotJson);
            var snap = snapshotDoc.RootElement;

            if (snap.TryGetProperty("prevDailyBar", out var prevBar))
                prevClose = prevBar.GetProperty("c").GetDecimal();

            if (snap.TryGetProperty("dailyBar", out var dailyBar))
            {
                open = dailyBar.GetProperty("o").GetDecimal();
                high = dailyBar.GetProperty("h").GetDecimal();
                low = dailyBar.GetProperty("l").GetDecimal();
                volume = dailyBar.GetProperty("v").GetInt64();
            }
        }

        return MarketDataResult<Quote>.Ok(new Quote
        {
            Symbol = symbol,
            Price = midPrice,
            Change = midPrice - prevClose,
            ChangePercent = prevClose != 0 ? (midPrice - prevClose) / prevClose * 100 : 0,
            Open = open,
            High = high,
            Low = low,
            PreviousClose = prevClose,
            Volume = volume,
            Timestamp = DateTime.UtcNow,
            Source = "Alpaca"
        }, MarketDataProvider.Alpaca);
    }

    private async Task<MarketDataResult<List<Quote>>> GetAlpacaMultipleQuotesAsync(List<string> symbols)
    {
        var config = _configService.Configuration;
        var client = _httpClientFactory.CreateClient();

        var endpoint = config.AlpacaEndpoint?.Replace("/v2", "") ?? "https://data.alpaca.markets";
        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", config.AlpacaApiKey);
        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", config.AlpacaSecret);

        var symbolsParam = string.Join(",", symbols);
        var response = await client.GetAsync($"{endpoint}/v2/stocks/snapshots?symbols={symbolsParam}");

        if (!response.IsSuccessStatusCode)
            return MarketDataResult<List<Quote>>.Fail("Alpaca batch request failed");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var quotes = new List<Quote>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var symbol = prop.Name;
            var snap = prop.Value;

            var latestQuote = snap.GetProperty("latestQuote");
            var dailyBar = snap.GetProperty("dailyBar");
            var prevBar = snap.GetProperty("prevDailyBar");

            var askPrice = latestQuote.GetProperty("ap").GetDecimal();
            var bidPrice = latestQuote.GetProperty("bp").GetDecimal();
            var midPrice = (askPrice + bidPrice) / 2;
            var prevClose = prevBar.GetProperty("c").GetDecimal();

            quotes.Add(new Quote
            {
                Symbol = symbol,
                Price = midPrice,
                Change = midPrice - prevClose,
                ChangePercent = prevClose != 0 ? (midPrice - prevClose) / prevClose * 100 : 0,
                Open = dailyBar.GetProperty("o").GetDecimal(),
                High = dailyBar.GetProperty("h").GetDecimal(),
                Low = dailyBar.GetProperty("l").GetDecimal(),
                PreviousClose = prevClose,
                Volume = dailyBar.GetProperty("v").GetInt64(),
                Timestamp = DateTime.UtcNow,
                Source = "Alpaca"
            });
        }

        return MarketDataResult<List<Quote>>.Ok(quotes, MarketDataProvider.Alpaca);
    }

    private async Task<MarketDataResult<List<OhlcvBar>>?> GetAlpacaBarsAsync(string symbol, BarTimeframe timeframe, int limit)
    {
        var config = _configService.Configuration;
        var client = _httpClientFactory.CreateClient();

        var endpoint = config.AlpacaEndpoint?.Replace("/v2", "") ?? "https://data.alpaca.markets";
        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", config.AlpacaApiKey);
        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", config.AlpacaSecret);

        var tf = timeframe switch
        {
            BarTimeframe.Minute1 => "1Min",
            BarTimeframe.Minute5 => "5Min",
            BarTimeframe.Minute15 => "15Min",
            BarTimeframe.Minute30 => "30Min",
            BarTimeframe.Hour1 => "1Hour",
            BarTimeframe.Hour4 => "4Hour",
            BarTimeframe.Day => "1Day",
            BarTimeframe.Week => "1Week",
            BarTimeframe.Month => "1Month",
            _ => "1Day"
        };

        var response = await client.GetAsync($"{endpoint}/v2/stocks/{symbol}/bars?timeframe={tf}&limit={limit}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("bars", out var bars))
            return null;

        var ohlcvBars = new List<OhlcvBar>();
        foreach (var bar in bars.EnumerateArray())
        {
            ohlcvBars.Add(new OhlcvBar
            {
                Symbol = symbol,
                Timestamp = bar.GetProperty("t").GetDateTime(),
                Open = bar.GetProperty("o").GetDecimal(),
                High = bar.GetProperty("h").GetDecimal(),
                Low = bar.GetProperty("l").GetDecimal(),
                Close = bar.GetProperty("c").GetDecimal(),
                Volume = bar.GetProperty("v").GetInt64()
            });
        }

        return MarketDataResult<List<OhlcvBar>>.Ok(ohlcvBars, MarketDataProvider.Alpaca);
    }

    #endregion

    #region Finnhub Implementation

    private async Task<MarketDataResult<Quote>?> GetFinnhubQuoteAsync(string symbol)
    {
        var config = _configService.Configuration;
        var client = _httpClientFactory.CreateClient();

        var response = await client.GetAsync(
            $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={config.FinnhubKey}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Check if valid response (Finnhub returns 0 for all values if symbol not found)
        var price = root.GetProperty("c").GetDecimal();
        if (price == 0)
            return null;

        return MarketDataResult<Quote>.Ok(new Quote
        {
            Symbol = symbol,
            Price = price,
            Change = root.GetProperty("d").GetDecimal(),
            ChangePercent = root.GetProperty("dp").GetDecimal(),
            Open = root.GetProperty("o").GetDecimal(),
            High = root.GetProperty("h").GetDecimal(),
            Low = root.GetProperty("l").GetDecimal(),
            PreviousClose = root.GetProperty("pc").GetDecimal(),
            Volume = 0, // Finnhub quote doesn't include volume
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("t").GetInt64()).UtcDateTime,
            Source = "Finnhub"
        }, MarketDataProvider.Finnhub);
    }

    private async Task<MarketDataResult<List<OhlcvBar>>?> GetFinnhubBarsAsync(string symbol, BarTimeframe timeframe, int limit)
    {
        var config = _configService.Configuration;
        var client = _httpClientFactory.CreateClient();

        var resolution = timeframe switch
        {
            BarTimeframe.Minute1 => "1",
            BarTimeframe.Minute5 => "5",
            BarTimeframe.Minute15 => "15",
            BarTimeframe.Minute30 => "30",
            BarTimeframe.Hour1 => "60",
            BarTimeframe.Day => "D",
            BarTimeframe.Week => "W",
            BarTimeframe.Month => "M",
            _ => "D"
        };

        var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var from = to - GetSecondsForTimeframe(timeframe) * limit;

        var response = await client.GetAsync(
            $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution={resolution}&from={from}&to={to}&token={config.FinnhubKey}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("s", out var status) && status.GetString() == "no_data")
            return null;

        var timestamps = root.GetProperty("t").EnumerateArray().ToList();
        var opens = root.GetProperty("o").EnumerateArray().ToList();
        var highs = root.GetProperty("h").EnumerateArray().ToList();
        var lows = root.GetProperty("l").EnumerateArray().ToList();
        var closes = root.GetProperty("c").EnumerateArray().ToList();
        var volumes = root.GetProperty("v").EnumerateArray().ToList();

        var bars = new List<OhlcvBar>();
        for (int i = 0; i < timestamps.Count; i++)
        {
            bars.Add(new OhlcvBar
            {
                Symbol = symbol,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime,
                Open = opens[i].GetDecimal(),
                High = highs[i].GetDecimal(),
                Low = lows[i].GetDecimal(),
                Close = closes[i].GetDecimal(),
                Volume = volumes[i].GetInt64()
            });
        }

        return MarketDataResult<List<OhlcvBar>>.Ok(bars, MarketDataProvider.Finnhub);
    }

    private static long GetSecondsForTimeframe(BarTimeframe tf) => tf switch
    {
        BarTimeframe.Minute1 => 60,
        BarTimeframe.Minute5 => 300,
        BarTimeframe.Minute15 => 900,
        BarTimeframe.Minute30 => 1800,
        BarTimeframe.Hour1 => 3600,
        BarTimeframe.Hour4 => 14400,
        BarTimeframe.Day => 86400,
        BarTimeframe.Week => 604800,
        BarTimeframe.Month => 2592000,
        _ => 86400
    };

    #endregion
}
