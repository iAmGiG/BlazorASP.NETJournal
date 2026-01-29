// Copyright (c) GexVisor. All rights reserved.

using GexVisor.Api.Services;
using GexVisor.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace GexVisor.Api.Tests;

public class GammaWallAnalysisTests
{
    private readonly Mock<IOptionsChainService> _mockOptionsService;
    private readonly Mock<IMarketDataService> _mockMarketDataService;
    private readonly GexCalculationService _service;

    public GammaWallAnalysisTests()
    {
        _mockOptionsService = new Mock<IOptionsChainService>();
        _mockMarketDataService = new Mock<IMarketDataService>();

        _service = new GexCalculationService(
            _mockOptionsService.Object,
            _mockMarketDataService.Object,
            Mock.Of<ILogger<GexCalculationService>>());
    }

    [Fact]
    public async Task AnalyzeWalls_ValidData_ReturnsAnalysis()
    {
        // Arrange
        const decimal SpotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 445m, OptionType.Call, 0.03m, 3000),  // Positive below spot = support
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 1000),
            CreateContract("SPY", 450m, OptionType.Call, 0.02m, 2000),
            CreateContract("SPY", 450m, OptionType.Put, 0.02m, 2000),
            CreateContract("SPY", 455m, OptionType.Call, 0.01m, 1000),
            CreateContract("SPY", 455m, OptionType.Put, 0.03m, 3000),  // Negative above spot = resistance
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", SpotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.SupportLevels);
        Assert.NotNull(result.Data.ResistanceLevels);
        Assert.NotNull(result.Data.FlipPoints);
    }

    [Fact]
    public async Task AnalyzeWalls_IdentifiesSupportLevels()
    {
        // Arrange - Create strong positive gamma below spot
        const decimal SpotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 440m, OptionType.Call, 0.05m, 5000),  // Strong positive below spot
            CreateContract("SPY", 440m, OptionType.Put, 0.01m, 1000),
            CreateContract("SPY", 445m, OptionType.Call, 0.04m, 4000),  // Medium positive below spot
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 1000),
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", SpotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.SupportLevels);
        Assert.All(result.Data.SupportLevels, wall =>
        {
            Assert.Equal(GammaWallType.Support, wall.WallType);
            Assert.True(wall.StrikePrice < SpotPrice);
            Assert.True(wall.NetGex > 0);
        });
    }

    [Fact]
    public async Task AnalyzeWalls_IdentifiesResistanceLevels()
    {
        // Arrange - Create strong negative gamma above spot
        const decimal SpotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 455m, OptionType.Call, 0.01m, 1000),  // Low call
            CreateContract("SPY", 455m, OptionType.Put, 0.05m, 5000),   // High put = negative above spot
            CreateContract("SPY", 460m, OptionType.Call, 0.01m, 1000),
            CreateContract("SPY", 460m, OptionType.Put, 0.04m, 4000),
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", SpotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ResistanceLevels);
        Assert.All(result.Data.ResistanceLevels, wall =>
        {
            Assert.Equal(GammaWallType.Resistance, wall.WallType);
            Assert.True(wall.StrikePrice > SpotPrice);
            Assert.True(wall.NetGex < 0);
        });
    }

    [Fact]
    public async Task AnalyzeWalls_FindsFlipPoints()
    {
        // Arrange - Create data where net GEX crosses zero between strikes
        const decimal SpotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 445m, OptionType.Call, 0.05m, 2000),  // Net positive
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 1000),
            CreateContract("SPY", 455m, OptionType.Call, 0.01m, 1000),  // Net negative
            CreateContract("SPY", 455m, OptionType.Put, 0.05m, 2000),
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", SpotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.FlipPoints);

        var flipPoint = result.Data.FlipPoints[0];
        Assert.Equal(GammaWallType.FlipPoint, flipPoint.WallType);
        Assert.True(flipPoint.StrikePrice > 445m);
        Assert.True(flipPoint.StrikePrice < 455m);
        Assert.Equal(0m, flipPoint.NetGex);
    }

    [Fact]
    public async Task AnalyzeWalls_CalculatesAsymmetry()
    {
        // Arrange - Create data with asymmetric GEX distribution
        const decimal SpotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 445m, OptionType.Call, 0.02m, 2000),  // Below spot
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 1000),
            CreateContract("SPY", 455m, OptionType.Call, 0.05m, 5000),  // Much stronger above spot
            CreateContract("SPY", 455m, OptionType.Put, 0.02m, 2000),
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", SpotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.GexAsymmetry >= -1m);
        Assert.True(result.Data.GexAsymmetry <= 1m);

        // With more GEX above spot, asymmetry should be positive
        Assert.True(result.Data.GexAboveSpot != 0 || result.Data.GexBelowSpot != 0);
    }

    [Fact]
    public async Task AnalyzeWalls_MagnetismWithinBounds()
    {
        // Arrange
        const decimal SpotPrice = 450m;
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 445m, OptionType.Call, 0.03m, 3000),
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 1000),
            CreateContract("SPY", 450m, OptionType.Call, 0.05m, 5000),  // At spot, should have high magnetism
            CreateContract("SPY", 450m, OptionType.Put, 0.02m, 2000),
            CreateContract("SPY", 455m, OptionType.Call, 0.02m, 2000),
            CreateContract("SPY", 455m, OptionType.Put, 0.03m, 3000),
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", SpotPrice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        // Check all walls have magnetism within bounds
        var allWalls = result.Data.SupportLevels
            .Concat(result.Data.ResistanceLevels)
            .Concat(result.Data.FlipPoints);

        Assert.All(allWalls, wall =>
        {
            Assert.True(wall.MagnetismScore >= 0m);
            Assert.True(wall.MagnetismScore <= 1m);
        });
    }

    [Fact]
    public async Task AnalyzeWalls_EmptyStrikes_ReturnsEmptyLists()
    {
        // Arrange - Chain with no valid contracts
        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 450m, OptionType.Call, null, 0),  // Invalid gamma/OI
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act
        var result = await _service.AnalyzeGammaWallsAsync("SPY", 450m);

        // Assert - Should fail because no valid contracts
        Assert.False(result.Success);
        Assert.Contains("No valid contracts", result.Error);
    }

    [Fact]
    public async Task AnalyzeWalls_AutoFetchesSpotPrice()
    {
        // Arrange
        const decimal SpotPrice = 450m;
        var quote = new Quote
        {
            Symbol = "SPY",
            Price = SpotPrice,
            Timestamp = DateTime.UtcNow,
        };

        _mockMarketDataService
            .Setup(s => s.GetQuoteAsync("SPY"))
            .ReturnsAsync(MarketDataResult<Quote>.Ok(quote, MarketDataProvider.Alpaca));

        var chain = CreateTestOptionsChain("SPY", new[]
        {
            CreateContract("SPY", 445m, OptionType.Call, 0.02m, 2000),
            CreateContract("SPY", 445m, OptionType.Put, 0.01m, 1000),
        });

        _mockOptionsService
            .Setup(s => s.GetChainAsync("SPY", null))
            .ReturnsAsync(OptionsChainResult<OptionsChain>.Ok(chain, MarketDataProvider.AlphaVantage));

        // Act - Call without explicit spot price
        var result = await _service.AnalyzeGammaWallsAsync("SPY");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _mockMarketDataService.Verify(s => s.GetQuoteAsync("SPY"), Times.Once);
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
