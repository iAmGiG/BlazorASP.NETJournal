using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Services;
using Moq;
using Xunit;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for TradeLogService.
/// Tests CRUD operations, filtering, analytics, search, and export functionality.
/// </summary>
public class TradeLogServiceTests
{
    [Fact]
    public async Task AddAsync_AddsTradeToCollection()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);

        // Act
        await service.AddAsync(trade);

        // Assert
        var allTrades = service.GetAllTrades();
        allTrades.Should().HaveCount(1);
        allTrades.First().Id.Should().Be(trade.Id);
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleTrades()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trades = new List<OptionsLog>
        {
            CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m),
            CreateSampleTrade("QQQ", OptionsLog.TradeType.STO, 380m),
            CreateSampleTrade("IWM", OptionsLog.TradeType.BTC, 200m)
        };

        // Act
        await service.AddRangeAsync(trades);

        // Assert
        var allTrades = service.GetAllTrades();
        allTrades.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTrade()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        await service.AddAsync(trade);

        // Act
        await service.DeleteAsync(trade.Id);

        // Assert
        service.GetAllTrades().Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsCorrectTrade()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        await service.LoadAsync();
        await service.AddAsync(trade);

        // Act
        var result = service.GetById(trade.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(trade.Id);
        result.Ticker.Should().Be("SPY");
    }

    [Fact]
    public async Task GetById_WithNonexistentId_ReturnsNull()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();

        // Act
        var result = service.GetById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTradesByDateRange_FiltersCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();

        var jan1 = new DateTime(2024, 1, 1, 10, 0, 0);
        var jan15 = new DateTime(2024, 1, 15, 10, 0, 0);
        var feb1 = new DateTime(2024, 2, 1, 10, 0, 0);

        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m, jan1));
        await service.AddAsync(CreateSampleTrade("QQQ", OptionsLog.TradeType.BTO, 380m, jan15));
        await service.AddAsync(CreateSampleTrade("IWM", OptionsLog.TradeType.BTO, 200m, feb1));

        // Act
        var result = service.GetTradesByDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31)
        );

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Ticker == "SPY");
        result.Should().Contain(t => t.Ticker == "QQQ");
        result.Should().NotContain(t => t.Ticker == "IWM");
    }

    [Fact]
    public async Task GetTradesBySymbol_FiltersCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m));
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.STC, 455m));
        await service.AddAsync(CreateSampleTrade("QQQ", OptionsLog.TradeType.BTO, 380m));

        // Act
        var result = service.GetTradesBySymbol("SPY");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Ticker == "SPY");
    }

    [Fact]
    public async Task GetTradesByType_FiltersCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m));
        await service.AddAsync(CreateSampleTrade("QQQ", OptionsLog.TradeType.BTO, 380m));
        await service.AddAsync(CreateSampleTrade("IWM", OptionsLog.TradeType.STO, 200m));

        // Act
        var result = service.GetTradesByType(OptionsLog.TradeType.BTO);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.OptionTradeType == OptionsLog.TradeType.BTO);
    }

    [Fact]
    public async Task GetClosedTrades_FiltersWinnersCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateClosedTrade("SPY", 450m, 460m)); // Profit
        await service.AddAsync(CreateClosedTrade("QQQ", 380m, 375m)); // Loss
        await service.AddAsync(CreateSampleTrade("IWM", OptionsLog.TradeType.BTO, 200m)); // Open

        // Act
        var closedTrades = service.GetClosedTrades();
        var winners = closedTrades.Where(t => t.CalculatePnL() > 0).ToList();

        // Assert
        winners.Should().HaveCount(1);
        winners.First().Ticker.Should().Be("SPY");
    }

    [Fact]
    public async Task GetClosedTrades_FiltersLosersCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateClosedTrade("SPY", 450m, 460m)); // Profit
        await service.AddAsync(CreateClosedTrade("QQQ", 380m, 375m)); // Loss

        // Act
        var closedTrades = service.GetClosedTrades();
        var losers = closedTrades.Where(t => t.CalculatePnL() < 0).ToList();

        // Assert
        losers.Should().HaveCount(1);
        losers.First().Ticker.Should().Be("QQQ");
    }

    [Fact]
    public async Task Search_FindsMatchingTrades()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade1 = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        trade1.Notes = "Bullish breakout pattern";
        var trade2 = CreateSampleTrade("QQQ", OptionsLog.TradeType.BTO, 380m);
        trade2.Notes = "Following downtrend";
        await service.AddAsync(trade1);
        await service.AddAsync(trade2);

        // Act
        var result = service.Search("bullish");

        // Assert
        result.Should().HaveCount(1);
        result.First().Ticker.Should().Be("SPY");
    }

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        trade.Notes = "IMPORTANT TRADE";
        await service.AddAsync(trade);

        // Act
        var result = service.Search("important");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllSymbols_ReturnsUniqueSymbols()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m));
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.STC, 455m));
        await service.AddAsync(CreateSampleTrade("QQQ", OptionsLog.TradeType.BTO, 380m));

        // Act
        var result = service.GetAllSymbols();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("SPY");
        result.Should().Contain("QQQ");
    }

    [Fact]
    public async Task CalculateSummary_WithNoTrades_ReturnsZeros()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();

        // Act
        var summary = service.CalculateSummary();

        // Assert
        summary.TotalTrades.Should().Be(0);
        summary.OpenTrades.Should().Be(0);
        summary.ClosedTrades.Should().Be(0);
        summary.WinRate.Should().Be(0);
        summary.TotalPnL.Should().Be(0);
    }

    [Fact]
    public async Task CalculateSummary_WithMixedTrades_CalculatesCorrectly()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateClosedTrade("SPY", 450m, 460m)); // +$1000
        await service.AddAsync(CreateClosedTrade("QQQ", 380m, 375m)); // -$500
        await service.AddAsync(CreateClosedTrade("IWM", 200m, 210m)); // +$1000
        await service.AddAsync(CreateSampleTrade("TSLA", OptionsLog.TradeType.BTO, 250m)); // Open

        // Act
        var summary = service.CalculateSummary();

        // Assert
        summary.TotalTrades.Should().Be(4);
        summary.OpenTrades.Should().Be(1);
        summary.ClosedTrades.Should().Be(3);
        summary.WinningTrades.Should().Be(2);
        summary.LosingTrades.Should().Be(1);
        summary.WinRate.Should().BeApproximately(66.67m, 0.01m); // 2/3 = 66.67%
        summary.TotalPnL.Should().Be(1500m); // 1000 - 500 + 1000
        summary.AveragePnL.Should().Be(500m); // 1500 / 3
    }

    [Fact]
    public async Task CalculateSummary_WithOnlyWinners_Has100PercentWinRate()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateClosedTrade("SPY", 450m, 460m));
        await service.AddAsync(CreateClosedTrade("QQQ", 380m, 390m));

        // Act
        var summary = service.CalculateSummary();

        // Assert
        summary.WinRate.Should().Be(100m);
        summary.WinningTrades.Should().Be(2);
        summary.LosingTrades.Should().Be(0);
    }

    [Fact]
    public async Task ExportToJson_ProducesValidJson()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        await service.AddAsync(trade);

        // Act
        var json = service.ExportToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("SPY");
        json.Should().Contain("BTO");
        json.Should().Contain("450");
    }

    [Fact]
    public async Task ExportToCsv_ProducesValidCsv()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        await service.AddAsync(trade);

        // Act
        var csv = service.ExportToCsv();

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("Ticker");
        csv.Should().Contain("Type");
        csv.Should().Contain("SPY");
        csv.Should().Contain("BTO");
    }

    [Fact]
    public async Task ExportToCsv_EscapesCommasInNotes()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var trade = CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m);
        trade.Notes = "This has a comma, and should be quoted";
        await service.AddAsync(trade);

        // Act
        var csv = service.ExportToCsv();

        // Assert
        csv.Should().Contain("\"This has a comma, and should be quoted\"");
    }

    [Fact]
    public async Task OnTradesChanged_FiresWhenTradesModified()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        var eventFired = false;
        service.OnTradesChanged += () => eventFired = true;

        // Act
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m));

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public async Task ClearAsync_RemovesAllTrades()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new TradeLogService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateSampleTrade("SPY", OptionsLog.TradeType.BTO, 450m));
        await service.AddAsync(CreateSampleTrade("QQQ", OptionsLog.TradeType.BTO, 380m));

        // Act
        await service.ClearAsync();

        // Assert
        service.GetAllTrades().Should().BeEmpty();
    }

    // Helper methods
    private static OptionsLog CreateSampleTrade(
        string ticker,
        OptionsLog.TradeType type,
        decimal entryPrice,
        DateTime? date = null)
    {
        return new OptionsLog
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            OptionTradeType = type,
            EntryPrice = entryPrice,
            Quantity = 1,
            StrikePrice = entryPrice,
            ContractMultiplier = 100,
            TradeDirection = GexVisor.Core.TradeLog.Type.Long,
            CreatedDate = date ?? DateTime.UtcNow,
            Notes = string.Empty,
            Analysis = string.Empty
        };
    }

    private static OptionsLog CreateClosedTrade(
        string ticker,
        decimal entryPrice,
        decimal exitPrice)
    {
        var trade = CreateSampleTrade(ticker, OptionsLog.TradeType.BTO, entryPrice);
        trade.ExitPrice = exitPrice;
        return trade;
    }
}
