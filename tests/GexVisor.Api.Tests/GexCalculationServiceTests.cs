// Copyright (c) GexVisor. All rights reserved.

using GexVisor.Api.Services;
using GexVisor.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace GexVisor.Api.Tests;

public class GexCalculationServiceTests
{
    private readonly Mock<IOptionsChainService> mockOptionsService;
    private readonly Mock<IMarketDataService> mockMarketDataService;
    private readonly IGexCalculationService service;

    public GexCalculationServiceTests()
    {
        this.mockOptionsService = new Mock<IOptionsChainService>();
        this.mockMarketDataService = new Mock<IMarketDataService>();

        this.service = new GexCalculationService(
            this.mockOptionsService.Object,
            this.mockMarketDataService.Object,
            Mock.Of<ILogger<GexCalculationService>>());
    }

    [Fact]
    public async Task CalculateGex_ValidSymbol_ReturnsResult()
    {
        // Arrange
        const decimal spotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 445m, OptionType.Call, 0.01m, 1000),
            CreateContract("SPY", 450m, OptionType.Call, 0.02m, 2000),
            CreateContract("SPY", 455m, OptionType.Call, 0.015m, 1500),
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 800),
            CreateContract("SPY", 450m, OptionType.Put, 0.02m, 1800),
            CreateContract("SPY", 455m, OptionType.Put, 0.015m, 1200),
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("SPY", result.Data.Symbol);
        Assert.Equal(spotPrice, result.Data.SpotPrice);
        Assert.Equal(3, result.Data.StrikeGammas.Count); // 3 unique strikes
    }

    [Fact]
    public async Task CalculateGex_TotalGex_MatchesFormula()
    {
        // Arrange - Create known test data to verify GEX formula
        // GEX = gamma * OI * 100 * S²
        // S = 100, S² = 10,000
        // Call GEX at 100 strike = 0.05 * 1000 * 100 * 10000 = 50,000,000
        // Put GEX at 100 strike = 0.04 * 800 * 100 * 10000 = 32,000,000
        // Expected Total GEX = Call - Put = 50,000,000 - 32,000,000 = 18,000,000
        const decimal spotPrice = 100m;
        var chain = CreateTestOptionsChain("TEST", new[]
        {
            CreateContract("TEST", 100m, OptionType.Call, 0.05m, 1000),
            CreateContract("TEST", 100m, OptionType.Put, 0.04m, 800),
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("TEST", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("TEST", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        // Verify formula: gamma * OI * 100 * S²
        var expectedCallGex = 0.05m * 1000 * 100 * (spotPrice * spotPrice); // 50,000,000
        var expectedPutGex = 0.04m * 800 * 100 * (spotPrice * spotPrice);   // 32,000,000
        var expectedTotalGex = expectedCallGex - expectedPutGex;             // 18,000,000

        Assert.Equal(expectedCallGex, result.Data.CallGex);
        Assert.Equal(expectedPutGex, result.Data.PutGex);
        Assert.Equal(expectedTotalGex, result.Data.TotalGex);
    }

    [Fact]
    public async Task FindZeroGammaLevel_CrossingExists_ReturnsInterpolated()
    {
        // Arrange - Create data where net GEX crosses zero between strikes
        // Strike 95: Call GEX 10M, Put GEX 5M -> Net = +5M
        // Strike 100: Call GEX 5M, Put GEX 10M -> Net = -5M
        // Zero crossing should be at strike 97.5 (midpoint)
        const decimal spotPrice = 100m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 95m, OptionType.Call, 0.05m, 2000),  // High call GEX
            CreateContract("SPY", 95m, OptionType.Put, 0.025m, 2000), // Low put GEX
            CreateContract("SPY", 100m, OptionType.Call, 0.025m, 2000), // Low call GEX
            CreateContract("SPY", 100m, OptionType.Put, 0.05m, 2000),   // High put GEX
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ZeroGammaLevel);
        Assert.True(result.Data.ZeroGammaLevel > 95m);
        Assert.True(result.Data.ZeroGammaLevel < 100m);
    }

    [Fact]
    public async Task ClassifyRegime_NetPositive_ReturnsLongGamma()
    {
        // Arrange - Create data with much higher call GEX than put GEX
        const decimal spotPrice = 100m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 100m, OptionType.Call, 0.1m, 5000),  // Very high call GEX
            CreateContract("SPY", 100m, OptionType.Put, 0.01m, 500),    // Low put GEX
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(GexRegime.LongGamma, result.Data.Regime);
    }

    [Fact]
    public async Task ClassifyRegime_NetNegative_ReturnsShortGamma()
    {
        // Arrange - Create data with much higher put GEX than call GEX
        const decimal spotPrice = 100m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 100m, OptionType.Call, 0.01m, 500),   // Low call GEX
            CreateContract("SPY", 100m, OptionType.Put, 0.1m, 5000),     // Very high put GEX
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(GexRegime.ShortGamma, result.Data.Regime);
    }

    [Fact]
    public async Task ClassifyRegime_Balanced_ReturnsNeutral()
    {
        // Arrange - Create data with roughly equal call and put GEX
        const decimal spotPrice = 100m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 100m, OptionType.Call, 0.05m, 1000),
            CreateContract("SPY", 100m, OptionType.Put, 0.048m, 1000),   // Within 10% of call
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(GexRegime.Neutral, result.Data.Regime);
    }

    [Fact]
    public async Task StrikeGammas_GroupedCorrectly_ByStrike()
    {
        // Arrange - Multiple contracts at same strike
        const decimal spotPrice = 100m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 95m, OptionType.Call, 0.01m, 1000),
            CreateContract("SPY", 100m, OptionType.Call, 0.02m, 2000),
            CreateContract("SPY", 100m, OptionType.Put, 0.02m, 1500),
            CreateContract("SPY", 105m, OptionType.Put, 0.01m, 1000),
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", spotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.StrikeGammas.Count); // 3 unique strikes: 95, 100, 105

        var strike100 = result.Data.StrikeGammas.First(s => s.StrikePrice == 100m);
        Assert.Equal(2, strike100.ContractsCount); // Both call and put at 100
    }

    [Fact]
    public async Task CalculateGex_AutoFetchesSpotPrice()
    {
        // Arrange
        const decimal spotPrice = 450m;
        var quote = new Quote
        {
            Symbol = "SPY",
            Price = spotPrice,
            Timestamp = DateTime.UtcNow,
        };

        this.mockMarketDataService
            .Setup(s => s.GetQuoteAsync("SPY"))
            .ReturnsAsync(MarketDataResult<Quote>.Ok(quote, MarketDataProvider.Alpaca));

        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 450m, OptionType.Call, 0.02m, 1000),
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act - Call without explicit spot price
        var result = await this.service.CalculateGexAsync("SPY");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(spotPrice, result.Data.SpotPrice);
        this.mockMarketDataService.Verify(s => s.GetQuoteAsync("SPY"), Times.Once);
    }

    [Fact]
    public async Task CalculateGex_NoValidContracts_ReturnsFailure()
    {
        // Arrange - Chain with no valid gamma/OI
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 450m, OptionType.Call, null, 0), // No gamma, no OI
            CreateContract("SPY", 455m, OptionType.Put, 0m, 1000),  // Zero gamma
        });

        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", 450m);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid contracts", result.Error);
    }

    [Fact]
    public async Task CalculateGex_OptionsChainFails_ReturnsFailure()
    {
        // Arrange
        this.mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Fail("API error"));

        // Act
        var result = await this.service.CalculateGexAsync("SPY", 450m);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to fetch options chain", result.Error);
    }

    private static OptionsChain CreateTestOptionsChain(string symbol, OptionContract[] contracts)
    {
        return new OptionsChain
        {
            Symbol = symbol,
            Contracts = contracts.ToList(),
            Timestamp = DateTime.UtcNow,
            Source = MarketDataProvider.AlphaVantage,
        };
    }

    private static OptionContract CreateContract(
        string symbol,
        decimal strike,
        OptionType type,
        decimal? gamma,
        long openInterest)
    {
        return new OptionContract
        {
            Symbol = symbol,
            ContractSymbol = $"{symbol}240119{(type == OptionType.Call ? 'C' : 'P')}{strike:00000000}",
            StrikePrice = strike,
            Type = type,
            ExpirationDate = new DateTime(2024, 1, 19),
            TradingDate = new DateTime(2024, 1, 15),
            Gamma = gamma,
            OpenInterest = openInterest,
            Delta = type == OptionType.Call ? 0.5m : -0.5m,
            Timestamp = DateTime.UtcNow,
            Source = MarketDataProvider.AlphaVantage,
        };
    }
}
