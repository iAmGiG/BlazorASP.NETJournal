// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Services;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for DecisionMetadataParser.
/// Tests JSON and CSV log file parsing for autotrader imports.
/// </summary>
public class DecisionMetadataParserTests
{
    [Fact]
    public void ParseJsonLog_WithValidData_ParsesCorrectly()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = @"[
            {
                ""tradeId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""exitPrice"": 455.00,
                ""quantity"": 1,
                ""strikePrice"": 450.00,
                ""activePatterns"": [""gamma_positioning"", ""stock_pinning""],
                ""primaryTrigger"": ""gamma_positioning"",
                ""confidenceScore"": 0.85,
                ""regimeType"": ""bullish"",
                ""rationale"": ""Strong gamma support at 450 level""
            }
        ]";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(json);

        // Assert
        trades.Should().HaveCount(1);
        decisions.Should().HaveCount(1);

        var trade = trades.First();
        trade.Ticker.Should().Be("SPY");
        trade.OptionTradeType.Should().Be(OptionsLog.TradeType.BTO);
        trade.EntryPrice.Should().Be(450.50m);
        trade.ExitPrice.Should().Be(455.00m);
        trade.Quantity.Should().Be(1);
        trade.StrikePrice.Should().Be(450.00m);

        var decision = decisions.First();
        decision.TradeId.Should().Be(trade.Id);
        decision.ActivePatterns.Should().Contain("gamma_positioning");
        decision.ActivePatterns.Should().Contain("stock_pinning");
        decision.PrimaryTrigger.Should().Be("gamma_positioning");
        decision.ConfidenceScore.Should().Be(0.85m);
        decision.RegimeType.Should().Be("bullish");
        decision.DecisionRationale.Should().Be("Strong gamma support at 450 level");
    }

    [Fact]
    public void ParseJsonLog_WithMultipleEntries_ParsesAll()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = @"[
            {
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""quantity"": 1,
                ""strikePrice"": 450.00
            },
            {
                ""timestamp"": ""2024-01-15T11:30:00Z"",
                ""symbol"": ""QQQ"",
                ""action"": ""STO"",
                ""entryPrice"": 380.00,
                ""quantity"": 2,
                ""strikePrice"": 380.00
            }
        ]";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(json);

        // Assert
        trades.Should().HaveCount(2);
        decisions.Should().HaveCount(2);
        trades[0].Ticker.Should().Be("SPY");
        trades[1].Ticker.Should().Be("QQQ");
    }

    [Fact]
    public void ParseJsonLog_WithEmptyPatterns_HandlesGracefully()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = @"[
            {
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""quantity"": 1,
                ""strikePrice"": 450.00,
                ""activePatterns"": []
            }
        ]";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(json);

        // Assert
        trades.Should().HaveCount(1);
        decisions.Should().HaveCount(1);
        decisions.First().ActivePatterns.Should().BeEmpty();
    }

    [Fact]
    public void ParseJsonLog_WithSignalStrengths_ParsesCorrectly()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = @"[
            {
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""quantity"": 1,
                ""strikePrice"": 450.00,
                ""activePatterns"": [""gamma_positioning""],
                ""signalStrengths"": {
                    ""gamma_positioning"": 0.92,
                    ""stock_pinning"": 0.45
                }
            }
        ]";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(json);

        // Assert
        var decision = decisions.First();
        decision.SignalStrengths.Should().ContainKey("gamma_positioning");
        decision.SignalStrengths["gamma_positioning"].Should().Be(0.92m);
        decision.SignalStrengths.Should().ContainKey("stock_pinning");
        decision.SignalStrengths["stock_pinning"].Should().Be(0.45m);
    }

    [Fact]
    public void ParseJsonLog_WithGexAndIvLevels_ParsesCorrectly()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = @"[
            {
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""quantity"": 1,
                ""strikePrice"": 450.00,
                ""gexLevel"": 2.5,
                ""ivLevel"": 0.25,
                ""isNegativeGamma"": false
            }
        ]";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(json);

        // Assert
        var decision = decisions.First();
        decision.GexLevel.Should().Be(2.5m);
        decision.IvLevel.Should().Be(0.25m);
        decision.IsNegativeGamma.Should().BeFalse();
    }

    [Fact]
    public void ParseCsvLog_WithValidData_ParsesCorrectly()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var csv = @"Timestamp,Symbol,Action,EntryPrice,ExitPrice,Quantity,StrikePrice,Notes
2024-01-15T10:30:00Z,SPY,BTO,450.50,455.00,1,450.00,Test trade
2024-01-15T11:30:00Z,QQQ,STO,380.00,,2,380.00,Short position";

        // Act
        var (trades, decisions) = parser.ParseCsvLog(csv);

        // Assert
        trades.Should().HaveCount(2);

        var trade1 = trades[0];
        trade1.Ticker.Should().Be("SPY");
        trade1.OptionTradeType.Should().Be(OptionsLog.TradeType.BTO);
        trade1.EntryPrice.Should().Be(450.50m);
        trade1.ExitPrice.Should().Be(455.00m);
        trade1.Quantity.Should().Be(1);
        trade1.StrikePrice.Should().Be(450.00m);
        trade1.Notes.Should().Be("Test trade");

        var trade2 = trades[1];
        trade2.Ticker.Should().Be("QQQ");
        trade2.OptionTradeType.Should().Be(OptionsLog.TradeType.STO);
        trade2.ExitPrice.Should().BeNull();
    }

    [Fact]
    public void ParseCsvLog_WithHeaderOnly_ReturnsEmpty()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var csv = "Timestamp,Symbol,Action,EntryPrice,ExitPrice,Quantity,StrikePrice,Notes";

        // Act
        var (trades, decisions) = parser.ParseCsvLog(csv);

        // Assert
        trades.Should().BeEmpty();
        decisions.Should().BeEmpty();
    }

    [Fact]
    public void ParseCsvLog_WithMissingOptionalFields_HandlesGracefully()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var csv = @"Timestamp,Symbol,Action,EntryPrice,ExitPrice,Quantity,StrikePrice,Notes
2024-01-15T10:30:00Z,SPY,BTO,450.50,,1,450.00,";

        // Act
        var (trades, decisions) = parser.ParseCsvLog(csv);

        // Assert
        trades.Should().HaveCount(1);
        var trade = trades.First();
        trade.ExitPrice.Should().BeNull();
        trade.Notes.Should().Be(string.Empty);
    }

    [Fact]
    public void ParseCsvLog_WithInvalidNumericValue_SkipsRow()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var csv = @"Timestamp,Symbol,Action,EntryPrice,ExitPrice,Quantity,StrikePrice,Notes
2024-01-15T10:30:00Z,SPY,BTO,INVALID,455.00,1,450.00,Test
2024-01-15T11:30:00Z,QQQ,STO,380.00,,2,380.00,Valid";

        // Act
        var (trades, decisions) = parser.ParseCsvLog(csv);

        // Assert
        trades.Should().HaveCount(1);
        trades.First().Ticker.Should().Be("QQQ");
    }

    [Fact]
    public void ParseJsonLog_WithInvalidJson_ReturnsEmpty()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var invalidJson = "{ this is not valid json }";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(invalidJson);

        // Assert
        trades.Should().BeEmpty();
        decisions.Should().BeEmpty();
    }

    [Fact]
    public void ParseJsonLog_WithEmptyArray_ReturnsEmpty()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = "[]";

        // Act
        var (trades, decisions) = parser.ParseJsonLog(json);

        // Assert
        trades.Should().BeEmpty();
        decisions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("BTO", OptionsLog.TradeType.BTO)]
    [InlineData("btc", OptionsLog.TradeType.BTC)]
    [InlineData("STO", OptionsLog.TradeType.STO)]
    [InlineData("stc", OptionsLog.TradeType.STC)]
    [InlineData("UNKNOWN", OptionsLog.TradeType.BTO)] // Defaults to BTO
    public void ParseJsonLog_ParsesTradeTypeCorrectly(string action, OptionsLog.TradeType expectedType)
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = $@"[{{
            ""timestamp"": ""2024-01-15T10:30:00Z"",
            ""symbol"": ""SPY"",
            ""action"": ""{action}"",
            ""entryPrice"": 450.50,
            ""quantity"": 1,
            ""strikePrice"": 450.00
        }}]";

        // Act
        var (trades, _) = parser.ParseJsonLog(json);

        // Assert
        trades.First().OptionTradeType.Should().Be(expectedType);
    }

    [Fact]
    public void ParseJsonLog_WithRiskAssessment_ParsesCorrectly()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var json = @"[
            {
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""quantity"": 1,
                ""strikePrice"": 450.00,
                ""riskAssessment"": ""High volatility expected around FOMC announcement""
            }
        ]";

        // Act
        var (_, decisions) = parser.ParseJsonLog(json);

        // Assert
        var decision = decisions.First();
        decision.RiskAssessment.Should().Be("High volatility expected around FOMC announcement");
    }

    [Fact]
    public void ParseJsonLog_SetsDecisionTimeFromTimestamp()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var expectedTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var json = @"[
            {
                ""timestamp"": ""2024-01-15T10:30:00Z"",
                ""symbol"": ""SPY"",
                ""action"": ""BTO"",
                ""entryPrice"": 450.50,
                ""quantity"": 1,
                ""strikePrice"": 450.00
            }
        ]";

        // Act
        var (_, decisions) = parser.ParseJsonLog(json);

        // Assert
        var decision = decisions.First();
        decision.DecisionTime.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ParseCsvLog_WithQuotedCommasInNotes_ParsesCorrectly()
    {
        // Arrange
        var parser = new DecisionMetadataParser();
        var csv = @"Timestamp,Symbol,Action,EntryPrice,ExitPrice,Quantity,StrikePrice,Notes
2024-01-15T10:30:00Z,SPY,BTO,450.50,455.00,1,450.00,""Entry at support, tight stop loss""";

        // Act
        var (trades, _) = parser.ParseCsvLog(csv);

        // Assert
        var trade = trades.First();
        trade.Notes.Should().Be("Entry at support, tight stop loss");
    }
}
