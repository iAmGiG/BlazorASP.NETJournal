// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for PaperTradeService analytics and P&amp;L calculations.
/// Tests open/closed filtering, win rate, pattern stats, and regime analysis.
/// </summary>
public class PaperTradeServiceTests
{
    [Fact]
    public async Task GetOpenTrades_ReturnsOnlyOpenTrades()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateOpenTrade("Long", 450m));
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true));

        // Act
        var openTrades = service.GetOpenTrades();

        // Assert
        openTrades.Should().HaveCount(1);
        openTrades.First().IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task GetClosedTrades_ReturnsOnlyClosedTrades()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateOpenTrade("Long", 450m));
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true));

        // Act
        var closedTrades = service.GetClosedTrades();

        // Assert
        closedTrades.Should().HaveCount(1);
        closedTrades.First().IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CloseTradeAsync_UpdatesTradeWithExitDetails()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        var trade = CreateOpenTrade("Long", 450m);
        await service.LoadAsync();
        await service.AddAsync(trade);

        // Act
        await service.CloseTradeAsync(trade.Id, exitPrice: 460m, exitReason: ExitReason.Target);

        // Assert
        var closedTrade = service.GetById(trade.Id);
        closedTrade.Should().NotBeNull();
        closedTrade!.IsOpen.Should().BeFalse();
        closedTrade.ExitPrice.Should().Be(460m);
        closedTrade.ExitReason.Should().Be(ExitReason.Target);
    }

    [Fact]
    public async Task CloseTradeAsync_WithNonexistentTrade_DoesNothing()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Act
        await service.CloseTradeAsync(Guid.NewGuid(), exitPrice: 460m, exitReason: ExitReason.Target);

        // Assert - No exception thrown
        service.All.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummary_CalculatesCorrectStats()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Add 2 winning, 1 losing, 1 open
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true));  // +10 points, +2.22%
        await service.AddAsync(CreateClosedTrade("Long", 440m, 450m, profit: true));  // +10 points, +2.27%
        await service.AddAsync(CreateClosedTrade("Short", 450m, 460m, profit: false)); // -10 points, -2.22%
        await service.AddAsync(CreateOpenTrade("Long", 455m));

        // Act
        var summary = service.GetSummary();

        // Assert
        summary.TotalTrades.Should().Be(4);
        summary.OpenTrades.Should().Be(1);
        summary.ClosedTrades.Should().Be(3);
        summary.Wins.Should().Be(2);
        summary.Losses.Should().Be(1);
        summary.WinRate.Should().BeApproximately(66.67m, 0.01m);
        summary.TotalPnLPoints.Should().Be(10m); // 10 + 10 - 10
        summary.LongTrades.Should().Be(3);
        summary.ShortTrades.Should().Be(1);
    }

    [Fact]
    public async Task GetSummary_WithNoTrades_ReturnsZeros()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Act
        var summary = service.GetSummary();

        // Assert
        summary.TotalTrades.Should().Be(0);
        summary.WinRate.Should().Be(0);
        summary.TotalPnLPoints.Should().Be(0);
    }

    [Fact]
    public async Task GetStatsByPattern_GroupsByTags()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Add trades with pattern tags
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true, tags: ["gamma_flip"]));
        await service.AddAsync(CreateClosedTrade("Long", 440m, 445m, profit: true, tags: ["gamma_flip"]));
        await service.AddAsync(CreateClosedTrade("Long", 450m, 445m, profit: false, tags: ["opex_pinning"]));

        // Act
        var stats = service.GetStatsByPattern().ToList();

        // Assert
        stats.Should().HaveCount(2);

        var gammaFlipStats = stats.First(s => s.Pattern == "gamma_flip");
        gammaFlipStats.TradeCount.Should().Be(2);
        gammaFlipStats.Wins.Should().Be(2);
        gammaFlipStats.WinRate.Should().Be(100m);

        var opexStats = stats.First(s => s.Pattern == "opex_pinning");
        opexStats.TradeCount.Should().Be(1);
        opexStats.Wins.Should().Be(0);
    }

    [Fact]
    public async Task GetStatsByPattern_HandlesMultipleTagsPerTrade()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Trade with 2 tags counts toward both patterns
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true,
            tags: ["gamma_flip", "volatility"]));

        // Act
        var stats = service.GetStatsByPattern().ToList();

        // Assert
        stats.Should().HaveCount(2);
        stats.Should().Contain(s => s.Pattern == "gamma_flip" && s.TradeCount == 1);
        stats.Should().Contain(s => s.Pattern == "volatility" && s.TradeCount == 1);
    }

    [Fact]
    public async Task GetStatsByRegime_CalculatesSeparateMetrics()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Add trades in different regimes
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true, isNegativeGamma: true));   // Win: +10
        await service.AddAsync(CreateClosedTrade("Long", 450m, 440m, profit: false, isNegativeGamma: true));  // Loss: -10
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true, isNegativeGamma: false));  // Win: +10

        // Act
        var stats = service.GetStatsByRegime();

        // Assert
        stats.NegativeGamma.TradeCount.Should().Be(2);
        stats.NegativeGamma.Wins.Should().Be(1);
        stats.NegativeGamma.WinRate.Should().Be(50m);

        stats.PositiveGamma.TradeCount.Should().Be(1);
        stats.PositiveGamma.Wins.Should().Be(1);
        stats.PositiveGamma.WinRate.Should().Be(100m);
    }

    [Fact]
    public async Task GetEquityCurve_ReturnsCorrectCumulativePnL()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        // Add trades with different exit dates
        var trade1 = CreateClosedTrade("Long", 450m, 460m, profit: true); // +10
        trade1 = trade1 with { ExitDate = new DateTime(2024, 1, 1) };
        await service.AddAsync(trade1);

        var trade2 = CreateClosedTrade("Long", 440m, 445m, profit: true); // +5
        trade2 = trade2 with { ExitDate = new DateTime(2024, 1, 2) };
        await service.AddAsync(trade2);

        var trade3 = CreateClosedTrade("Long", 450m, 445m, profit: false); // -5
        trade3 = trade3 with { ExitDate = new DateTime(2024, 1, 3) };
        await service.AddAsync(trade3);

        // Act
        var curve = service.GetEquityCurve().ToList();

        // Assert
        curve.Should().HaveCount(3);
        curve[0].CumulativePnL.Should().Be(10m);
        curve[1].CumulativePnL.Should().Be(15m);
        curve[2].CumulativePnL.Should().Be(10m);
    }

    [Fact]
    public async Task Search_FindsTradesByNotes()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateOpenTrade("Long", 450m) with { Notes = "Testing gamma flip setup" });
        await service.AddAsync(CreateOpenTrade("Short", 455m) with { Notes = "OPEX week trade" });

        // Act
        var results = service.Search("gamma");

        // Assert
        results.Should().HaveCount(1);
        results.First().Notes.Should().Contain("gamma");
    }

    [Fact]
    public async Task Search_FindsTradesByTags()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateOpenTrade("Long", 450m, tags: ["gamma_flip", "high_confidence"]));
        await service.AddAsync(CreateOpenTrade("Short", 455m, tags: ["opex_pinning"]));

        // Act
        var results = service.Search("gamma");

        // Assert
        results.Should().HaveCount(1);
        results.First().Tags.Should().Contain("gamma_flip");
    }

    [Fact]
    public async Task ExportAsCsv_ReturnsValidCsv()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new PaperTradeService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateClosedTrade("Long", 450m, 460m, profit: true, tags: ["test"]));

        // Act
        var csv = service.ExportAsCsv();

        // Assert
        csv.Should().Contain("Id,CreatedAt,Direction,EntryPrice");
        csv.Should().Contain("Long");
        csv.Should().Contain("450");
        csv.Should().Contain("460");
    }

    // Helper methods
    private static PaperTrade CreateOpenTrade(string direction, decimal entryPrice, string[]? tags = null)
    {
        return new PaperTrade
        {
            Direction = direction,
            EntryPrice = entryPrice,
            TargetPrice = entryPrice + 10,
            StopLoss = entryPrice - 5,
            Tags = tags != null ? [.. tags] : [],
            IsNegativeGammaAtCreation = false,
        };
    }

    private static PaperTrade CreateClosedTrade(string direction, decimal entryPrice, decimal exitPrice,
        bool profit, string[]? tags = null, bool isNegativeGamma = false)
    {
        return new PaperTrade
        {
            Direction = direction,
            EntryPrice = entryPrice,
            ExitDate = DateTime.UtcNow,
            ExitPrice = exitPrice,
            ExitReason = profit ? ExitReason.Target : ExitReason.Stop,
            Tags = tags != null ? [.. tags] : [],
            IsNegativeGammaAtCreation = isNegativeGamma,
        };
    }
}
