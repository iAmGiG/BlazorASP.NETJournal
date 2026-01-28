// Copyright (c) GexVisor. All rights reserved.

using GexVisor.Api.Services;
using GexVisor.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace GexVisor.Api.Tests;

public class HistoricalBackfillServiceTests : IDisposable
{
    private static readonly string[] _singleSymbol = ["SPY"];
    private static readonly string[] _multipleSymbols = ["SPY", "QQQ", "IWM"];
    private static readonly string[] _spySymbol = ["SPY"];
    private static readonly string[] _qqqSymbol = ["QQQ"];

    private readonly Mock<IGexCalculationService> _mockGexService;
    private readonly Mock<IMarketDataService> _mockMarketDataService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly HistoricalBackfillService _service;

    public HistoricalBackfillServiceTests()
    {
        _mockGexService = new Mock<IGexCalculationService>();
        _mockMarketDataService = new Mock<IMarketDataService>();
        _mockCacheService = new Mock<ICacheService>();

        _service = new HistoricalBackfillService(
            _mockGexService.Object,
            _mockMarketDataService.Object,
            _mockCacheService.Object,
            Mock.Of<ILogger<HistoricalBackfillService>>());
    }

    [Fact]
    public async Task StartBackfill_ValidRequest_ReturnsJobId()
    {
        // Arrange
        var symbols = _singleSymbol;
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 5);

        // Act
        var result = await _service.StartBackfillAsync(symbols, startDate, endDate);

        // Assert
        Assert.True(result.Started);
        Assert.NotEmpty(result.JobId);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task StartBackfill_NoSymbols_ReturnsFail()
    {
        // Arrange
        var symbols = Array.Empty<string>();
        var startDate = new DateOnly(2024, 1, 1);

        // Act
        var result = await _service.StartBackfillAsync(symbols, startDate);

        // Assert
        Assert.False(result.Started);
        Assert.Equal("No symbols provided", result.Error);
    }

    [Fact]
    public async Task StartBackfill_StartDateAfterEndDate_ReturnsFail()
    {
        // Arrange
        var symbols = _singleSymbol;
        var startDate = new DateOnly(2024, 12, 31);
        var endDate = new DateOnly(2024, 1, 1);

        // Act
        var result = await _service.StartBackfillAsync(symbols, startDate, endDate);

        // Assert
        Assert.False(result.Started);
        Assert.Contains("before or equal to end date", result.Error);
    }

    [Fact]
    public async Task GetStatus_ExistingJob_ReturnsStatus()
    {
        // Arrange
        var symbols = _singleSymbol;
        var startDate = new DateOnly(2024, 1, 1);
        var jobResult = await _service.StartBackfillAsync(symbols, startDate);

        // Act
        var status = _service.GetStatus(jobResult.JobId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(jobResult.JobId, status.JobId);
        Assert.Contains("SPY", status.Symbols);
        Assert.Equal(startDate, status.StartDate);
    }

    [Fact]
    public void GetStatus_NonExistentJob_ReturnsNull()
    {
        // Act
        var status = _service.GetStatus("nonexistent-job-id");

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task GetAllStatuses_MultipleJobs_ReturnsAll()
    {
        // Arrange
        await _service.StartBackfillAsync(_spySymbol, new DateOnly(2024, 1, 1));
        await _service.StartBackfillAsync(_qqqSymbol, new DateOnly(2024, 2, 1));

        // Act
        var statuses = _service.GetAllStatuses();

        // Assert
        Assert.Equal(2, statuses.Count);
    }

    [Fact]
    public async Task StopBackfill_RunningJob_Stops()
    {
        // Arrange
        var jobResult = await _service.StartBackfillAsync(
            _spySymbol,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31)); // Long-running job

        // Give it a moment to start processing
        await Task.Delay(100);

        // Act
        var stopped = _service.StopBackfill(jobResult.JobId);

        // Assert
        Assert.True(stopped);

        var status = _service.GetStatus(jobResult.JobId);
        Assert.NotNull(status);
        Assert.Equal(BackfillState.Stopped, status.State);
    }

    [Fact]
    public void StopBackfill_NonExistentJob_ReturnsFalse()
    {
        // Act
        var stopped = _service.StopBackfill("nonexistent-job-id");

        // Assert
        Assert.False(stopped);
    }

    [Fact]
    public async Task BackfillJob_ProcessesMultipleSymbols()
    {
        // Arrange
        var symbols = _multipleSymbols;

        // Tuesday
        var startDate = new DateOnly(2024, 1, 2);

        // Thursday (3 trading days)
        var endDate = new DateOnly(2024, 1, 4);

        SetupMockMarketData("SPY", 450m);
        SetupMockMarketData("QQQ", 380m);
        SetupMockMarketData("IWM", 200m);

        SetupMockGexCalculation("SPY");
        SetupMockGexCalculation("QQQ");
        SetupMockGexCalculation("IWM");

        // Act
        var jobResult = await _service.StartBackfillAsync(symbols, startDate, endDate);

        // Wait for processing (short date range)
        await Task.Delay(500);

        var status = _service.GetStatus(jobResult.JobId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(3, status.Symbols.Count);

        // Should process 3 symbols * 3 trading days = 9 total
        Assert.True(status.CompletedDays > 0, "Should have processed some days");
    }

    [Fact]
    public async Task BackfillJob_SkipsWeekends()
    {
        // Arrange - Include a weekend
        var symbols = _singleSymbol;

        // Friday
        var startDate = new DateOnly(2024, 1, 5);

        // Monday (includes Sat+Sun)
        var endDate = new DateOnly(2024, 1, 8);

        SetupMockMarketData("SPY", 450m);
        SetupMockGexCalculation("SPY");

        // Act
        var jobResult = await _service.StartBackfillAsync(symbols, startDate, endDate);
        await Task.Delay(300);

        var status = _service.GetStatus(jobResult.JobId);

        // Assert
        Assert.NotNull(status);

        // Should only process Fri + Mon = 2 days (not 4)
        Assert.True(status.CompletedDays <= 2, $"Should process max 2 trading days, got {status.CompletedDays}");
    }

    [Fact]
    public async Task BackfillJob_HandlesFailedDates()
    {
        // Arrange
        var symbols = _singleSymbol;
        var startDate = new DateOnly(2024, 1, 2);
        var endDate = new DateOnly(2024, 1, 4);

        // Setup mock to fail on market data fetch
        _mockMarketDataService
            .Setup(s => s.GetBarsAsync(It.IsAny<string>(), It.IsAny<BarTimeframe>(), It.IsAny<int>()))
            .ReturnsAsync(MarketDataResult<List<OhlcvBar>>.Fail("API error"));

        // Act
        var jobResult = await _service.StartBackfillAsync(symbols, startDate, endDate);
        await Task.Delay(300);

        var status = _service.GetStatus(jobResult.JobId);

        // Assert
        Assert.NotNull(status);

        // Job should continue despite failures
        Assert.True(status.State == BackfillState.Running || status.State == BackfillState.Completed);
    }

    [Fact]
    public async Task BackfillJob_StoresInCacheWithCorrectKey()
    {
        // Arrange
        var symbols = _singleSymbol;
        var startDate = new DateOnly(2024, 1, 2);

        // Single day
        var endDate = new DateOnly(2024, 1, 2);

        // Setup market data with timestamp that matches the target date
        var bars = new List<OhlcvBar>
        {
            new()
            {
                Symbol = "SPY",
                Timestamp = new DateTime(2024, 1, 2, 16, 0, 0, DateTimeKind.Utc),
                Open = 449m,
                High = 451m,
                Low = 448m,
                Close = 450m,
                Volume = 1000000,
            },
        };

        _mockMarketDataService
            .Setup(s => s.GetBarsAsync("SPY", BarTimeframe.Day, It.IsAny<int>()))
            .ReturnsAsync(MarketDataResult<List<OhlcvBar>>.Ok(bars, MarketDataProvider.Alpaca));

        SetupMockGexCalculation("SPY");

        // Act
        var jobResult = await _service.StartBackfillAsync(symbols, startDate, endDate);

        // Wait longer for background processing
        await Task.Delay(1000);

        // Assert - Verify cache was called with correct key pattern
        _mockCacheService.Verify(
            c => c.SetAsync(
                It.Is<string>(key => key.StartsWith("gex:historical:SPY:")),
                It.IsAny<GexCalculationResult>(),
                It.Is<TimeSpan>(ttl => ttl.TotalDays > 365)), // Long TTL for historical data
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResumeBackfill_StoppedJob_Resumes()
    {
        // Arrange
        var jobResult = await _service.StartBackfillAsync(
            _spySymbol,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        await Task.Delay(100);
        _service.StopBackfill(jobResult.JobId);
        await Task.Delay(100);

        // Act
        var resumed = await _service.ResumeBackfillAsync(jobResult.JobId);

        // Assert
        Assert.True(resumed);

        var status = _service.GetStatus(jobResult.JobId);
        Assert.NotNull(status);
        Assert.Equal(BackfillState.Running, status.State);
    }

    [Fact]
    public async Task ResumeBackfill_NonExistentJob_ReturnsFalse()
    {
        // Act
        var resumed = await _service.ResumeBackfillAsync("nonexistent-job-id");

        // Assert
        Assert.False(resumed);
    }

    private void SetupMockMarketData(string symbol, decimal closePrice)
    {
        var bars = new List<OhlcvBar>
        {
            new()
            {
                Symbol = symbol,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Open = closePrice - 1,
                High = closePrice + 1,
                Low = closePrice - 2,
                Close = closePrice,
                Volume = 1000000,
            },
        };

        _mockMarketDataService
            .Setup(s => s.GetBarsAsync(symbol, BarTimeframe.Day, It.IsAny<int>()))
            .ReturnsAsync(MarketDataResult<List<OhlcvBar>>.Ok(bars, MarketDataProvider.Alpaca));
    }

    private void SetupMockGexCalculation(string symbol)
    {
        var gexResult = new GexCalculationResult
        {
            Symbol = symbol,
            SpotPrice = 450m,
            TotalGex = 1000000m,
            CallGex = 600000m,
            PutGex = 400000m,
            ZeroGammaLevel = 448m,
            Regime = GexRegime.LongGamma,
            StrikeGammas = new List<StrikeGamma>
            {
                new()
                {
                    StrikePrice = 450m,
                    CallGex = 600000m,
                    PutGex = 400000m,
                    ContractsCount = 1800,
                    TotalOpenInterest = 1800,
                },
            },
            Timestamp = DateTime.UtcNow,
        };

        _mockGexService
            .Setup(s => s.CalculateGexAsync(symbol, It.IsAny<decimal>()))
            .ReturnsAsync(OptionsChainResult<GexCalculationResult>.Ok(gexResult, MarketDataProvider.AlphaVantage));
    }

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }
}
