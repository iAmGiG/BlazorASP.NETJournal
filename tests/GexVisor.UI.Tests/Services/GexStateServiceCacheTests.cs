// Copyright (c) GexVisor. All rights reserved.

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;
using Moq.Protected;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for GexStateService offline caching functionality.
/// </summary>
public class GexStateServiceCacheTests
{
    private readonly Mock<ILocalStorageService> _mockStorage;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly GexStateService _service;

    public GexStateServiceCacheTests()
    {
        _mockStorage = new Mock<ILocalStorageService>();
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://test/"),
        };
        _service = new GexStateService(_httpClient, _mockStorage.Object);
    }

    [Fact]
    public async Task RefreshLiveDataAsync_OnSuccess_CachesData()
    {
        // Arrange
        var gexResult = CreateTestGexResult();
        SetupSuccessResponse(gexResult);

        // Act
        await _service.RefreshLiveDataAsync("SPY");

        // Assert
        _mockStorage.Verify(
            s => s.SetAsync(
                It.Is<string>(k => k == "gexvisor.liveGex.SPY"),
                It.IsAny<CachedGexData>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshLiveDataAsync_OnSuccess_SetsFromCacheFalse()
    {
        // Arrange
        var gexResult = CreateTestGexResult();
        SetupSuccessResponse(gexResult);

        // Act
        await _service.RefreshLiveDataAsync("SPY");

        // Assert
        _service.IsLiveDataFromCache.Should().BeFalse();
        _service.IsLiveDataStale.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshLiveDataAsync_OnApiError_FallsBackToCache()
    {
        // Arrange
        var cachedData = new CachedGexData
        {
            Data = CreateTestGexResult(),
            Symbol = "SPY",
            CachedAt = DateTime.UtcNow.AddMinutes(-2),
            FetchedAt = DateTime.UtcNow.AddMinutes(-2),
            IsCached = false,
        };
        SetupErrorResponse(HttpStatusCode.InternalServerError);
        _mockStorage
            .Setup(s => s.GetAsync<CachedGexData>("gexvisor.liveGex.SPY"))
            .ReturnsAsync(cachedData);

        // Act
        await _service.RefreshLiveDataAsync("SPY");

        // Assert
        _service.IsLiveDataFromCache.Should().BeTrue();
        _service.LiveGexData.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshLiveDataAsync_OnNetworkError_FallsBackToCache()
    {
        // Arrange
        var cachedData = new CachedGexData
        {
            Data = CreateTestGexResult(),
            Symbol = "SPY",
            CachedAt = DateTime.UtcNow.AddMinutes(-2),
            FetchedAt = DateTime.UtcNow.AddMinutes(-2),
            IsCached = false,
        };
        SetupNetworkError();
        _mockStorage
            .Setup(s => s.GetAsync<CachedGexData>("gexvisor.liveGex.SPY"))
            .ReturnsAsync(cachedData);

        // Act
        await _service.RefreshLiveDataAsync("SPY");

        // Assert
        _service.IsLiveDataFromCache.Should().BeTrue();
        _service.LiveDataError.Should().Contain("using cached data");
    }

    [Fact]
    public async Task RefreshLiveDataAsync_OnError_NoCacheAvailable_SetsNullData()
    {
        // Arrange
        SetupErrorResponse(HttpStatusCode.InternalServerError);
        _mockStorage
            .Setup(s => s.GetAsync<CachedGexData>(It.IsAny<string>()))
            .ReturnsAsync((CachedGexData?)null);

        // Act
        await _service.RefreshLiveDataAsync("SPY");

        // Assert
        _service.LiveGexData.Should().BeNull();
        _service.IsLiveDataFromCache.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCachedGexDataAsync_ReturnsNullWhenNoCache()
    {
        // Arrange
        _mockStorage
            .Setup(s => s.GetAsync<CachedGexData>(It.IsAny<string>()))
            .ReturnsAsync((CachedGexData?)null);

        // Act
        var result = await _service.LoadCachedGexDataAsync("SPY");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadCachedGexDataAsync_ReturnsCachedData()
    {
        // Arrange
        var cachedData = new CachedGexData
        {
            Data = CreateTestGexResult(),
            Symbol = "SPY",
            CachedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
            IsCached = false,
        };
        _mockStorage
            .Setup(s => s.GetAsync<CachedGexData>("gexvisor.liveGex.SPY"))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _service.LoadCachedGexDataAsync("SPY");

        // Assert
        result.Should().NotBeNull();
        result!.IsCached.Should().BeTrue();
        result.Symbol.Should().Be("SPY");
    }

    [Fact]
    public async Task ClearCachedGexDataAsync_SpecificSymbol_RemovesKey()
    {
        // Arrange & Act
        await _service.ClearCachedGexDataAsync("SPY");

        // Assert
        _mockStorage.Verify(
            s => s.RemoveAsync("gexvisor.liveGex.SPY"),
            Times.Once);
    }

    [Fact]
    public async Task ClearCachedGexDataAsync_AllSymbols_RemovesMatchingKeys()
    {
        // Arrange
        _mockStorage
            .Setup(s => s.GetKeysAsync())
            .ReturnsAsync(new[]
            {
                "gexvisor.liveGex.SPY",
                "gexvisor.liveGex.QQQ",
                "gexvisor.settings",
                "other.key",
            });

        // Act
        await _service.ClearCachedGexDataAsync();

        // Assert
        _mockStorage.Verify(s => s.RemoveAsync("gexvisor.liveGex.SPY"), Times.Once);
        _mockStorage.Verify(s => s.RemoveAsync("gexvisor.liveGex.QQQ"), Times.Once);
        _mockStorage.Verify(s => s.RemoveAsync("gexvisor.settings"), Times.Never);
        _mockStorage.Verify(s => s.RemoveAsync("other.key"), Times.Never);
    }

    [Fact]
    public async Task RefreshLiveDataAsync_SetsLiveDataFetchedAt()
    {
        // Arrange
        var gexResult = CreateTestGexResult();
        SetupSuccessResponse(gexResult);
        var before = DateTime.UtcNow;

        // Act
        await _service.RefreshLiveDataAsync("SPY");

        // Assert
        _service.LiveDataFetchedAt.Should().NotBeNull();
        _service.LiveDataFetchedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task RefreshLiveDataAsync_SymbolNormalization_UsesUppercase()
    {
        // Arrange
        var gexResult = CreateTestGexResult();
        SetupSuccessResponse(gexResult);

        // Act
        await _service.RefreshLiveDataAsync("spy");

        // Assert
        _mockStorage.Verify(
            s => s.SetAsync(
                It.Is<string>(k => k == "gexvisor.liveGex.SPY"),
                It.IsAny<CachedGexData>()),
            Times.Once);
    }

    private static GexCalculationResult CreateTestGexResult()
    {
        return new GexCalculationResult
        {
            Symbol = "SPY",
            SpotPrice = 450m,
            TotalGex = 5.5m,
            CallGex = 8.0m,
            PutGex = 2.5m,
            ZeroGammaLevel = 445m,
            Regime = GexRegime.LongGamma,
            StrikeGammas = new List<StrikeGamma>
            {
                new() { StrikePrice = 450m, CallGex = 2m, PutGex = 1m },
            },
            Timestamp = DateTime.UtcNow,
        };
    }

    private void SetupSuccessResponse(GexCalculationResult result)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(result),
        };

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupErrorResponse(HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage(statusCode);

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupNetworkError()
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
    }
}
