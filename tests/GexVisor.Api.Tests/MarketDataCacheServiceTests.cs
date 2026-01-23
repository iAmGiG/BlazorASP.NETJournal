// Copyright (c) GexVisor. All rights reserved.

using GexVisor.Api.Services;
using GexVisor.Core;
using Microsoft.Data.Sqlite;

namespace GexVisor.Api.Tests;

/// <summary>
/// Unit tests for MarketDataCacheService.
/// Tests domain-specific caching logic and smart TTL.
/// </summary>
public class MarketDataCacheServiceTests : IDisposable
{
    private readonly string testDbPath;
    private readonly SqliteCacheService cacheService;
    private readonly MarketDataCacheService marketCache;

    public MarketDataCacheServiceTests()
    {
        this.testDbPath = Path.Combine(Path.GetTempPath(), $"gexvisor_market_test_{Guid.NewGuid()}.db");
        this.cacheService = new SqliteCacheService(this.testDbPath);
        this.marketCache = new MarketDataCacheService(this.cacheService);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.cacheService.Dispose();

        // Clear SQLite connection pools to release file locks
        SqliteConnection.ClearAllPools();

        // Clean up test database with retry
        for (int i = 0; i < 3; i++)
        {
            try
            {
                if (File.Exists(this.testDbPath))
                {
                    File.Delete(this.testDbPath);
                }

                break;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetTtlForQuote_RecentQuote_Returns24Hours()
    {
        // Arrange - Quote from 1 hour ago
        var quoteTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var ttl = MarketDataCacheService.GetTtlForQuote(quoteTime);

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), ttl);
    }

    [Fact]
    public void GetTtlForQuote_HistoricalQuote_Returns10Years()
    {
        // Arrange - Quote from 1 week ago
        var quoteTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var ttl = MarketDataCacheService.GetTtlForQuote(quoteTime);

        // Assert
        Assert.Equal(TimeSpan.FromDays(3650), ttl);
    }

    [Fact]
    public void IsQuoteStale_FreshQuote_ReturnsFalse()
    {
        // Arrange - Quote from 5 minutes ago
        var quote = new Quote
        {
            Symbol = "SPY",
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
        };

        // Act
        var isStale = MarketDataCacheService.IsQuoteStale(quote);

        // Assert
        Assert.False(isStale);
    }

    [Fact]
    public void IsQuoteStale_OldQuote_ReturnsTrue()
    {
        // Arrange - Quote from 30 minutes ago
        var quote = new Quote
        {
            Symbol = "SPY",
            Timestamp = DateTime.UtcNow.AddMinutes(-30),
        };

        // Act
        var isStale = MarketDataCacheService.IsQuoteStale(quote);

        // Assert
        Assert.True(isStale);
    }

    [Fact]
    public async Task SetQuoteAsync_ThenGetQuoteAsync_ReturnsQuote()
    {
        // Arrange
        var quote = new Quote
        {
            Symbol = "SPY",
            Price = 500.00m,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this.marketCache.SetQuoteAsync(quote);
        var result = await this.marketCache.GetQuoteAsync("SPY");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SPY", result.Symbol);
        Assert.Equal(500.00m, result.Price);
    }

    [Fact]
    public async Task GetQuoteAsync_CaseInsensitive()
    {
        // Arrange
        var quote = new Quote
        {
            Symbol = "SPY",
            Price = 500.00m,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this.marketCache.SetQuoteAsync(quote);
        var result = await this.marketCache.GetQuoteAsync("spy");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SPY", result.Symbol);
    }

    [Fact]
    public void GetTtlForBars_Minute1_Returns5Minutes()
    {
        var ttl = MarketDataCacheService.GetTtlForBars(BarTimeframe.Minute1, DateTime.UtcNow);
        Assert.Equal(TimeSpan.FromMinutes(5), ttl);
    }

    [Fact]
    public void GetTtlForBars_Hour1_Returns2Hours()
    {
        var ttl = MarketDataCacheService.GetTtlForBars(BarTimeframe.Hour1, DateTime.UtcNow);
        Assert.Equal(TimeSpan.FromHours(2), ttl);
    }

    [Fact]
    public void GetTtlForBars_Day_Returns24Hours()
    {
        var ttl = MarketDataCacheService.GetTtlForBars(BarTimeframe.Day, DateTime.UtcNow);
        Assert.Equal(TimeSpan.FromHours(24), ttl);
    }

    [Fact]
    public void GetTtlForBars_HistoricalData_Returns10Years()
    {
        // Data from a week ago
        var oldDate = DateTime.UtcNow.AddDays(-7);
        var ttl = MarketDataCacheService.GetTtlForBars(BarTimeframe.Day, oldDate);
        Assert.Equal(TimeSpan.FromDays(3650), ttl);
    }

    [Fact]
    public async Task SetBarsAsync_ThenGetBarsAsync_ReturnsBars()
    {
        // Arrange
        var bars = new List<OhlcvBar>
        {
            new() { Symbol = "SPY", Timestamp = DateTime.UtcNow.AddDays(-1), Open = 500, High = 505, Low = 498, Close = 502, Volume = 1000000 },
            new() { Symbol = "SPY", Timestamp = DateTime.UtcNow, Open = 502, High = 508, Low = 501, Close = 506, Volume = 1200000 },
        };

        // Act
        await this.marketCache.SetBarsAsync("SPY", BarTimeframe.Day, bars);
        var result = await this.marketCache.GetBarsAsync("SPY", BarTimeframe.Day);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SetBarsAsync_EmptyList_DoesNotCache()
    {
        // Arrange
        var emptyBars = new List<OhlcvBar>();

        // Act
        await this.marketCache.SetBarsAsync("EMPTY", BarTimeframe.Day, emptyBars);
        var result = await this.marketCache.GetBarsAsync("EMPTY", BarTimeframe.Day);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMultipleQuotesAsync_ReturnsCachedQuotes()
    {
        // Arrange
        await this.marketCache.SetQuoteAsync(new Quote { Symbol = "SPY", Price = 500, Timestamp = DateTime.UtcNow });
        await this.marketCache.SetQuoteAsync(new Quote { Symbol = "QQQ", Price = 400, Timestamp = DateTime.UtcNow });

        // Act
        var result = await this.marketCache.GetMultipleQuotesAsync(new[] { "SPY", "QQQ", "IWM" });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("SPY"));
        Assert.True(result.ContainsKey("QQQ"));
        Assert.False(result.ContainsKey("IWM"));
    }

    [Fact]
    public async Task SetMultipleQuotesAsync_CachesAllQuotes()
    {
        // Arrange
        var quotes = new[]
        {
            new Quote { Symbol = "SPY", Price = 500, Timestamp = DateTime.UtcNow },
            new Quote { Symbol = "QQQ", Price = 400, Timestamp = DateTime.UtcNow },
        };

        // Act
        await this.marketCache.SetMultipleQuotesAsync(quotes);

        // Assert
        Assert.NotNull(await this.marketCache.GetQuoteAsync("SPY"));
        Assert.NotNull(await this.marketCache.GetQuoteAsync("QQQ"));
    }

    [Fact]
    public async Task InvalidateSymbolAsync_RemovesSymbolData()
    {
        // Arrange
        await this.marketCache.SetQuoteAsync(new Quote { Symbol = "SPY", Price = 500, Timestamp = DateTime.UtcNow });
        await this.marketCache.SetBarsAsync("SPY", BarTimeframe.Day, new List<OhlcvBar>
        {
            new() { Symbol = "SPY", Timestamp = DateTime.UtcNow, Open = 500, High = 505, Low = 498, Close = 502, Volume = 1000000 },
        });

        // Act
        await this.marketCache.InvalidateSymbolAsync("SPY");

        // Assert
        Assert.Null(await this.marketCache.GetQuoteAsync("SPY"));
        Assert.Null(await this.marketCache.GetBarsAsync("SPY", BarTimeframe.Day));
    }

    [Fact]
    public async Task InvalidateAllQuotesAsync_RemovesOnlyQuotes()
    {
        // Arrange
        await this.marketCache.SetQuoteAsync(new Quote { Symbol = "SPY", Price = 500, Timestamp = DateTime.UtcNow });
        await this.marketCache.SetBarsAsync("SPY", BarTimeframe.Day, new List<OhlcvBar>
        {
            new() { Symbol = "SPY", Timestamp = DateTime.UtcNow, Open = 500, High = 505, Low = 498, Close = 502, Volume = 1000000 },
        });

        // Act
        await this.marketCache.InvalidateAllQuotesAsync();

        // Assert
        Assert.Null(await this.marketCache.GetQuoteAsync("SPY"));
        Assert.NotNull(await this.marketCache.GetBarsAsync("SPY", BarTimeframe.Day));
    }
}
