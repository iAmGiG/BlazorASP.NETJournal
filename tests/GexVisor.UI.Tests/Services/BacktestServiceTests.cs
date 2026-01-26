// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for BacktestService analytics and comparison functionality.
/// Tests strategy filtering, selection management, and performance metrics.
/// </summary>
public class BacktestServiceTests
{
    private static readonly string[] _strategyOneAndThree = ["Strategy1", "Strategy3"];

    [Fact]
    public async Task GetByStrategy_FiltersCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 15m));
        await service.AddAsync(CreateBacktest("OPEX Pinning", totalReturn: 10m));
        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 20m));

        // Act
        var results = service.GetByStrategy("GammaFlip");

        // Assert
        results.Should().HaveCount(2);
        results.All(r => r.StrategyName == "GammaFlip").Should().BeTrue();
    }

    [Fact]
    public async Task GetStrategyNames_ReturnsUniqueNames()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 15m));
        await service.AddAsync(CreateBacktest("OPEX Pinning", totalReturn: 10m));
        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 20m));

        // Act
        var names = service.GetStrategyNames().ToList();

        // Assert
        names.Should().HaveCount(2);
        names.Should().Contain("GammaFlip");
        names.Should().Contain("OPEX Pinning");
    }

    [Fact]
    public async Task GetOrderedResults_ReturnsNewestFirst()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        var old = CreateBacktest("Strategy1", totalReturn: 10m) with
        {
            CreatedAt = DateTime.UtcNow.AddDays(-2),
        };
        var newest = CreateBacktest("Strategy2", totalReturn: 15m) with
        {
            CreatedAt = DateTime.UtcNow,
        };
        var middle = CreateBacktest("Strategy3", totalReturn: 12m) with
        {
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        };

        await service.AddAsync(old);
        await service.AddAsync(newest);
        await service.AddAsync(middle);

        // Act
        var ordered = service.GetOrderedResults().ToList();

        // Assert
        ordered[0].StrategyName.Should().Be("Strategy2");
        ordered[1].StrategyName.Should().Be("Strategy3");
        ordered[2].StrategyName.Should().Be("Strategy1");
    }

    [Fact]
    public async Task ToggleSelectionAsync_TogglesIsSelected()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        var backtest = CreateBacktest("GammaFlip", totalReturn: 15m);
        await service.LoadAsync();
        await service.AddAsync(backtest);

        // Act - Toggle on
        await service.ToggleSelectionAsync(backtest.Id);
        var selected = service.GetById(backtest.Id);
        selected!.IsSelected.Should().BeTrue();

        // Act - Toggle off
        await service.ToggleSelectionAsync(backtest.Id);
        var unselected = service.GetById(backtest.Id);
        unselected!.IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task GetSelectedResults_ReturnsOnlySelected()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        var bt1 = CreateBacktest("Strategy1", totalReturn: 10m);
        var bt2 = CreateBacktest("Strategy2", totalReturn: 15m);
        var bt3 = CreateBacktest("Strategy3", totalReturn: 12m);

        await service.AddAsync(bt1);
        await service.AddAsync(bt2);
        await service.AddAsync(bt3);

        // Act - Select 2 of them
        await service.ToggleSelectionAsync(bt1.Id);
        await service.ToggleSelectionAsync(bt3.Id);

        var selected = service.GetSelectedResults().ToList();

        // Assert
        selected.Should().HaveCount(2);
        selected.Select(s => s.StrategyName).Should().BeEquivalentTo(_strategyOneAndThree);
    }

    [Fact]
    public async Task ClearSelectionsAsync_DeselectsAll()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        var bt1 = CreateBacktest("Strategy1", totalReturn: 10m);
        var bt2 = CreateBacktest("Strategy2", totalReturn: 15m);

        await service.AddAsync(bt1);
        await service.AddAsync(bt2);
        await service.ToggleSelectionAsync(bt1.Id);
        await service.ToggleSelectionAsync(bt2.Id);

        // Act
        await service.ClearSelectionsAsync();

        // Assert
        service.GetSelectedResults().Should().BeEmpty();
    }

    [Fact]
    public async Task GetComparison_CalculatesMetrics()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        var bt1 = CreateBacktest("Strategy1", totalReturn: 10m, totalTrades: 100, winningTrades: 60);
        var bt2 = CreateBacktest("Strategy2", totalReturn: 20m, totalTrades: 100, winningTrades: 70);

        await service.AddAsync(bt1);
        await service.AddAsync(bt2);
        await service.ToggleSelectionAsync(bt1.Id);
        await service.ToggleSelectionAsync(bt2.Id);

        // Act
        var comparison = service.GetComparison();

        // Assert
        comparison.Results.Should().HaveCount(2);
        comparison.AvgReturn.Should().Be(15m);
        comparison.BestReturn.Should().Be(20m);
        comparison.WorstReturn.Should().Be(10m);
        comparison.AvgWinRate.Should().Be(65m); // (60% + 70%) / 2
    }

    [Fact]
    public async Task GetSummary_CalculatesAggregateStats()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 15m, totalTrades: 100, winningTrades: 60, sharpeRatio: 1.5m));
        await service.AddAsync(CreateBacktest("OPEX Pinning", totalReturn: 10m, totalTrades: 100, winningTrades: 50, sharpeRatio: 1.0m));
        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 20m, totalTrades: 100, winningTrades: 70, sharpeRatio: 2.0m));

        // Act
        var summary = service.GetSummary();

        // Assert
        summary.TotalResults.Should().Be(3);
        summary.UniqueStrategies.Should().Be(2);
        summary.AvgReturn.Should().Be(15m);
        summary.BestReturn.Should().Be(20m);
        summary.WorstReturn.Should().Be(10m);
        summary.AvgSharpe.Should().Be(1.5m); // (1.5 + 1.0 + 2.0) / 3
    }

    [Fact]
    public void GetSummary_WithNoResults_ReturnsZeros()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);

        // Act
        var summary = service.GetSummary();

        // Assert
        summary.TotalResults.Should().Be(0);
        summary.AvgReturn.Should().Be(0);
        summary.AvgSharpe.Should().Be(0);
    }

    [Fact]
    public async Task GetRegimePerformance_CalculatesRegimeMetrics()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateBacktest("Strategy1", totalReturn: 15m,
            returnInPositiveGamma: 10m, returnInNegativeGamma: 5m));
        await service.AddAsync(CreateBacktest("Strategy2", totalReturn: 20m,
            returnInPositiveGamma: 15m, returnInNegativeGamma: 10m));

        // Act
        var performance = service.GetRegimePerformance();

        // Assert
        performance.AvgPositiveGammaReturn.Should().Be(12.5m); // (10 + 15) / 2
        performance.AvgNegativeGammaReturn.Should().Be(7.5m);  // (5 + 10) / 2
        performance.ResultsWithRegimeData.Should().Be(2);
    }

    [Fact]
    public void GetRegimePerformance_WithNoData_ReturnsZeros()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);

        // Act
        var performance = service.GetRegimePerformance();

        // Assert
        performance.AvgPositiveGammaReturn.Should().Be(0);
        performance.AvgNegativeGammaReturn.Should().Be(0);
        performance.ResultsWithRegimeData.Should().Be(0);
    }

    [Fact]
    public async Task Search_FindsByStrategyName()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateBacktest("GammaFlip Strategy", totalReturn: 15m));
        await service.AddAsync(CreateBacktest("OPEX Pinning", totalReturn: 10m));

        // Act
        var results = service.Search("Gamma");

        // Assert
        results.Should().HaveCount(1);
        results.First().StrategyName.Should().Contain("Gamma");
    }

    [Fact]
    public async Task Search_FindsByTags()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateBacktest("Strategy1", totalReturn: 15m, tags: ["gamma", "mechanical"]));
        await service.AddAsync(CreateBacktest("Strategy2", totalReturn: 10m, tags: ["opex"]));

        // Act
        var results = service.Search("gamma");

        // Assert
        results.Should().HaveCount(1);
        results.First().Tags.Should().Contain("gamma");
    }

    [Fact]
    public async Task ExportAsCsv_ReturnsValidCsv()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new BacktestService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateBacktest("GammaFlip", totalReturn: 15m, tags: ["test"]));

        // Act
        var csv = service.ExportAsCsv();

        // Assert
        csv.Should().Contain("Id,CreatedAt,StrategyName");
        csv.Should().Contain("GammaFlip");
        csv.Should().Contain("15");
    }

    // Helper method
    private static BacktestResult CreateBacktest(string strategyName, decimal totalReturn,
        int totalTrades = 100, int winningTrades = 60, decimal? sharpeRatio = null,
        decimal? returnInPositiveGamma = null, decimal? returnInNegativeGamma = null,
        string[]? tags = null)
    {
        return new BacktestResult
        {
            StrategyName = strategyName,
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            TotalReturn = totalReturn,
            MaxDrawdown = -5m,
            SharpeRatio = sharpeRatio,
            ReturnInPositiveGamma = returnInPositiveGamma,
            ReturnInNegativeGamma = returnInNegativeGamma,
            Tags = tags != null ? [.. tags] : [],
        };
    }
}
