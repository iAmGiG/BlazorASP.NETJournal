// Copyright (c) GexVisor. All rights reserved.

using System.Diagnostics;
using System.Globalization;
using System.Net;
using GexVisor.Api.Services;
using GexVisor.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit.Abstractions;

namespace GexVisor.Api.Tests.LoadTests;

/// <summary>
/// Load tests for GEX API endpoints.
/// These tests validate performance under concurrent load.
/// Run separately from unit tests using: dotnet test --filter "Category=LoadTest".
/// </summary>
[Trait("Category", "LoadTest")]
public class GexApiLoadTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly ITestOutputHelper output;
    private HttpClient client = null!;
    private readonly string testDbPath;

    public GexApiLoadTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        this.output = output;
        this.testDbPath = Path.Combine(Path.GetTempPath(), $"gexvisor_loadtest_{Guid.NewGuid()}.db");

        // Configure factory with mocked external services
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing service registrations
                services.RemoveAll<ICacheService>();
                services.RemoveAll<IMarketDataService>();
                services.RemoveAll<IOptionsChainService>();
                services.RemoveAll<IGexCalculationService>();

                // Replace cache service with test-specific database
                services.AddSingleton<ICacheService>(sp =>
                    new SqliteCacheService(this.testDbPath, sp.GetService<Microsoft.Extensions.Logging.ILogger<SqliteCacheService>>()));

                // Mock external market data service to avoid real API calls
                var mockMarketService = new Mock<IMarketDataService>();
                mockMarketService
                    .Setup(s => s.GetQuoteAsync(It.IsAny<string>()))
                    .ReturnsAsync((string symbol) => MarketDataResult<Quote>.Ok(
                        new Quote
                        {
                            Symbol = symbol,
                            Price = 450.00m,
                            Change = 2.50m,
                            ChangePercent = 0.56m,
                            Volume = 50000000,
                            Timestamp = DateTime.UtcNow,
                        },
                        MarketDataProvider.Alpaca));

                services.AddSingleton(mockMarketService.Object);

                // Mock options chain service
                var mockOptionsService = new Mock<IOptionsChainService>();
                mockOptionsService
                    .Setup(s => s.GetChainAsync(It.IsAny<string>(), It.IsAny<DateTime?>()))
                    .ReturnsAsync((string symbol, DateTime? exp) =>
                        OptionsChainResult<OptionsChain>.Ok(
                            CreateMockOptionsChain(symbol),
                            MarketDataProvider.AlphaVantage));

                services.AddSingleton(mockOptionsService.Object);

                // Re-add GEX calculation service (it depends on the mocked services)
                services.AddSingleton<IGexCalculationService, GexCalculationService>();
            });
        });
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        this.client = this.factory.CreateClient();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        this.client?.Dispose();

        // Cleanup test database
        await Task.Delay(100); // Allow connections to close
        try
        {
            if (File.Exists(this.testDbPath))
            {
                File.Delete(this.testDbPath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [Trait("Category", "LoadTest")]
    public async Task ConcurrentGexRequests_MeasurePerformance(int userCount)
    {
        // Arrange
        var latencies = new List<double>();
        var errors = 0;
        var sw = Stopwatch.StartNew();

        // Warm up cache with one request
        await this.client.GetAsync("/api/gex/SPY");

        // Get initial cache stats
        var initialStats = await this.GetCacheStatsAsync();

        // Act - simulate concurrent users
        var tasks = Enumerable.Range(0, userCount).Select(async _ =>
        {
            var requestSw = Stopwatch.StartNew();
            try
            {
                var response = await this.client.GetAsync("/api/gex/SPY");
                requestSw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    lock (latencies)
                    {
                        latencies.Add(requestSw.Elapsed.TotalMilliseconds);
                    }
                }
                else
                {
                    Interlocked.Increment(ref errors);
                }
            }
            catch
            {
                Interlocked.Increment(ref errors);
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Get final cache stats
        var finalStats = await this.GetCacheStatsAsync();

        // Calculate metrics
        var metrics = LoadTestMetrics.FromLatencies($"Concurrent GEX ({userCount} users)", latencies, sw.Elapsed);
        metrics.ErrorCount = errors;
        metrics.CacheHitsBefore = initialStats.hits;
        metrics.CacheMissesBefore = initialStats.misses;
        metrics.CacheHitsAfter = finalStats.hits;
        metrics.CacheMissesAfter = finalStats.misses;

        // Output results
        this.output.WriteLine(metrics.ToString());

        // Assert
        Assert.True(metrics.SuccessRate >= 95, $"Success rate {metrics.SuccessRate:F1}% below 95%");
        Assert.True(metrics.P99Ms < 2000, $"p99 latency {metrics.P99Ms:F0}ms exceeds 2000ms");
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task RapidRefresh_AllRequestsSucceed()
    {
        // Arrange
        const int requestCount = 100;
        var latencies = new List<double>();
        var sw = Stopwatch.StartNew();

        // Act - rapid sequential requests (simulates user spam-refreshing)
        for (int i = 0; i < requestCount; i++)
        {
            var requestSw = Stopwatch.StartNew();
            var response = await this.client.GetAsync("/api/gex/SPY");
            requestSw.Stop();

            if (response.IsSuccessStatusCode)
            {
                latencies.Add(requestSw.Elapsed.TotalMilliseconds);
            }
        }

        sw.Stop();

        // Calculate metrics
        var metrics = LoadTestMetrics.FromLatencies("Rapid Refresh (100 requests)", latencies, sw.Elapsed);

        this.output.WriteLine(metrics.ToString());

        // Assert - all requests should succeed
        Assert.Equal(requestCount, metrics.SuccessCount);

        // Throughput should be reasonable (> 10 req/s)
        Assert.True(metrics.RequestsPerSecond > 10, $"Throughput {metrics.RequestsPerSecond:F1} req/s is too low");
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task MultiSymbol_IndependentCaching()
    {
        // Arrange
        var symbols = new[] { "SPY", "QQQ", "IWM", "AAPL", "MSFT" };
        const int requestsPerSymbol = 20;
        var latencies = new List<double>();
        var sw = Stopwatch.StartNew();

        // Act - concurrent requests for different symbols
        var tasks = new List<Task>();
        foreach (var symbol in symbols)
        {
            for (int i = 0; i < requestsPerSymbol; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var requestSw = Stopwatch.StartNew();
                    var response = await this.client.GetAsync($"/api/gex/{symbol}");
                    requestSw.Stop();

                    if (response.IsSuccessStatusCode)
                    {
                        lock (latencies)
                        {
                            latencies.Add(requestSw.Elapsed.TotalMilliseconds);
                        }
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
        sw.Stop();

        // Calculate metrics
        var totalRequests = symbols.Length * requestsPerSymbol;
        var metrics = LoadTestMetrics.FromLatencies($"Multi-Symbol ({symbols.Length} symbols x {requestsPerSymbol})", latencies, sw.Elapsed);

        this.output.WriteLine(metrics.ToString());

        // Assert
        Assert.True(metrics.SuccessCount >= totalRequests * 0.95, $"Only {metrics.SuccessCount}/{totalRequests} succeeded");
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task QuoteEndpoint_HandlesRequests_Gracefully()
    {
        // Arrange
        var requestCount = 10;
        var completedCount = 0;

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var response = await this.client.GetAsync("/api/market/quote/SPY");
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
            {
                completedCount++;
            }
        }

        this.output.WriteLine($"Quote Endpoint Test: {completedCount}/{requestCount} requests completed properly");

        // Assert - all requests should complete without server errors
        Assert.Equal(requestCount, completedCount);
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task CacheStats_EndpointExists_ReturnsResponse()
    {
        // Arrange & Act - basic endpoint test
        var response = await this.client.GetAsync("/api/cache/stats");

        this.output.WriteLine($"Cache Stats Endpoint: {response.StatusCode}");

        // Assert - endpoint should exist (may return error if cache isn't configured, but shouldn't 404)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(long hits, long misses)> GetCacheStatsAsync()
    {
        try
        {
            var response = await this.client.GetAsync("/api/cache/stats");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                // Parse basic stats - looking for totalHits and totalMisses
                var hitsMatch = System.Text.RegularExpressions.Regex.Match(json, @"""totalHits""\s*:\s*(\d+)");
                var missesMatch = System.Text.RegularExpressions.Regex.Match(json, @"""totalMisses""\s*:\s*(\d+)");

                var hits = hitsMatch.Success ? long.Parse(hitsMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
                var misses = missesMatch.Success ? long.Parse(missesMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0;

                return (hits, misses);
            }
        }
        catch
        {
            // Ignore errors
        }

        return (0, 0);
    }

    private static OptionsChain CreateMockOptionsChain(string symbol)
    {
        var expiration = DateTime.Today.AddDays(30);
        var tradingDate = DateTime.Today;
        var contracts = new List<OptionContract>();

        // Generate mock contracts around spot price of 450
        for (decimal strike = 400; strike <= 500; strike += 5)
        {
            var callContractSymbol = $"{symbol}{expiration:yyMMdd}C{strike:00000000}";
            var putContractSymbol = $"{symbol}{expiration:yyMMdd}P{strike:00000000}";

            contracts.Add(new OptionContract
            {
                Symbol = symbol,
                ContractSymbol = callContractSymbol,
                StrikePrice = strike,
                Type = OptionType.Call,
                ExpirationDate = expiration,
                TradingDate = tradingDate,
                Bid = Math.Max(0.01m, 450 - strike + 5),
                Ask = Math.Max(0.05m, 450 - strike + 6),
                Last = Math.Max(0.03m, 450 - strike + 5.5m),
                Volume = 1000,
                OpenInterest = 5000,
                ImpliedVolatility = 0.25m,
                Delta = strike < 450 ? 0.7m : 0.3m,
                Gamma = 0.02m,
                Theta = -0.05m,
                Vega = 0.15m,
            });

            contracts.Add(new OptionContract
            {
                Symbol = symbol,
                ContractSymbol = putContractSymbol,
                StrikePrice = strike,
                Type = OptionType.Put,
                ExpirationDate = expiration,
                TradingDate = tradingDate,
                Bid = Math.Max(0.01m, strike - 450 + 5),
                Ask = Math.Max(0.05m, strike - 450 + 6),
                Last = Math.Max(0.03m, strike - 450 + 5.5m),
                Volume = 800,
                OpenInterest = 4000,
                ImpliedVolatility = 0.25m,
                Delta = strike > 450 ? -0.7m : -0.3m,
                Gamma = 0.02m,
                Theta = -0.05m,
                Vega = 0.15m,
            });
        }

        return new OptionsChain
        {
            Symbol = symbol,
            ExpirationDate = expiration,
            Contracts = contracts,
            Timestamp = DateTime.UtcNow,
            Source = MarketDataProvider.AlphaVantage,
        };
    }
}
