using FluentAssertions;
using GexVisor.UI.Services;
using Xunit;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for ComparisonService selection and validation logic.
/// Tests multi-symbol selection constraints and state management.
///
/// NOTE: LoadSymbolsAsync tests require IGexDataService interface extraction.
/// See issue #87 for implementation details.
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

    private ComparisonService CreateService()
    {
        // Use a mock HttpClient-based GexDataService for selection-only tests
        var mockDataService = new GexDataService(new HttpClient());
        return new ComparisonService(mockDataService);
    }
}
