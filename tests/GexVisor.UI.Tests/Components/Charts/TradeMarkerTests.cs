// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Components.Charts;

namespace GexVisor.UI.Tests.Components.Charts;

/// <summary>
/// Unit tests for TradeMarker helper class.
/// Tests annotation building for trade entry/exit markers on charts.
/// </summary>
public class TradeMarkerTests
{
    [Fact]
    public void BuildTradeAnnotations_ReturnsEmptyPoints_WhenNoValidData()
    {
        // Arrange
        var trade = new OptionsLog();

        // Act
        var annotations = TradeMarker.BuildTradeAnnotations(trade);

        // Assert
        annotations.Should().NotBeNull();
        annotations.Points.Should().BeEmpty();
    }

    [Fact]
    public void BuildTradeAnnotations_AddsEntryMarker_WhenEntryDataValid()
    {
        // Arrange
        var trade = CreateLongTrade();

        // Act
        var annotations = TradeMarker.BuildTradeAnnotations(trade);

        // Assert
        annotations.Points.Should().HaveCount(1);
    }

    [Fact]
    public void BuildTradeAnnotations_AddsEntryAndExitMarkers_WhenClosedTrade()
    {
        // Arrange
        var trade = CreateClosedLongTrade();

        // Act
        var annotations = TradeMarker.BuildTradeAnnotations(trade);

        // Assert
        annotations.Points.Should().HaveCount(2);
    }

    [Fact]
    public void BuildEntryMarker_SetsCorrectXAndY()
    {
        // Arrange
        var trade = CreateLongTrade();

        // Act
        var marker = TradeMarker.BuildEntryMarker(trade);

        // Assert
        marker.X.Should().NotBeNull();
        marker.Y.Should().Be(150.00);
    }

    [Fact]
    public void BuildEntryMarker_UsesGreenColor_ForLongTrade()
    {
        // Arrange
        var trade = CreateLongTrade();

        // Act
        var marker = TradeMarker.BuildEntryMarker(trade);

        // Assert
        marker.Marker?.FillColor.Should().Be("#00c853");
    }

    [Fact]
    public void BuildEntryMarker_UsesRedColor_ForShortTrade()
    {
        // Arrange
        var trade = CreateShortTrade();

        // Act
        var marker = TradeMarker.BuildEntryMarker(trade);

        // Assert
        marker.Marker?.FillColor.Should().Be("#ff5252");
    }

    [Fact]
    public void BuildEntryMarker_SetsCorrectLabel_ForBTO()
    {
        // Arrange
        var trade = CreateLongTrade();
        trade.OptionTradeType = OptionsLog.TradeType.BTO;

        // Act
        var marker = TradeMarker.BuildEntryMarker(trade);

        // Assert
        marker.Label?.Text.Should().Contain("BTO");
    }

    [Fact]
    public void BuildEntryMarker_SetsCorrectLabel_ForSTO()
    {
        // Arrange
        var trade = CreateShortTrade();
        trade.OptionTradeType = OptionsLog.TradeType.STO;

        // Act
        var marker = TradeMarker.BuildEntryMarker(trade);

        // Assert
        marker.Label?.Text.Should().Contain("STO");
    }

    [Fact]
    public void BuildExitMarker_UsesGreenColor_ForProfitableTrade()
    {
        // Arrange
        var trade = CreateClosedLongTrade(entryPrice: 100, exitPrice: 120);

        // Act
        var marker = TradeMarker.BuildExitMarker(trade);

        // Assert
        marker.Marker?.FillColor.Should().Be("#00c853");
    }

    [Fact]
    public void BuildExitMarker_UsesRedColor_ForLosingTrade()
    {
        // Arrange
        var trade = CreateClosedLongTrade(entryPrice: 100, exitPrice: 80);

        // Act
        var marker = TradeMarker.BuildExitMarker(trade);

        // Assert
        marker.Marker?.FillColor.Should().Be("#ff5252");
    }

    [Fact]
    public void BuildExitMarker_IncludesPnLInLabel_WhenProfitable()
    {
        // Arrange - BTO at 100, exit at 120, 1 contract = +$2000 PnL
        var trade = CreateClosedLongTrade(entryPrice: 100, exitPrice: 120);

        // Act
        var marker = TradeMarker.BuildExitMarker(trade);

        // Assert - Text is List<string>, get first element for string assertions
        var labelText = marker.Label?.Text?.FirstOrDefault();
        labelText.Should().Contain("+$");
        labelText.Should().StartWith("STC");
    }

    [Fact]
    public void BuildExitMarker_IncludesNegativePnLInLabel_WhenLosing()
    {
        // Arrange - BTO at 100, exit at 80
        var trade = CreateClosedLongTrade(entryPrice: 100, exitPrice: 80);

        // Act
        var marker = TradeMarker.BuildExitMarker(trade);

        // Assert - Format is ($-N) not -$N
        var labelText = marker.Label?.Text?.FirstOrDefault();
        labelText.Should().Contain("$-");
    }

    [Fact]
    public void BuildEntryPriceLine_CreatesYAxisAnnotation()
    {
        // Arrange
        var trade = CreateLongTrade();

        // Act
        var line = TradeMarker.BuildEntryPriceLine(trade);

        // Assert
        line.Should().NotBeNull();
        line.Y.Should().Be(150.00);
        line.BorderColor.Should().Be("#00c853");
        var labelText = line.Label?.Text?.FirstOrDefault();
        labelText.Should().Contain("Entry");
        labelText.Should().Contain("$150.00");
    }

    [Fact]
    public void BuildEntryPriceLine_UsesDashedLine()
    {
        // Arrange
        var trade = CreateLongTrade();

        // Act
        var line = TradeMarker.BuildEntryPriceLine(trade);

        // Assert
        line.StrokeDashArray.Should().Be(4);
        line.BorderWidth.Should().Be(1);
    }

    [Fact]
    public void BuildExitPriceLine_ReturnsNull_WhenNoExitPrice()
    {
        // Arrange
        var trade = CreateLongTrade(); // Open trade, no exit

        // Act
        var line = TradeMarker.BuildExitPriceLine(trade);

        // Assert
        line.Should().BeNull();
    }

    [Fact]
    public void BuildExitPriceLine_CreatesYAxisAnnotation_WhenClosed()
    {
        // Arrange
        var trade = CreateClosedLongTrade(entryPrice: 100, exitPrice: 110);

        // Act
        var line = TradeMarker.BuildExitPriceLine(trade);

        // Assert
        line.Should().NotBeNull();
        line!.Y.Should().Be(110.00);
        var labelText = line.Label?.Text?.FirstOrDefault();
        labelText.Should().Contain("Exit");
        labelText.Should().Contain("$110.00");
    }

    private static OptionsLog CreateLongTrade()
    {
        return new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 150m,
            CreatedDate = DateTime.Now.AddDays(-5),
            Quantity = 1,
            StrikePrice = 400m,
            ExpirationDate = DateTime.Now.AddDays(30),
            Ticker = "SPY",
        };
    }

    private static OptionsLog CreateShortTrade()
    {
        return new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STO,
            EntryPrice = 150m,
            CreatedDate = DateTime.Now.AddDays(-5),
            Quantity = 1,
            StrikePrice = 400m,
            ExpirationDate = DateTime.Now.AddDays(30),
            Ticker = "SPY",
        };
    }

    private static OptionsLog CreateClosedLongTrade(decimal entryPrice = 150m, decimal exitPrice = 180m)
    {
        return new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = entryPrice,
            ExitPrice = exitPrice,
            CreatedDate = DateTime.Now.AddDays(-10),
            TargetCompletionDate = DateTime.Now.AddDays(-2),
            Quantity = 1,
            StrikePrice = 400m,
            ExpirationDate = DateTime.Now.AddDays(30),
            Ticker = "SPY",
        };
    }
}
