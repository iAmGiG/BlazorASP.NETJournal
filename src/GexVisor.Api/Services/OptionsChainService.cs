using System.Text.Json;
using System.Text.Json.Serialization;
using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for fetching options chain data from market data providers with validation and caching.
/// </summary>
public class OptionsChainService : IOptionsChainService
{
    private readonly IApiConfigService _config;
    private readonly OptionsChainCacheService _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OptionsChainService> _logger;

    // Rate limiting (O(1) with queue)
    private readonly Queue<DateTime> _callTimestamps = new();
    private readonly SemaphoreSlim _rateLimitLock = new(1, 1);
    private readonly int _callsPerMinute = 75; // Default standard tier

    // Provider fallback order
    private readonly List<MarketDataProvider> _providerOrder = new()
    {
        MarketDataProvider.AlphaVantage,
        MarketDataProvider.Polygon
    };

    public OptionsChainService(
        IApiConfigService config,
        OptionsChainCacheService cache,
        IHttpClientFactory httpClientFactory,
        ILogger<OptionsChainService> logger)
    {
        _config = config;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Detect premium tier for higher rate limits
        if (!string.IsNullOrEmpty(_config.Configuration.AlphaVantageKey) &&
            _config.Configuration.AlphaVantageKey.Contains("PREMO", StringComparison.OrdinalIgnoreCase))
        {
            _callsPerMinute = 1000;
            _logger.LogInformation("Alpha Vantage Premium tier detected (1000 calls/min)");
        }
        else
        {
            _logger.LogInformation("Alpha Vantage Standard tier (75 calls/min)");
        }
    }

    public async Task<OptionsChainResult<OptionsChain>> GetChainAsync(
        string symbol,
        DateTime? expirationDate = null)
    {
        // 1. Check cache first
        var cached = await _cache.GetChainAsync(symbol, expirationDate);
        if (cached != null && !IsStale(cached))
        {
            _logger.LogDebug("Cache hit for {Symbol} options chain", symbol);
            return OptionsChainResult<OptionsChain>.Ok(cached, cached.Source ?? MarketDataProvider.AlphaVantage);
        }

        // 2. Try providers in order
        foreach (var provider in _providerOrder)
        {
            try
            {
                var result = provider switch
                {
                    MarketDataProvider.AlphaVantage => await GetAlphaVantageChainAsync(symbol, expirationDate),
                    MarketDataProvider.Polygon => await GetPolygonChainAsync(symbol, expirationDate),
                    _ => null
                };

                if (result?.Success == true && result.Data != null)
                {
                    // Validate and cache
                    var validated = ValidateAndEnrichChain(result.Data);
                    await _cache.SetChainAsync(validated, expirationDate);
                    return OptionsChainResult<OptionsChain>.Ok(validated, provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch options chain from {Provider}", provider);
            }
        }

        return OptionsChainResult<OptionsChain>.Fail(
            $"Failed to fetch options chain for {symbol} from all providers");
    }

    public async Task<OptionsChainResult<OptionContract>> GetContractAsync(
        string symbol,
        decimal strikePrice,
        OptionType type,
        DateTime expirationDate)
    {
        // Check cache first
        var cached = await _cache.GetContractAsync(symbol, strikePrice, type, expirationDate);
        if (cached != null)
        {
            return OptionsChainResult<OptionContract>.Ok(cached, cached.Source ?? MarketDataProvider.AlphaVantage);
        }

        // Fetch full chain and filter
        var chainResult = await GetChainAsync(symbol, expirationDate);
        if (!chainResult.Success || chainResult.Data == null)
        {
            return OptionsChainResult<OptionContract>.Fail(chainResult.Error ?? "Failed to fetch chain");
        }

        var contract = chainResult.Data.Contracts
            .FirstOrDefault(c => c.StrikePrice == strikePrice && c.Type == type);

        if (contract == null)
        {
            return OptionsChainResult<OptionContract>.Fail(
                $"Contract not found: {symbol} {strikePrice} {type} {expirationDate:yyyy-MM-dd}");
        }

        // Cache individual contract
        await _cache.SetContractAsync(contract);

        return OptionsChainResult<OptionContract>.Ok(contract, chainResult.Source ?? MarketDataProvider.AlphaVantage);
    }

    public async Task<OptionsChainResult<List<DateTime>>> GetExpirationDatesAsync(string symbol)
    {
        // Fetch full chain (all expirations)
        var chainResult = await GetChainAsync(symbol, null);

        if (!chainResult.Success || chainResult.Data == null)
        {
            return OptionsChainResult<List<DateTime>>.Fail(
                chainResult.Error ?? "Failed to fetch chain");
        }

        return OptionsChainResult<List<DateTime>>.Ok(
            chainResult.Data.ExpirationDates,
            chainResult.Source ?? MarketDataProvider.AlphaVantage);
    }

    private async Task<OptionsChainResult<OptionsChain>?> GetAlphaVantageChainAsync(
        string symbol,
        DateTime? expirationDate)
    {
        if (string.IsNullOrEmpty(_config.Configuration.AlphaVantageKey))
        {
            _logger.LogDebug("Alpha Vantage API key not configured");
            return null;
        }

        // Rate limit check
        if (!await CheckRateLimitAsync())
        {
            _logger.LogWarning("Alpha Vantage rate limit exceeded");
            return OptionsChainResult<OptionsChain>.Fail("Rate limit exceeded");
        }

        var client = _httpClientFactory.CreateClient();

        // Use previous trading day if no expiration specified
        var date = expirationDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ??
                   DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var url = "https://www.alphavantage.co/query" +
                  $"?function=HISTORICAL_OPTIONS" +
                  $"&symbol={symbol}" +
                  $"&date={date}" +
                  $"&apikey={_config.Configuration.AlphaVantageKey}";

        _logger.LogDebug("Fetching options chain from Alpha Vantage: {Symbol} {Date}", symbol, date);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // Check for API error messages
        if (json.Contains("\"Error Message\"") || json.Contains("\"Note\""))
        {
            _logger.LogWarning("Alpha Vantage API error: {Response}", json.Substring(0, Math.Min(200, json.Length)));
            return OptionsChainResult<OptionsChain>.Fail("API error or rate limit");
        }

        var parsed = JsonSerializer.Deserialize<AlphaVantageOptionsResponse>(json);

        if (parsed?.Data == null || parsed.Data.Count == 0)
        {
            _logger.LogWarning("No data returned from Alpha Vantage for {Symbol}", symbol);
            return OptionsChainResult<OptionsChain>.Fail("No data returned from Alpha Vantage");
        }

        var contracts = parsed.Data.Select(ParseAlphaVantageContract).ToList();
        var chain = new OptionsChain
        {
            Symbol = symbol,
            ExpirationDate = expirationDate,
            Contracts = contracts,
            Timestamp = DateTime.UtcNow,
            Source = MarketDataProvider.AlphaVantage
        };

        _logger.LogInformation("Fetched {Count} contracts from Alpha Vantage for {Symbol}",
            contracts.Count, symbol);

        return OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage);
    }

    private async Task<OptionsChainResult<OptionsChain>?> GetPolygonChainAsync(
        string symbol,
        DateTime? expirationDate)
    {
        // TODO: Implement Polygon.io integration
        _logger.LogDebug("Polygon provider not yet implemented");
        await Task.CompletedTask;
        return null;
    }

    private OptionContract ParseAlphaVantageContract(AlphaVantageOptionData data)
    {
        // Parse option type (normalize "call"/"c"/"Call" -> Call)
        var typeStr = data.Type?.ToLower()?.Trim() ?? "call";
        var type = typeStr.StartsWith("c") ? OptionType.Call : OptionType.Put;

        // Calculate derived fields
        decimal? midPrice = null;
        decimal? bidAskSpread = null;
        if (data.Bid.HasValue && data.Ask.HasValue)
        {
            midPrice = (data.Bid.Value + data.Ask.Value) / 2m;
            bidAskSpread = data.Ask.Value - data.Bid.Value;
        }

        // Generate contract symbol if not provided
        var contractSymbol = data.ContractID;
        if (string.IsNullOrEmpty(contractSymbol))
        {
            var exp = DateTime.Parse(data.Expiration, System.Globalization.CultureInfo.InvariantCulture);
            contractSymbol = $"{data.Symbol}{exp:yyMMdd}{(type == OptionType.Call ? 'C' : 'P')}{data.Strike:00000000}";
        }

        return new OptionContract
        {
            Symbol = data.Symbol,
            ContractSymbol = contractSymbol,
            StrikePrice = data.Strike,
            Type = type,
            ExpirationDate = DateTime.Parse(data.Expiration, System.Globalization.CultureInfo.InvariantCulture),
            TradingDate = DateTime.Parse(data.Date, System.Globalization.CultureInfo.InvariantCulture),
            Bid = data.Bid,
            Ask = data.Ask,
            Last = data.Last,
            Mark = data.Mark,
            BidSize = data.BidSize,
            AskSize = data.AskSize,
            Volume = data.Volume,
            OpenInterest = data.OpenInterest,
            Delta = data.Delta,
            Gamma = data.Gamma,
            Theta = data.Theta,
            Vega = data.Vega,
            Rho = data.Rho,
            ImpliedVolatility = data.ImpliedVolatility,
            MidPrice = midPrice,
            BidAskSpread = bidAskSpread,
            Source = MarketDataProvider.AlphaVantage,
            Timestamp = DateTime.UtcNow,
            DataQualityScore = CalculateQualityScore(data)
        };
    }

    private decimal CalculateQualityScore(AlphaVantageOptionData data)
    {
        decimal score = 0;

        // Greeks present (40% weight total)
        if (data.Delta.HasValue)
        {
            score += 0.1m;
        }

        if (data.Gamma.HasValue)
        {
            score += 0.1m;
        }

        if (data.Theta.HasValue)
        {
            score += 0.1m;
        }

        if (data.Vega.HasValue)
        {
            score += 0.1m;
        }

        // IV present (20% weight)
        if (data.ImpliedVolatility.HasValue)
        {
            score += 0.2m;
        }

        // Bid/Ask valid (20% weight)
        if (data.Bid.HasValue && data.Ask.HasValue && data.Ask > data.Bid)
        {
            score += 0.2m;
        }

        // Volume/OI present (20% weight total)
        if (data.Volume > 0)
        {
            score += 0.1m;
        }

        if (data.OpenInterest > 0)
        {
            score += 0.1m;
        }

        return score;
    }

    private OptionsChain ValidateAndEnrichChain(OptionsChain chain)
    {
        var validated = chain.Contracts
            .Where(ValidateContract)
            .ToList();

        var filtered = chain.Contracts.Count - validated.Count;
        if (filtered > 0)
        {
            _logger.LogWarning(
                "Filtered {Filtered}/{Total} invalid contracts for {Symbol}",
                filtered, chain.Contracts.Count, chain.Symbol);
        }

        _logger.LogInformation(
            "Validated {Valid}/{Total} contracts for {Symbol}",
            validated.Count, chain.Contracts.Count, chain.Symbol);

        return chain with { Contracts = validated };
    }

    private bool ValidateContract(OptionContract contract)
    {
        // Critical validations (from Python reference implementation)

        // 1. Bid <= Ask
        if (contract.Bid.HasValue && contract.Ask.HasValue && contract.Bid > contract.Ask)
        {
            _logger.LogWarning("Invalid bid/ask for {Contract}: Bid={Bid} > Ask={Ask}",
                contract.ContractSymbol, contract.Bid, contract.Ask);
            return false;
        }

        // 2. Delta bounds
        if (contract.Delta.HasValue)
        {
            if (contract.Type == OptionType.Call && (contract.Delta < 0 || contract.Delta > 1))
            {
                _logger.LogWarning("Invalid delta for call {Contract}: {Delta}",
                    contract.ContractSymbol, contract.Delta);
                return false;
            }
            if (contract.Type == OptionType.Put && (contract.Delta < -1 || contract.Delta > 0))
            {
                _logger.LogWarning("Invalid delta for put {Contract}: {Delta}",
                    contract.ContractSymbol, contract.Delta);
                return false;
            }
        }

        // 3. Gamma non-negative (always >= 0)
        if (contract.Gamma.HasValue && contract.Gamma < 0)
        {
            _logger.LogWarning("Invalid gamma for {Contract}: {Gamma}",
                contract.ContractSymbol, contract.Gamma);
            return false;
        }

        // 4. Strike > 0
        if (contract.StrikePrice <= 0)
        {
            _logger.LogWarning("Invalid strike for {Contract}: {Strike}",
                contract.ContractSymbol, contract.StrikePrice);
            return false;
        }

        // 5. Open Interest >= 0
        if (contract.OpenInterest.HasValue && contract.OpenInterest < 0)
        {
            _logger.LogWarning("Invalid open interest for {Contract}: {OI}",
                contract.ContractSymbol, contract.OpenInterest);
            return false;
        }

        return true;
    }

    private async Task<bool> CheckRateLimitAsync()
    {
        await _rateLimitLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            // Remove old timestamps (O(1) queue operations)
            while (_callTimestamps.Count > 0 && _callTimestamps.Peek() < oneMinuteAgo)
            {
                _callTimestamps.Dequeue();
            }

            // Check if at limit
            if (_callTimestamps.Count >= _callsPerMinute)
            {
                _logger.LogDebug("Rate limit reached: {Count}/{Limit}",
                    _callTimestamps.Count, _callsPerMinute);
                return false;
            }

            // Record this call
            _callTimestamps.Enqueue(now);
            return true;
        }
        finally
        {
            _rateLimitLock.Release();
        }
    }

    private static bool IsStale(OptionsChain chain)
    {
        // Options data is stale after 5 minutes (more volatile than quotes)
        return (DateTime.UtcNow - chain.Timestamp).TotalMinutes > 5;
    }
}

#region Alpha Vantage API Response Models

/// <summary>
/// Alpha Vantage HISTORICAL_OPTIONS API response.
/// </summary>
internal record AlphaVantageOptionsResponse
{
    [JsonPropertyName("data")]
    public List<AlphaVantageOptionData> Data { get; init; } = new();
}

/// <summary>
/// Individual option contract data from Alpha Vantage.
/// </summary>
internal record AlphaVantageOptionData
{
    [JsonPropertyName("contractID")]
    public string? ContractID { get; init; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = "";

    [JsonPropertyName("expiration")]
    public string Expiration { get; init; } = "";

    [JsonPropertyName("strike")]
    public decimal Strike { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("date")]
    public string Date { get; init; } = "";

    // Pricing
    [JsonPropertyName("bid")]
    public decimal? Bid { get; init; }

    [JsonPropertyName("ask")]
    public decimal? Ask { get; init; }

    [JsonPropertyName("last")]
    public decimal? Last { get; init; }

    [JsonPropertyName("mark")]
    public decimal? Mark { get; init; }

    [JsonPropertyName("bid_size")]
    public int? BidSize { get; init; }

    [JsonPropertyName("ask_size")]
    public int? AskSize { get; init; }

    // Volume & Interest
    [JsonPropertyName("volume")]
    public long? Volume { get; init; }

    [JsonPropertyName("open_interest")]
    public long? OpenInterest { get; init; }

    // Greeks
    [JsonPropertyName("implied_volatility")]
    public decimal? ImpliedVolatility { get; init; }

    [JsonPropertyName("delta")]
    public decimal? Delta { get; init; }

    [JsonPropertyName("gamma")]
    public decimal? Gamma { get; init; }

    [JsonPropertyName("theta")]
    public decimal? Theta { get; init; }

    [JsonPropertyName("vega")]
    public decimal? Vega { get; init; }

    [JsonPropertyName("rho")]
    public decimal? Rho { get; init; }
}

#endregion
