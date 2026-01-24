// Copyright (c) GexVisor. All rights reserved.

using System.Net;
using System.Text.Json;
using GexVisor.Api.Services;
using GexVisor.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace GexVisor.Api.Tests;

public class OptionsChainServiceTests : IDisposable
{
    private readonly Mock<IApiConfigService> _mockConfig;
    private readonly Mock<IHttpClientFactory> _mockHttpFactory;
    private readonly OptionsChainCacheService _cache;
    private readonly IOptionsChainService _service;
    private readonly string _testDbPath;

    public OptionsChainServiceTests()
    {
        // Setup API config mock
        _mockConfig = new Mock<IApiConfigService>();
        var config = new ApiConfiguration
        {
            AlphaVantageKey = "demo",
        };
        _mockConfig.Setup(c => c.Configuration).Returns(config);

        // Setup HTTP client factory mock
        _mockHttpFactory = new Mock<IHttpClientFactory>();

        // Setup cache with temporary SQLite database
        _testDbPath = Path.Combine(Path.GetTempPath(), $"options_test_{Guid.NewGuid()}.db");
        var sqliteCache = new SqliteCacheService(_testDbPath, null);
        _cache = new OptionsChainCacheService(sqliteCache, Mock.Of<ILogger<OptionsChainCacheService>>());

        // Create service
        _service = new OptionsChainService(
            _mockConfig.Object,
            _cache,
            _mockHttpFactory.Object,
            Mock.Of<ILogger<OptionsChainService>>());
    }

    [Fact]
    public async Task GetChainAsync_ValidSymbol_ReturnsContracts()
    {
        // Arrange
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Contracts);
        Assert.Equal("SPY", result.Data.Symbol);
        Assert.Equal(MarketDataProvider.AlphaVantage, result.Source);
    }

    [Fact]
    public async Task GetChainAsync_InvalidBidAsk_FiltersOut()
    {
        // Arrange - Create contracts with invalid bid > ask
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", bid = 10m, ask = 5m, delta = 0.5m, gamma = 0.01m },
                new { symbol = "SPY", strike = 451m, type = "call", expiration = "2024-01-19", date = "2024-01-15", bid = 5m, ask = 10m, delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Contracts); // Only 1 valid contract
    }

    [Fact]
    public async Task Validation_DeltaOutOfBounds_RejectsContract()
    {
        // Arrange - Create call with delta > 1 (invalid)
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 1.5m, gamma = 0.01m },
                new { symbol = "SPY", strike = 451m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Contracts); // Only 1 valid contract
    }

    [Fact]
    public async Task Validation_NegativeGamma_RejectsContract()
    {
        // Arrange - Create contract with gamma < 0 (invalid)
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = -0.01m },
                new { symbol = "SPY", strike = 451m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Contracts); // Only 1 valid contract
    }

    [Fact]
    public async Task Validation_NegativeStrike_RejectsContract()
    {
        // Arrange - Create contract with strike <= 0 (invalid)
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 0m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Contracts); // Only 1 valid contract
    }

    [Fact]
    public async Task Validation_NegativeOpenInterest_RejectsContract()
    {
        // Arrange - Create contract with OI < 0 (invalid)
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m, openInterest = -100L },
                new { symbol = "SPY", strike = 451m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m, openInterest = 100L },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Contracts); // Only 1 valid contract
    }

    [Fact]
    public async Task GetChainAsync_CachedData_ReturnsCached()
    {
        // Arrange - First call to populate cache
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Act - Second call should return cached data (no HTTP call)
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetContractAsync_ValidParams_ReturnsContract()
    {
        // Arrange
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
                new { symbol = "SPY", strike = 451m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.6m, gamma = 0.02m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetContractAsync("SPY", 450m, OptionType.Call, new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(450m, result.Data.StrikePrice);
        Assert.Equal(OptionType.Call, result.Data.Type);
    }

    [Fact]
    public async Task GetContractAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act - Request strike that doesn't exist
        var result = await _service.GetContractAsync("SPY", 999m, OptionType.Call, new DateTime(2024, 1, 19));

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Contains("Contract not found", result.Error);
    }

    [Fact]
    public async Task GetExpirationDatesAsync_ValidSymbol_ReturnsUniqueDates()
    {
        // Arrange
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
                new { symbol = "SPY", strike = 451m, type = "call", expiration = "2024-01-19", date = "2024-01-15", delta = 0.6m, gamma = 0.02m },
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-02-16", date = "2024-01-15", delta = 0.5m, gamma = 0.01m },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetExpirationDatesAsync("SPY");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count); // Two unique expiration dates
        Assert.Contains(new DateTime(2024, 1, 19), result.Data);
        Assert.Contains(new DateTime(2024, 2, 16), result.Data);
    }

    [Fact]
    public async Task QualityScore_AllGreeks_ReturnsHigh()
    {
        // Arrange
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new
                {
                    symbol = "SPY",
                    strike = 450m,
                    type = "call",
                    expiration = "2024-01-19",
                    date = "2024-01-15",
                    delta = 0.5m,
                    gamma = 0.01m,
                    theta = -0.05m,
                    vega = 0.1m,
                    impliedVolatility = 0.2m,
                    bid = 5m,
                    ask = 6m,
                    volume = 1000L,
                    openInterest = 5000L,
                },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var contract = result.Data.Contracts.First();
        Assert.Equal(1.0m, contract.DataQualityScore); // All fields present = 1.0
    }

    [Fact]
    public async Task QualityScore_NoGreeks_ReturnsLow()
    {
        // Arrange - Only symbol, strike, type, expiration, date
        var mockResponse = CreateMockAlphaVantageResponse(
            new[]
            {
                new { symbol = "SPY", strike = 450m, type = "call", expiration = "2024-01-19", date = "2024-01-15" },
            });

        SetupHttpMock(mockResponse);

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var contract = result.Data.Contracts.First();
        Assert.True(contract.DataQualityScore < 0.3m); // No Greeks/IV/volume = low score
    }

    [Fact]
    public async Task AlphaVantage_NoApiKey_ReturnsFailure()
    {
        // Arrange - Clear API key
        var emptyConfig = new ApiConfiguration();
        _mockConfig.Setup(c => c.Configuration).Returns(emptyConfig);

        var service = new OptionsChainService(
            _mockConfig.Object,
            _cache,
            _mockHttpFactory.Object,
            Mock.Of<ILogger<OptionsChainService>>());

        // Act
        var result = await _service.GetChainAsync("SPY", new DateTime(2024, 1, 19));

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to fetch", result.Error);
    }

    private string CreateMockAlphaVantageResponse(dynamic[] contracts)
    {
        var response = new
        {
            data = contracts.Select(c =>
            {
                // Safely extract properties from dynamic object
                var dict = c as IDictionary<string, object> ?? new Dictionary<string, object>();

                return new
                {
                    contractID = (string?)null,
                    symbol = GetProp<string>(c, "symbol"),
                    expiration = GetProp<string>(c, "expiration"),
                    strike = GetProp<decimal>(c, "strike"),
                    type = GetProp<string>(c, "type"),
                    date = GetProp<string>(c, "date"),
                    bid = GetProp<decimal?>(c, "bid"),
                    ask = GetProp<decimal?>(c, "ask"),
                    last = (decimal?)null,
                    mark = (decimal?)null,
                    bid_size = (int?)null,
                    ask_size = (int?)null,
                    volume = GetProp<long?>(c, "volume"),
                    open_interest = GetProp<long?>(c, "openInterest"),
                    implied_volatility = GetProp<decimal?>(c, "impliedVolatility"),
                    delta = GetProp<decimal?>(c, "delta"),
                    gamma = GetProp<decimal?>(c, "gamma"),
                    theta = GetProp<decimal?>(c, "theta"),
                    vega = GetProp<decimal?>(c, "vega"),
                    rho = (decimal?)null,
                };
            }).ToList(),
        };

        return JsonSerializer.Serialize(response);
    }

    private static T GetProp<T>(dynamic obj, string propName)
    {
        try
        {
            var type = (Type)obj.GetType();
            var prop = type.GetProperty(propName);
            if (prop != null)
            {
                var value = prop.GetValue(obj);
                if (value == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                {
                    return default!;
                }

                return (T)value!;
            }

            return default!;
        }
        catch
        {
            return default!;
        }
    }

    private void SetupHttpMock(string responseContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent),
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Cleanup test database
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }

        GC.SuppressFinalize(this);
    }
}
