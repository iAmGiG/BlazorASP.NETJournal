// Copyright (c) GexVisor. All rights reserved.

namespace GexVisor.Core.Tests;

/// <summary>
/// Unit tests for JournalFramework, specifically OptionsLog P&L calculations.
/// Tests verify correct behavior after fixing #91 (BTO/STC grouping bug).
/// </summary>
public class OptionsLogTests
{
    [Fact]
    public void CalculatePnL_BTO_ProfitScenario_ReturnsPositivePnL()
    {
        // Arrange: Buy to open at $2, sell at $5 (profit scenario)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($5 - $2) × 1 contract × 100 multiplier × +1 (long) = $300
        Assert.NotNull(pnl);
        Assert.Equal(300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_BTO_LossScenario_ReturnsNegativePnL()
    {
        // Arrange: Buy to open at $5, sell at $2 (loss scenario)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 5.00m,
            ExitPrice = 2.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($2 - $5) × 1 contract × 100 multiplier × +1 (long) = -$300
        Assert.NotNull(pnl);
        Assert.Equal(-300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_BTO_MultipleContracts_ScalesCorrectly()
    {
        // Arrange: 5 contracts at $2 → $5
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 5,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($5 - $2) × 5 contracts × 100 multiplier = $1,500
        Assert.NotNull(pnl);
        Assert.Equal(1500m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_STC_ProfitScenario_ReturnsPositivePnL()
    {
        // Arrange: Bought at $2 (via BTO), selling to close at $5
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STC,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: Same as BTO - long position profits when price rises
        Assert.NotNull(pnl);
        Assert.Equal(300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_STC_LossScenario_ReturnsNegativePnL()
    {
        // Arrange: Bought at $5 (via BTO), selling to close at $2
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STC,
            EntryPrice = 5.00m,
            ExitPrice = 2.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($2 - $5) × 1 contract × 100 multiplier × +1 (long) = -$300
        Assert.NotNull(pnl);
        Assert.Equal(-300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_STO_ProfitScenario_ReturnsPositivePnL()
    {
        // Arrange: Sell to open at $5, buy back at $2 (profit when price falls)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STO,
            EntryPrice = 5.00m,
            ExitPrice = 2.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($2 - $5) × 1 contract × 100 multiplier × -1 (short) = $300
        Assert.NotNull(pnl);
        Assert.Equal(300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_STO_LossScenario_ReturnsNegativePnL()
    {
        // Arrange: Sell to open at $2, buy back at $5 (loss when price rises)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STO,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($5 - $2) × 1 contract × 100 multiplier × -1 (short) = -$300
        Assert.NotNull(pnl);
        Assert.Equal(-300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_STO_MultipleContracts_ScalesCorrectly()
    {
        // Arrange: 5 contracts at $5 → $2
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STO,
            EntryPrice = 5.00m,
            ExitPrice = 2.00m,
            Quantity = 5,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($2 - $5) × 5 contracts × 100 multiplier × -1 (short) = $1,500
        Assert.NotNull(pnl);
        Assert.Equal(1500m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_BTC_ProfitScenario_ReturnsPositivePnL()
    {
        // Arrange: Sold at $5 (via STO), buying to close at $2
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTC,
            EntryPrice = 5.00m,
            ExitPrice = 2.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: Same as STO - short position profits when price falls
        // This was the critical bug in #91: BTC was grouped with BTO incorrectly
        Assert.NotNull(pnl);
        Assert.Equal(300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_BTC_LossScenario_ReturnsNegativePnL()
    {
        // Arrange: Sold at $2 (via STO), buying to close at $5
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTC,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($5 - $2) × 1 contract × 100 multiplier × -1 (short) = -$300
        Assert.NotNull(pnl);
        Assert.Equal(-300m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_OpenPosition_ReturnsNull()
    {
        // Arrange: Trade not yet closed (ExitPrice is null)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 2.00m,
            ExitPrice = null,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: Should return null for open positions
        Assert.Null(pnl);
    }

    [Fact]
    public void CalculatePnL_ZeroPnL_ReturnsZero()
    {
        // Arrange: Entry and exit at same price
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 3.50m,
            ExitPrice = 3.50m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: No price movement = no profit/loss
        Assert.NotNull(pnl);
        Assert.Equal(0m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_MiniOptions_UsesCorrectMultiplier()
    {
        // Arrange: Mini-Options with 10 multiplier
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 1,
            ContractMultiplier = 10m, // Mini-Options
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($5 - $2) × 1 contract × 10 multiplier = $30
        Assert.NotNull(pnl);
        Assert.Equal(30m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_FuturesOptions_UsesCorrectMultiplier()
    {
        // Arrange: Futures Options with 50 multiplier
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STO,
            EntryPrice = 100.00m,
            ExitPrice = 95.00m,
            Quantity = 2,
            ContractMultiplier = 50m, // Futures Options
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($95 - $100) × 2 contracts × 50 multiplier × -1 (short) = $500
        Assert.NotNull(pnl);
        Assert.Equal(500m, pnl.Value);
    }

    [Fact]
    public void CalculatePnL_FractionalPrices_HandlesDecimalsPrecisely()
    {
        // Arrange: Fractional option prices
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTO,
            EntryPrice = 1.25m,
            ExitPrice = 3.75m,
            Quantity = 10,
            ContractMultiplier = 100m,
        };

        // Act
        var pnl = trade.CalculatePnL();

        // Assert: ($3.75 - $1.25) × 10 contracts × 100 multiplier = $2,500
        Assert.NotNull(pnl);
        Assert.Equal(2500m, pnl.Value);
    }

    [Fact]
    public void BugRegression_BTC_WasIncorrectlyGroupedWithBTO()
    {
        // This test documents the bug fixed in #91
        // BTC (Buy to Close) was incorrectly grouped with BTO (Buy to Open)
        // causing short position closings to calculate as long positions

        // Scenario: Sell option at $5 (STO), buy back at $3 (BTC)
        // Expected profit: $200 (price fell while short)
        // Old bug: Calculated as -$200 (treated as long position)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.BTC,
            EntryPrice = 5.00m,
            ExitPrice = 3.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        var pnl = trade.CalculatePnL();

        // Assert: Should be +$200, not -$200
        Assert.NotNull(pnl);
        Assert.Equal(200m, pnl.Value);
    }

    [Fact]
    public void BugRegression_STC_WasIncorrectlyGroupedWithSTO()
    {
        // This test documents the bug fixed in #91
        // STC (Sell to Close) was incorrectly grouped with STO (Sell to Open)
        // causing long position closings to calculate as short positions

        // Scenario: Buy option at $2 (BTO), sell at $5 (STC)
        // Expected profit: $300 (price rose while long)
        // Old bug: Calculated as -$300 (treated as short position)
        var trade = new OptionsLog
        {
            OptionTradeType = OptionsLog.TradeType.STC,
            EntryPrice = 2.00m,
            ExitPrice = 5.00m,
            Quantity = 1,
            ContractMultiplier = 100m,
        };

        var pnl = trade.CalculatePnL();

        // Assert: Should be +$300, not -$300
        Assert.NotNull(pnl);
        Assert.Equal(300m, pnl.Value);
    }
}
