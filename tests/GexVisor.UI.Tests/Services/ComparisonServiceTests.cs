// Copyright (c) GexVisor. All rights reserved.

using System.Globalization;
using FluentAssertions;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for ComparisonService selection and validation logic.
/// Tests multi-symbol selection constraints and state management.
/// </summary>
public class ComparisonServiceTests
{
    [Fact]
    public void ToggleSymbol_AddsSymbolWhenNotSelected()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.ToggleSymbol("SPY");

        // Assert
        service.IsSymbolSelected("SPY").Should().BeTrue();
        service.SelectedSymbols.Should().Contain("SPY");
        service.SelectionCount.Should().Be(1);
    }

    [Fact]
    public void ToggleSymbol_RemovesSymbolWhenAlreadySelected()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");

        // Act
        service.ToggleSymbol("SPY");

        // Assert
        service.IsSymbolSelected("SPY").Should().BeFalse();
        service.SelectedSymbols.Should().NotContain("SPY");
        service.SelectionCount.Should().Be(0);
    }

    [Fact]
    public void ToggleSymbol_EnforcesMaximum4Symbols()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");
        service.ToggleSymbol("QQQ");
        service.ToggleSymbol("IWM");
        service.ToggleSymbol("DIA");

        // Act
        service.ToggleSymbol("AAPL"); // 5th symbol

        // Assert
        service.SelectionCount.Should().Be(4);
        service.IsSymbolSelected("AAPL").Should().BeFalse();
        service.SelectedSymbols.Should().NotContain("AAPL");
    }

    [Fact]
    public void ToggleSymbol_FiresOnSelectionChanged()
    {
        // Arrange
        var service = CreateService();
        var eventFired = false;
        service.OnSelectionChanged += () => eventFired = true;

        // Act
        service.ToggleSymbol("SPY");

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void IsValidSelection_WithZeroSymbols_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.IsValidSelection.Should().BeFalse();
        service.SelectionCount.Should().Be(0);
    }

    [Fact]
    public void IsValidSelection_WithOneSymbol_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");

        // Assert
        service.IsValidSelection.Should().BeFalse();
        service.SelectionCount.Should().Be(1);
    }

    [Fact]
    public void IsValidSelection_WithTwoSymbols_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");
        service.ToggleSymbol("QQQ");

        // Assert
        service.IsValidSelection.Should().BeTrue();
        service.SelectionCount.Should().Be(2);
    }

    [Fact]
    public void IsValidSelection_WithFourSymbols_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");
        service.ToggleSymbol("QQQ");
        service.ToggleSymbol("IWM");
        service.ToggleSymbol("DIA");

        // Assert
        service.IsValidSelection.Should().BeTrue();
        service.SelectionCount.Should().Be(4);
    }

    [Fact]
    public void GetValidationMessage_WithZeroSymbols_ReturnsMessage()
    {
        // Arrange
        var service = CreateService();

        // Act
        var message = service.GetValidationMessage();

        // Assert
        message.Should().Be("Select at least 2 symbols to compare");
    }

    [Fact]
    public void GetValidationMessage_WithOneSymbol_ReturnsMessage()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");

        // Act
        var message = service.GetValidationMessage();

        // Assert
        message.Should().Be("Select at least 1 more symbol to compare");
    }

    [Fact]
    public void GetValidationMessage_WithValidSelection_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");
        service.ToggleSymbol("QQQ");

        // Act
        var message = service.GetValidationMessage();

        // Assert
        message.Should().BeNull();
    }

    [Fact]
    public void ClearSelection_RemovesAllSymbols()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");
        service.ToggleSymbol("QQQ");

        // Act
        service.ClearSelection();

        // Assert
        service.SelectedSymbols.Should().BeEmpty();
        service.LoadedAssets.Should().BeEmpty();
        service.SelectionCount.Should().Be(0);
    }

    [Fact]
    public void ClearSelection_FiresOnSelectionChanged()
    {
        // Arrange
        var service = CreateService();
        service.ToggleSymbol("SPY");
        var eventFired = false;
        service.OnSelectionChanged += () => eventFired = true;

        // Act
        service.ClearSelection();

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void GetAsset_ReturnsNullIfNotLoaded()
    {
        // Arrange
        var service = CreateService();

        // Act
        var asset = service.GetAsset("INVALID");

        // Assert
        asset.Should().BeNull();
    }

    private ComparisonService CreateService(Mock<IGexDataService>? mockDataService = null)
    {
        mockDataService ??= new Mock<IGexDataService>();
        return new ComparisonService(mockDataService.Object);
    }

    private static GexTimeline CreateMockTimeline(string symbol)
    {
        return new GexTimeline
        {
            Symbol = symbol,
            AssetClass = "Index",
            DateRange = new DateRange { Start = DateOnly.Parse("2024-01-01", CultureInfo.InvariantCulture), End = DateOnly.Parse("2024-12-31", CultureInfo.InvariantCulture) },
            Count = 10,
            Timeline = new List<GexDataPoint>
            {
                new() { Date = DateOnly.Parse("2024-01-01", CultureInfo.InvariantCulture), Price = 450, Gex = 1000, CallGex = 600, PutGex = 400, ZeroGamma = 445, MaxGamma = 460, Regime = "Positive Gamma", CallOi = 500000, PutOi = 400000, Contracts = 900000, Quality = 0.95m },
                new() { Date = DateOnly.Parse("2024-01-02", CultureInfo.InvariantCulture), Price = 455, Gex = 1100, CallGex = 650, PutGex = 450, ZeroGamma = 448, MaxGamma = 465, Regime = "Positive Gamma", CallOi = 510000, PutOi = 410000, Contracts = 920000, Quality = 0.95m },
                new() { Date = DateOnly.Parse("2024-01-03", CultureInfo.InvariantCulture), Price = 460, Gex = 1200, CallGex = 700, PutGex = 500, ZeroGamma = 450, MaxGamma = 470, Regime = "Positive Gamma", CallOi = 520000, PutOi = 420000, Contracts = 940000, Quality = 0.96m },
                new() { Date = DateOnly.Parse("2024-01-04", CultureInfo.InvariantCulture), Price = 448, Gex = -500, CallGex = 200, PutGex = -700, ZeroGamma = 455, MaxGamma = 465, Regime = "Negative Gamma", CallOi = 400000, PutOi = 600000, Contracts = 1000000, Quality = 0.94m },
                new() { Date = DateOnly.Parse("2024-01-05", CultureInfo.InvariantCulture), Price = 445, Gex = -600, CallGex = 150, PutGex = -750, ZeroGamma = 458, MaxGamma = 468, Regime = "Negative Gamma", CallOi = 380000, PutOi = 620000, Contracts = 1000000, Quality = 0.93m },
                new() { Date = DateOnly.Parse("2024-01-06", CultureInfo.InvariantCulture), Price = 442, Gex = -700, CallGex = 100, PutGex = -800, ZeroGamma = 460, MaxGamma = 470, Regime = "Negative Gamma", CallOi = 360000, PutOi = 640000, Contracts = 1000000, Quality = 0.92m },
                new() { Date = DateOnly.Parse("2024-01-07", CultureInfo.InvariantCulture), Price = 455, Gex = 800, CallGex = 500, PutGex = 300, ZeroGamma = 450, MaxGamma = 465, Regime = "Positive Gamma", CallOi = 480000, PutOi = 420000, Contracts = 900000, Quality = 0.94m },
                new() { Date = DateOnly.Parse("2024-01-08", CultureInfo.InvariantCulture), Price = 460, Gex = 900, CallGex = 550, PutGex = 350, ZeroGamma = 452, MaxGamma = 467, Regime = "Positive Gamma", CallOi = 490000, PutOi = 410000, Contracts = 900000, Quality = 0.95m },
                new() { Date = DateOnly.Parse("2024-01-09", CultureInfo.InvariantCulture), Price = 465, Gex = 1000, CallGex = 600, PutGex = 400, ZeroGamma = 454, MaxGamma = 469, Regime = "Positive Gamma", CallOi = 500000, PutOi = 400000, Contracts = 900000, Quality = 0.96m },
                new() { Date = DateOnly.Parse("2024-01-10", CultureInfo.InvariantCulture), Price = 470, Gex = 1100, CallGex = 650, PutGex = 450, ZeroGamma = 456, MaxGamma = 471, Regime = "Positive Gamma", CallOi = 510000, PutOi = 390000, Contracts = 900000, Quality = 0.97m },
            },
        };
    }

    // === LoadSymbolsAsync Tests ===
    [Fact]
    public async Task LoadSymbolsAsync_WithTwoValidSymbols_ReturnsTrue()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync("SPY")).ReturnsAsync(CreateMockTimeline("SPY"));
        mock.Setup(x => x.LoadSymbolAsync("QQQ")).ReturnsAsync(CreateMockTimeline("QQQ"));
        var service = CreateService(mock);

        // Act
        var result = await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ" });

        // Assert
        result.Should().BeTrue();
        service.LoadedAssets.Should().HaveCount(2);
        service.LoadedAssets.Should().ContainKey("SPY");
        service.LoadedAssets.Should().ContainKey("QQQ");
        service.SelectedSymbols.Should().BeEquivalentTo(new[] { "SPY", "QQQ" });
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithFourValidSymbols_LoadsInParallel()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync("SPY")).ReturnsAsync(CreateMockTimeline("SPY"));
        mock.Setup(x => x.LoadSymbolAsync("QQQ")).ReturnsAsync(CreateMockTimeline("QQQ"));
        mock.Setup(x => x.LoadSymbolAsync("IWM")).ReturnsAsync(CreateMockTimeline("IWM"));
        mock.Setup(x => x.LoadSymbolAsync("DIA")).ReturnsAsync(CreateMockTimeline("DIA"));
        var service = CreateService(mock);

        // Act
        var result = await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ", "IWM", "DIA" });

        // Assert
        result.Should().BeTrue();
        service.LoadedAssets.Should().HaveCount(4);
        mock.Verify(x => x.LoadSymbolAsync(It.IsAny<string>()), Times.Exactly(4));
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithLessThanTwoSymbols_ThrowsException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.LoadSymbolsAsync(new List<string> { "SPY" }));
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithMoreThanFourSymbols_ThrowsException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ", "IWM", "DIA", "AAPL" }));
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithOneInvalidSymbol_ReturnsNull()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync("SPY")).ReturnsAsync(CreateMockTimeline("SPY"));
        mock.Setup(x => x.LoadSymbolAsync("INVALID")).ReturnsAsync((GexTimeline?)null);
        var service = CreateService(mock);

        // Act
        var result = await service.LoadSymbolsAsync(new List<string> { "SPY", "INVALID" });

        // Assert
        result.Should().BeFalse();
        service.LoadedAssets.Should().HaveCount(1);
        service.LoadedAssets.Should().ContainKey("SPY");
        service.LoadedAssets.Should().NotContainKey("INVALID");
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithAllInvalidSymbols_ReturnsFalse()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync(It.IsAny<string>())).ReturnsAsync((GexTimeline?)null);
        var service = CreateService(mock);

        // Act
        var result = await service.LoadSymbolsAsync(new List<string> { "INVALID1", "INVALID2" });

        // Assert
        result.Should().BeFalse();
        service.LoadedAssets.Should().BeEmpty();
        service.SelectedSymbols.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithPartialFailures_KeepsSuccessfulLoads()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync("SPY")).ReturnsAsync(CreateMockTimeline("SPY"));
        mock.Setup(x => x.LoadSymbolAsync("QQQ")).ReturnsAsync(CreateMockTimeline("QQQ"));
        mock.Setup(x => x.LoadSymbolAsync("INVALID")).ReturnsAsync((GexTimeline?)null);
        var service = CreateService(mock);

        // Act
        var result = await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ", "INVALID" });

        // Assert
        result.Should().BeTrue(); // 2 succeeded, so returns true
        service.LoadedAssets.Should().HaveCount(2);
        service.LoadedAssets.Should().ContainKey("SPY");
        service.LoadedAssets.Should().ContainKey("QQQ");
        service.SelectedSymbols.Should().BeEquivalentTo(new[] { "SPY", "QQQ" });
    }

    [Fact]
    public async Task LoadSymbolsAsync_WithExceptionDuringLoad_HandlesGracefully()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync("SPY")).ReturnsAsync(CreateMockTimeline("SPY"));
        mock.Setup(x => x.LoadSymbolAsync("QQQ")).ThrowsAsync(new Exception("Network error"));
        var service = CreateService(mock);

        // Act
        var result = await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ" });

        // Assert
        result.Should().BeFalse(); // Only 1 succeeded, needs 2
        service.LoadedAssets.Should().HaveCount(1);
        service.LoadedAssets.Should().ContainKey("SPY");
        service.LoadedAssets.Should().NotContainKey("QQQ");
    }

    [Fact]
    public async Task LoadSymbolsAsync_FiresOnSelectionChanged()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync("SPY")).ReturnsAsync(CreateMockTimeline("SPY"));
        mock.Setup(x => x.LoadSymbolAsync("QQQ")).ReturnsAsync(CreateMockTimeline("QQQ"));
        var service = CreateService(mock);
        var eventFired = false;
        service.OnSelectionChanged += () => eventFired = true;

        // Act
        await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ" });

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public async Task LoadSymbolsAsync_ClearsPreviousState()
    {
        // Arrange
        var mock = new Mock<IGexDataService>();
        mock.Setup(x => x.LoadSymbolAsync(It.IsAny<string>())).ReturnsAsync((string s) => CreateMockTimeline(s));
        var service = CreateService(mock);

        // Load first set
        await service.LoadSymbolsAsync(new List<string> { "SPY", "QQQ" });
        service.LoadedAssets.Should().HaveCount(2);

        // Act - Load second set
        await service.LoadSymbolsAsync(new List<string> { "IWM", "DIA" });

        // Assert
        service.LoadedAssets.Should().HaveCount(2);
        service.LoadedAssets.Should().ContainKey("IWM");
        service.LoadedAssets.Should().ContainKey("DIA");
        service.LoadedAssets.Should().NotContainKey("SPY");
        service.LoadedAssets.Should().NotContainKey("QQQ");
    }
}
