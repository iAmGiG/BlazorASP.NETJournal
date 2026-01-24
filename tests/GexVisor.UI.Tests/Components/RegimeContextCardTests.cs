// Copyright (c) GexVisor. All rights reserved.

using Bunit;
using FluentAssertions;
using GexVisor.UI.Components.TradeJournal;
using GexVisor.UI.Models;

namespace GexVisor.UI.Tests.Components;

/// <summary>
/// Tests for RegimeContextCard component.
/// Verifies regime display, metrics rendering, and conditional sections.
/// </summary>
public class RegimeContextCardTests : TestContext
{
    [Fact]
    public void Component_WithNullDecision_DoesNotRenderCard()
    {
        // Arrange & Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, null));

        // Assert
        cut.FindAll(".regime-context-card").Should().BeEmpty();
    }

    [Fact]
    public void Component_WithDecision_RendersCard()
    {
        // Arrange
        var decision = new TradeDecision
        {
            RegimeType = "bullish",
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Find(".regime-context-card").Should().NotBeNull();
        cut.Markup.Should().Contain("Market Regime Context");
    }

    [Fact]
    public void Component_WithRegimeType_DisplaysRegime()
    {
        // Arrange
        var decision = new TradeDecision
        {
            RegimeType = "bullish",
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Find(".regime-badge").TextContent.Should().Be("bullish");
    }

    [Theory]
    [InlineData("bullish", "bullish")]
    [InlineData("bearish", "bearish")]
    [InlineData("volatile", "volatile")]
    [InlineData("calm", "calm")]
    [InlineData("neutral", "neutral")]
    public void Component_AppliesCorrectRegimeBadgeClass(string regimeType, string expectedClass)
    {
        // Arrange
        var decision = new TradeDecision
        {
            RegimeType = regimeType,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Find(".regime-badge").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Component_WithGexLevel_DisplaysGexValue()
    {
        // Arrange
        var decision = new TradeDecision
        {
            GexLevel = 2.5m,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Markup.Should().Contain("+2.50B");
    }

    [Fact]
    public void Component_WithNegativeGexLevel_DisplaysNegativeValue()
    {
        // Arrange
        var decision = new TradeDecision
        {
            GexLevel = -1.8m,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Markup.Should().Contain("-1.80B");
    }

    [Fact]
    public void Component_WithHighGexLevel_AppliesHighClass()
    {
        // Arrange
        var decision = new TradeDecision
        {
            GexLevel = 3.0m,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        var gexValue = cut.FindAll(".metric-value")
            .FirstOrDefault(e => e.TextContent.Contains("+3.00B"));
        gexValue.Should().NotBeNull();
        gexValue!.ClassList.Should().Contain("high");
    }

    [Fact]
    public void Component_WithLowGexLevel_AppliesLowClass()
    {
        // Arrange
        var decision = new TradeDecision
        {
            GexLevel = -2.0m,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        var gexValue = cut.FindAll(".metric-value")
            .FirstOrDefault(e => e.TextContent.Contains("-2.00B"));
        gexValue.Should().NotBeNull();
        gexValue!.ClassList.Should().Contain("low");
    }

    [Fact]
    public void Component_WithIvLevel_DisplaysPercentage()
    {
        // Arrange
        var decision = new TradeDecision
        {
            IvLevel = 0.25m,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Markup.Should().Contain("25.0%");
    }

    [Fact]
    public void Component_WithNegativeGamma_DisplaysNegativeGamma()
    {
        // Arrange
        var decision = new TradeDecision
        {
            IsNegativeGamma = true,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Markup.Should().Contain("Negative Gamma");
        var gammaValue = cut.FindAll(".gamma-env")
            .FirstOrDefault(e => e.TextContent.Contains("Negative Gamma"));
        gammaValue.Should().NotBeNull();
        gammaValue!.ClassList.Should().Contain("negative");
    }

    [Fact]
    public void Component_WithPositiveGamma_DisplaysPositiveGamma()
    {
        // Arrange
        var decision = new TradeDecision
        {
            IsNegativeGamma = false,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Markup.Should().Contain("Positive Gamma");
        var gammaValue = cut.FindAll(".gamma-env")
            .FirstOrDefault(e => e.TextContent.Contains("Positive Gamma"));
        gammaValue.Should().NotBeNull();
        gammaValue!.ClassList.Should().Contain("positive");
    }

    [Fact]
    public void Component_WithConfidenceScore_DisplaysProgressBar()
    {
        // Arrange
        var decision = new TradeDecision
        {
            ConfidenceScore = 0.85m,
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Find(".confidence-bar").Should().NotBeNull();
        cut.Find(".confidence-fill").GetAttribute("style").Should().Contain("width: 85%");
        cut.Find(".confidence-text").TextContent.Should().Be("85%");
    }

    [Fact]
    public void Component_WithRiskAssessment_DisplaysRiskSection()
    {
        // Arrange
        var decision = new TradeDecision
        {
            RiskAssessment = "High volatility expected around FOMC announcement",
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Find(".risk-assessment").Should().NotBeNull();
        cut.Markup.Should().Contain("High volatility expected around FOMC announcement");
    }

    [Fact]
    public void Component_WithoutRiskAssessment_DoesNotDisplayRiskSection()
    {
        // Arrange
        var decision = new TradeDecision
        {
            RegimeType = "bullish",
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.FindAll(".risk-assessment").Should().BeEmpty();
    }

    [Fact]
    public void Component_WithAllMetrics_DisplaysComplete()
    {
        // Arrange
        var decision = new TradeDecision
        {
            RegimeType = "bullish",
            GexLevel = 2.5m,
            IvLevel = 0.25m,
            IsNegativeGamma = false,
            ConfidenceScore = 0.85m,
            RiskAssessment = "Low risk environment",
        };

        // Act
        var cut = RenderComponent<RegimeContextCard>(parameters => parameters
            .Add(p => p.Decision, decision));

        // Assert
        cut.Markup.Should().Contain("bullish");
        cut.Markup.Should().Contain("+2.50B");
        cut.Markup.Should().Contain("25.0%");
        cut.Markup.Should().Contain("Positive Gamma");
        cut.Markup.Should().Contain("85%");
        cut.Markup.Should().Contain("Low risk environment");
    }
}
