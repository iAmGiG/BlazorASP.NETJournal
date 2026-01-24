using System.Net.Http.Json;
using GexVisor.Core;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for fetching OHLCV price data from the API for chart visualization.
/// </summary>
public class PriceDataService : IPriceDataService
{
    private readonly HttpClient _httpClient;

    // Cache to avoid redundant API calls within a session
    private readonly Dictionary<string, (DateTime Fetched, List<OhlcvBar> Bars)> _cache = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public PriceDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PriceDataResult> GetCandlesAsync(string symbol, string timeframe = "1d", int limit = 100)
    {
        var cacheKey = $"{symbol.ToUpperInvariant()}:{timeframe}:{limit}";

        // Check local cache
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.Fetched < _cacheDuration)
        {
            return PriceDataResult.Ok(cached.Bars);
        }

        try
        {
            var url = $"api/market/bars/{symbol.ToUpperInvariant()}?timeframe={timeframe}&limit={limit}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return PriceDataResult.Fail($"API error: {response.StatusCode} - {errorContent}");
            }

            var bars = await response.Content.ReadFromJsonAsync<List<OhlcvBar>>();

            if (bars == null || bars.Count == 0)
            {
                return PriceDataResult.Fail($"No data available for {symbol}");
            }

            // Sort by timestamp ascending for chart display
            bars = bars.OrderBy(b => b.Timestamp).ToList();

            // Cache the result
            _cache[cacheKey] = (DateTime.UtcNow, bars);

            return PriceDataResult.Ok(bars);
        }
        catch (HttpRequestException ex)
        {
            return PriceDataResult.Fail($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return PriceDataResult.Fail($"Error fetching price data: {ex.Message}");
        }
    }

    public async Task<PriceDataResult> GetCandlesForTradeAsync(
        string symbol,
        DateTime entryDate,
        DateTime? exitDate,
        int paddingDays = 5,
        string timeframe = "1d")
    {
        // Calculate the date range with padding
        var startDate = entryDate.AddDays(-paddingDays);
        var endDate = (exitDate ?? DateTime.Now).AddDays(paddingDays);

        // Calculate approximate number of trading days needed
        var totalDays = (endDate - startDate).TotalDays;
        var tradingDays = (int)(totalDays * 5 / 7) + paddingDays; // Approximate trading days
        var limit = Math.Max(tradingDays, 30); // Minimum 30 bars

        // Fetch the data
        var result = await GetCandlesAsync(symbol, timeframe, limit);

        if (!result.Success)
        {
            return result;
        }

        // Filter to the relevant date range
        var filteredBars = result.Bars
            .Where(b => b.Timestamp >= startDate && b.Timestamp <= endDate)
            .OrderBy(b => b.Timestamp)
            .ToList();

        if (filteredBars.Count == 0)
        {
            return PriceDataResult.Fail($"No data available for {symbol} in the specified date range");
        }

        return PriceDataResult.Ok(filteredBars);
    }

    /// <summary>
    /// Clear the local cache. Useful when refreshing data.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
