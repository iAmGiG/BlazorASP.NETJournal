using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Domain-aware cache wrapper for options chain data with smart TTL logic.
/// </summary>
public class OptionsChainCacheService
{
    private readonly ICacheService _cache;
    private readonly ILogger<OptionsChainCacheService> _logger;

    // Cache TTL for options data (shorter than quotes - options are more volatile)
    private static readonly TimeSpan RecentChainTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StaleChainTtl = TimeSpan.FromHours(1);

    public OptionsChainCacheService(ICacheService cache, ILogger<OptionsChainCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get cached options chain for symbol.
    /// </summary>
    public async Task<OptionsChain?> GetChainAsync(string symbol, DateTime? expirationDate)
    {
        var key = GetChainCacheKey(symbol, expirationDate);
        return await _cache.GetAsync<OptionsChain>(key);
    }

    /// <summary>
    /// Cache options chain with smart TTL based on data age.
    /// </summary>
    public async Task SetChainAsync(OptionsChain chain, DateTime? expirationDate = null)
    {
        var key = GetChainCacheKey(chain.Symbol, expirationDate);
        var ttl = GetTtlForChain(chain.Timestamp);
        await _cache.SetAsync(key, chain, ttl);

        _logger.LogDebug("Cached options chain for {Symbol} with TTL {TTL}",
            chain.Symbol, ttl);
    }

    /// <summary>
    /// Get cached individual option contract.
    /// </summary>
    public async Task<OptionContract?> GetContractAsync(
        string symbol,
        decimal strike,
        OptionType type,
        DateTime expiration)
    {
        var key = $"option:{symbol}:{strike}:{type}:{expiration:yyyy-MM-dd}";
        return await _cache.GetAsync<OptionContract>(key);
    }

    /// <summary>
    /// Cache individual option contract.
    /// </summary>
    public async Task SetContractAsync(OptionContract contract)
    {
        var key = $"option:{contract.Symbol}:{contract.StrikePrice}:{contract.Type}:{contract.ExpirationDate:yyyy-MM-dd}";
        var ttl = GetTtlForChain(contract.Timestamp);
        await _cache.SetAsync(key, contract, ttl);
    }

    /// <summary>
    /// Invalidate all cached data for a symbol (both chains and individual contracts).
    /// </summary>
    public async Task InvalidateSymbolAsync(string symbol)
    {
        await _cache.InvalidateAsync($"options:{symbol}:*");
        await _cache.InvalidateAsync($"option:{symbol}:*");

        _logger.LogInformation("Invalidated options cache for {Symbol}", symbol);
    }

    private static string GetChainCacheKey(string symbol, DateTime? expirationDate)
    {
        return expirationDate.HasValue
            ? $"options:{symbol}:{expirationDate.Value:yyyy-MM-dd}"
            : $"options:{symbol}:all";
    }

    private static TimeSpan GetTtlForChain(DateTime chainTimestamp)
    {
        var age = DateTime.UtcNow - chainTimestamp;

        // Recent data (< 30 min): short TTL for freshness
        // Older data: longer TTL (less likely to change significantly)
        return age.TotalMinutes < 30 ? RecentChainTtl : StaleChainTtl;
    }
}
