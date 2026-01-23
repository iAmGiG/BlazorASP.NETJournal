// Copyright (c) GexVisor. All rights reserved.

using Bunit;
using FluentAssertions;
using GexVisor.UI.Components.Comparison;
using GexVisor.UI.Models;

namespace GexVisor.UI.Tests.Components;

/// <summary>
/// Tests for CorrelationMatrix component optimization (#81).
/// Verifies that symbol extraction only occurs when Correlations reference changes.
/// </summary>
public class CorrelationMatrixTests : TestContext
{
    [Fact]
    public void Component_WithNoCorrelations_DisplaysEmptyState()
    {
        // Arrange & Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, null));

        // Assert
        cut.Find(".empty-state").Should().NotBeNull();
        cut.Markup.Should().Contain("No correlation data available");
    }

    [Fact]
    public void Component_WithEmptyList_DisplaysEmptyState()
    {
        // Arrange & Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, new List<CorrelationMetrics>()));

        // Assert
        cut.Find(".empty-state").Should().NotBeNull();
    }

    [Fact]
    public void Component_WithCorrelations_ExtractsSymbolsCorrectly()
    {
        // Arrange
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.85m),
            this.CreateCorrelation("SPY", "IWM", 0.72m),
            this.CreateCorrelation("QQQ", "IWM", 0.68m),
        };

        // Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Assert - should have 3 unique symbols (SPY, QQQ, IWM) sorted alphabetically
        var headerCells = cut.FindAll(".matrix-cell.header");

        // First cell is corner, next 3 are column headers, next 3 are row headers
        var columnHeaders = headerCells.Skip(1).Take(3).Select(e => e.TextContent).ToList();
        columnHeaders.Should().BeEquivalentTo(new[] { "IWM", "QQQ", "SPY" });
    }

    [Fact]
    public void Component_WithCorrelations_DisplaysHeatmapGrid()
    {
        // Arrange
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.85m),
        };

        // Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Assert
        cut.Find(".heatmap-container").Should().NotBeNull();
        cut.FindAll(".matrix-cell.data").Should().NotBeEmpty();
    }

    [Fact]
    public void Component_WithCorrelations_DisplaysDetailedTable()
    {
        // Arrange
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.85m),
        };

        // Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Assert
        cut.Find(".pairwise-table").Should().NotBeNull();
        cut.Markup.Should().Contain("SPY");
        cut.Markup.Should().Contain("QQQ");
        cut.Markup.Should().Contain("0.850"); // Price correlation formatted to 3 decimals
    }

    [Fact]
    public void Component_WithStrongCorrelation_AppliesStrongClass()
    {
        // Arrange
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.85m), // Strong: >= 0.7
        };

        // Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Assert - should have "strong" CSS class
        var strongCells = cut.FindAll(".strong");
        strongCells.Should().NotBeEmpty();
    }

    [Fact]
    public void Component_WithModerateCorrelation_AppliesModerateClass()
    {
        // Arrange
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.55m), // Moderate: 0.4-0.7
        };

        // Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Assert - should have "moderate" CSS class
        var moderateCells = cut.FindAll(".moderate");
        moderateCells.Should().NotBeEmpty();
    }

    [Fact]
    public void Component_WithWeakCorrelation_AppliesWeakClass()
    {
        // Arrange
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.25m), // Weak: < 0.4
        };

        // Act
        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Assert - should have "weak" CSS class
        var weakCells = cut.FindAll(".weak");
        weakCells.Should().NotBeEmpty();
    }

    [Fact]
    public void SymbolExtraction_OnReferenceChange_UpdatesSymbols()
    {
        // Arrange - render with initial correlations
        var initialCorrelations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.85m),
        };

        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, initialCorrelations));

        // Verify initial symbols (QQQ, SPY)
        var initialHeaders = cut.FindAll(".matrix-cell.header").Skip(1).Take(2)
            .Select(e => e.TextContent).ToList();
        initialHeaders.Should().BeEquivalentTo(new[] { "QQQ", "SPY" });

        // Act - change Correlations parameter to NEW list reference
        var newCorrelations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("IWM", "DIA", 0.75m),
            this.CreateCorrelation("IWM", "SPY", 0.68m),
        };

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Correlations, newCorrelations));

        // Assert - should have new symbols (DIA, IWM, SPY)
        var updatedHeaders = cut.FindAll(".matrix-cell.header").Skip(1).Take(3)
            .Select(e => e.TextContent).ToList();
        updatedHeaders.Should().BeEquivalentTo(new[] { "DIA", "IWM", "SPY" });
    }

    [Fact]
    public void SymbolExtraction_WhenNullCorrelations_ClearsSymbols()
    {
        // Arrange - render with correlations
        var correlations = new List<CorrelationMetrics>
        {
            this.CreateCorrelation("SPY", "QQQ", 0.85m),
        };

        var cut = this.RenderComponent<CorrelationMatrix>(parameters => parameters
            .Add(p => p.Correlations, correlations));

        // Verify symbols exist
        cut.FindAll(".matrix-cell.header").Should().NotBeEmpty();

        // Act - set Correlations to null
        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Correlations, (List<CorrelationMetrics>?)null));

        // Assert - should show empty state
        cut.Find(".empty-state").Should().NotBeNull();
    }

    private CorrelationMetrics CreateCorrelation(string symbol1, string symbol2, decimal priceCorr)
    {
        return new CorrelationMetrics
        {
            Symbol1 = symbol1,
            Symbol2 = symbol2,
            PriceCorrelation = priceCorr,
            GexCorrelation = 0.5m,
            RegimeAlignment = 75.0m,
            RegimeFlipCorrelation = 60.0m,
            PerformanceDispersion = 0.15m,
        };
    }
}
