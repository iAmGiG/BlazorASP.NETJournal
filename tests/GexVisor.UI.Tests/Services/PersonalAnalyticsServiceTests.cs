// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for PersonalAnalyticsService.
/// Tests performance calculations, heatmaps, habit metrics, and emotional correlation.
/// </summary>
public class PersonalAnalyticsServiceTests
{
    private readonly Mock<ILocalStorageService> _mockStorage;
    private readonly TradeLogService _tradeLogService;
    private readonly PersonalAnalyticsService _service;

    public PersonalAnalyticsServiceTests()
    {
        _mockStorage = new Mock<ILocalStorageService>();
        _tradeLogService = new TradeLogService(_mockStorage.Object);
        _service = new PersonalAnalyticsService(_tradeLogService, _mockStorage.Object);
    }

    [Fact]
    public void CalculatePerformance_WithNoTrades_ReturnsEmptyMetrics()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _service.CalculatePerformance(start, end);

        // Assert
        result.TradeCount.Should().Be(0);
        result.TotalPnL.Should().Be(0);
        result.WinRate.Should().Be(0);
    }

    [Fact]
    public async Task CalculatePerformance_WithTrades_CalculatesCorrectly()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("SPY", 100m, 110m, DateTime.UtcNow.AddDays(-5)),
            CreateClosedTrade("QQQ", 100m, 90m, DateTime.UtcNow.AddDays(-3)),
            CreateClosedTrade("IWM", 100m, 120m, DateTime.UtcNow.AddDays(-1)),
        ]);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _service.CalculatePerformance(start, end);

        // Assert
        result.TradeCount.Should().Be(3);
        result.WinningTrades.Should().Be(2);
        result.LosingTrades.Should().Be(1);
        result.WinRate.Should().BeApproximately(66.67m, 0.1m);
        result.TotalPnL.Should().Be(2000m); // (10 + 20 - 10) * 100 contracts
    }

    [Fact]
    public async Task CalculatePerformance_CalculatesProfitFactor()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("SPY", 100m, 120m, DateTime.UtcNow.AddDays(-5)), // +$2000
            CreateClosedTrade("QQQ", 100m, 90m, DateTime.UtcNow.AddDays(-3)),  // -$1000
        ]);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _service.CalculatePerformance(start, end);

        // Assert
        result.ProfitFactor.Should().Be(2m); // 2000 / 1000 = 2
    }

    [Fact]
    public async Task CalculatePerformance_FiltersTradesByDateRange()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("SPY", 100m, 110m, DateTime.UtcNow.AddDays(-60)), // Outside range
            CreateClosedTrade("QQQ", 100m, 120m, DateTime.UtcNow.AddDays(-5)),  // Inside range
        ]);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _service.CalculatePerformance(start, end);

        // Assert
        result.TradeCount.Should().Be(1);
        result.TotalPnL.Should().Be(2000m); // Only QQQ trade
    }

    [Fact]
    public async Task CalculateHabitMetrics_CalculatesStreaksCorrectly()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 110m, DateTime.UtcNow.AddDays(-10)), // Win
            CreateClosedTrade("B", 100m, 115m, DateTime.UtcNow.AddDays(-9)),  // Win
            CreateClosedTrade("C", 100m, 120m, DateTime.UtcNow.AddDays(-8)),  // Win
            CreateClosedTrade("D", 100m, 90m, DateTime.UtcNow.AddDays(-7)),   // Loss
            CreateClosedTrade("E", 100m, 85m, DateTime.UtcNow.AddDays(-6)),   // Loss
            CreateClosedTrade("F", 100m, 110m, DateTime.UtcNow.AddDays(-5)),  // Win
        ]);

        // Act
        var result = _service.CalculateHabitMetrics();

        // Assert
        result.LongestWinStreak.Should().Be(3);
        result.LongestLossStreak.Should().Be(2);
        result.IsCurrentStreakWinning.Should().BeTrue();
        result.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public async Task CalculateHabitMetrics_CalculatesAverageTradesPerDay()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        var baseDate = DateTime.UtcNow.AddDays(-2);
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 110m, baseDate),
            CreateClosedTrade("B", 100m, 110m, baseDate),
            CreateClosedTrade("C", 100m, 110m, baseDate.AddDays(1)),
        ]);

        // Act
        var result = _service.CalculateHabitMetrics();

        // Assert
        result.AverageTradesPerDay.Should().BeApproximately(1.5m, 0.01m); // 3 trades / 2 trading days
        result.TotalTradingDays.Should().Be(2);
    }

    [Fact]
    public async Task GenerateHeatmap_GroupsByDayAndHour()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        var monday9am = GetNextWeekday(DateTime.UtcNow, DayOfWeek.Monday).Date.AddHours(9);
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 110m, monday9am),
            CreateClosedTrade("B", 100m, 120m, monday9am.AddMinutes(30)),
        ]);

        // Act
        var result = _service.GenerateHeatmap(4);

        // Assert
        result.Should().ContainSingle();
        var cell = result.First();
        cell.DayOfWeek.Should().Be((int)DayOfWeek.Monday);
        cell.HourOfDay.Should().Be(9);
        cell.TradeCount.Should().Be(2);
        cell.AveragePnL.Should().Be(1500m); // (1000 + 2000) / 2
    }

    [Fact]
    public async Task SaveTrackingDataAsync_PersistsData()
    {
        // Arrange
        await _service.LoadTrackingDataAsync();
        var tradeId = Guid.NewGuid();
        var trackingData = new TradeTrackingData
        {
            TradeId = tradeId,
            Emotion = EmotionalState.Confident,
            ConfidenceLevel = 8,
            LessonLearned = "Wait for confirmation",
        };

        // Act
        await _service.SaveTrackingDataAsync(trackingData);

        // Assert
        _mockStorage.Verify(
            s => s.SetAsync(
                AppConstants.Storage.TradeTrackingData,
                It.Is<List<TradeTrackingData>>(list => list.Any(t => t.TradeId == tradeId))),
            Times.Once);
    }

    [Fact]
    public async Task GetTrackingData_ReturnsStoredData()
    {
        // Arrange
        var tradeId = Guid.NewGuid();
        var trackingData = new TradeTrackingData
        {
            TradeId = tradeId,
            Emotion = EmotionalState.Anxious,
            ConfidenceLevel = 5,
            LessonLearned = "Don't chase",
        };

        _mockStorage.Setup(s => s.GetAsync<List<TradeTrackingData>>(AppConstants.Storage.TradeTrackingData))
            .ReturnsAsync([trackingData]);

        await _service.LoadTrackingDataAsync();

        // Act
        var result = _service.GetTrackingData(tradeId);

        // Assert
        result.Should().NotBeNull();
        result!.Emotion.Should().Be(EmotionalState.Anxious);
        result.ConfidenceLevel.Should().Be(5);
    }

    [Fact]
    public async Task AnalyzeEmotionalCorrelation_GroupsByEmotion()
    {
        // Arrange
        await _tradeLogService.LoadAsync();

        var trade1 = CreateClosedTrade("A", 100m, 120m, DateTime.UtcNow.AddDays(-5));
        var trade2 = CreateClosedTrade("B", 100m, 80m, DateTime.UtcNow.AddDays(-4));
        await _tradeLogService.AddRangeAsync([trade1, trade2]);

        // Add tracking data with emotions
        var trackingList = new List<TradeTrackingData>
        {
            new() { TradeId = trade1.Id, Emotion = EmotionalState.Confident, ConfidenceLevel = 8 },
            new() { TradeId = trade2.Id, Emotion = EmotionalState.Anxious, ConfidenceLevel = 3 },
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradeTrackingData>>(AppConstants.Storage.TradeTrackingData))
            .ReturnsAsync(trackingList);

        await _service.LoadTrackingDataAsync();

        // Act
        var result = _service.AnalyzeEmotionalCorrelation();

        // Assert
        result.Should().HaveCount(2);

        var confidentAnalysis = result.First(e => e.State == EmotionalState.Confident);
        confidentAnalysis.TradeCount.Should().Be(1);
        confidentAnalysis.WinRate.Should().Be(100m);

        var anxiousAnalysis = result.First(e => e.State == EmotionalState.Anxious);
        anxiousAnalysis.TradeCount.Should().Be(1);
        anxiousAnalysis.WinRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetRecentLessons_ReturnsLessonsWithTradeInfo()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        var trade = CreateClosedTrade("SPY", 100m, 110m, DateTime.UtcNow.AddDays(-5));
        await _tradeLogService.AddAsync(trade);

        var trackingList = new List<TradeTrackingData>
        {
            new() { TradeId = trade.Id, LessonLearned = "Always use stop loss", CreatedAt = DateTime.UtcNow },
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradeTrackingData>>(AppConstants.Storage.TradeTrackingData))
            .ReturnsAsync(trackingList);

        await _service.LoadTrackingDataAsync();

        // Act
        var result = _service.GetRecentLessons(5);

        // Assert
        result.Should().HaveCount(1);
        result.First().Lesson.Should().Be("Always use stop loss");
        result.First().Ticker.Should().Be("SPY");
        result.First().TradePnL.Should().Be(1000m);
    }

    [Fact]
    public async Task GetPerformanceTrend_ReturnsWeeklyMetrics()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        var baseDate = DateTime.UtcNow.AddDays(-21);
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 110m, baseDate),          // Week 1
            CreateClosedTrade("B", 100m, 120m, baseDate.AddDays(7)), // Week 2
            CreateClosedTrade("C", 100m, 130m, baseDate.AddDays(14)), // Week 3
        ]);

        var start = DateOnly.FromDateTime(baseDate);
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _service.GetPerformanceTrend(start, end, TrendPeriod.Weekly);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task OnAnalyticsChanged_TriggersWhenTradesChange()
    {
        // Arrange
        var eventTriggered = false;
        _service.OnAnalyticsChanged += () => eventTriggered = true;
        await _tradeLogService.LoadAsync();

        // Act - Add a trade to trigger OnTradesChanged which should cascade to OnAnalyticsChanged
        await _tradeLogService.AddAsync(CreateClosedTrade("SPY", 100m, 110m, DateTime.UtcNow));

        // Assert
        eventTriggered.Should().BeTrue();
    }

    // === Helper Methods ===
    private static OptionsLog CreateClosedTrade(string ticker, decimal entry, decimal exit, DateTime date)
    {
        return new OptionsLog
        {
            Ticker = ticker,
            EntryPrice = entry,
            ExitPrice = exit,
            Quantity = 1,
            ContractMultiplier = 100,
            OptionTradeType = OptionsLog.TradeType.BTO,
            TradeDirection = TradeLog.Type.Long,
            StrikePrice = entry,
            CreatedDate = date,
            ExpirationDate = date.AddDays(30),
        };
    }

    private static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
    {
        var daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
        return start.AddDays(daysToAdd == 0 ? -7 : daysToAdd - 7);
    }
}
