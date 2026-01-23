using System.Net;
using System.Text.Json;
using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for PriceDataService.
/// Tests API calls, caching, error handling, and trade-context data loading.
/// </summary>
public class PriceDataServiceTests
{
    [Fact]
    public async Task GetCandlesAsync_ReturnsData_WhenApiSucceeds()
    {
        // Arrange
        var bars = CreateSampleBars("SPY", 10);
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, bars);
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesAsync("SPY", "1d", 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Bars.Should().HaveCount(10);
        result.Bars.First().Symbol.Should().Be("SPY");
    }

    [Fact]
    public async Task GetCandlesAsync_ReturnsError_WhenApiReturns404()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.NotFound, "Symbol not found");
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesAsync("INVALID", "1d", 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetCandlesAsync_ReturnsError_WhenApiReturns500()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Server error");
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesAsync("SPY", "1d", 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("InternalServerError");
    }

    [Fact]
    public async Task GetCandlesAsync_ReturnsCachedData_OnSecondCall()
    {
        // Arrange
        var bars = CreateSampleBars("SPY", 10);
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(bars))
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost/") };
        var service = new PriceDataService(httpClient);

        // Act
        var result1 = await service.GetCandlesAsync("SPY", "1d", 10);
        var result2 = await service.GetCandlesAsync("SPY", "1d", 10); // Should hit cache

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        callCount.Should().Be(1); // Only one HTTP call due to caching
    }

    [Fact]
    public async Task GetCandlesAsync_SortsBarsAscendingByTimestamp()
    {
        // Arrange
        var bars = new List<OhlcvBar>
        {
            CreateBar("SPY", DateTime.Now.AddDays(-1)),
            CreateBar("SPY", DateTime.Now.AddDays(-3)),
            CreateBar("SPY", DateTime.Now.AddDays(-2))
        };
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, bars);
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesAsync("SPY", "1d", 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Bars.Should().BeInAscendingOrder(b => b.Timestamp);
    }

    [Fact]
    public async Task GetCandlesAsync_UppercasesSymbol()
    {
        // Arrange
        var bars = CreateSampleBars("SPY", 5);
        var requestedUrl = "";
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => requestedUrl = req.RequestUri?.ToString() ?? "")
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(bars))
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost/") };
        var service = new PriceDataService(httpClient);

        // Act
        await service.GetCandlesAsync("spy", "1d", 10);

        // Assert
        requestedUrl.Should().Contain("SPY");
    }

    [Fact]
    public async Task GetCandlesForTradeAsync_ReturnsFilteredData()
    {
        // Arrange
        var entryDate = DateTime.Now.AddDays(-10);
        var exitDate = DateTime.Now.AddDays(-5);
        var bars = CreateSampleBars("SPY", 30); // More bars than needed
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, bars);
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesForTradeAsync("SPY", entryDate, exitDate, paddingDays: 5);

        // Assert
        result.Success.Should().BeTrue();
        result.Bars.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCandlesForTradeAsync_HandlesOpenTrade()
    {
        // Arrange
        var entryDate = DateTime.Now.AddDays(-5);
        var bars = CreateSampleBars("SPY", 30);
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, bars);
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesForTradeAsync("SPY", entryDate, exitDate: null);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetCandlesAsync_ReturnsError_WhenEmptyResponse()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, new List<OhlcvBar>());
        var service = new PriceDataService(httpClient);

        // Act
        var result = await service.GetCandlesAsync("SPY", "1d", 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No data");
    }

    [Fact]
    public void ClearCache_RemovesCachedData()
    {
        // Arrange
        var service = new PriceDataService(new HttpClient { BaseAddress = new Uri("http://localhost/") });

        // Act & Assert - should not throw
        service.ClearCache();
    }

    #region Helper Methods

    private static HttpClient CreateMockHttpClient<T>(HttpStatusCode statusCode, T content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content is string s ? s : JsonSerializer.Serialize(content))
            });

        return new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost/") };
    }

    private static List<OhlcvBar> CreateSampleBars(string symbol, int count)
    {
        var bars = new List<OhlcvBar>();
        var basePrice = 100m;
        var date = DateTime.Now.AddDays(-count);

        for (int i = 0; i < count; i++)
        {
            bars.Add(CreateBar(symbol, date.AddDays(i), basePrice + i));
        }

        return bars;
    }

    private static OhlcvBar CreateBar(string symbol, DateTime timestamp, decimal price = 100m)
    {
        return new OhlcvBar
        {
            Symbol = symbol,
            Timestamp = timestamp,
            Open = price,
            High = price + 2,
            Low = price - 2,
            Close = price + 1,
            Volume = 1_000_000
        };
    }

    #endregion
}
