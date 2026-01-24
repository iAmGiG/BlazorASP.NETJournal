// Copyright (c) GexVisor. All rights reserved.

using Bunit;
using FluentAssertions;
using GexVisor.UI.Components.TradeJournal;

namespace GexVisor.UI.Tests.Components;

/// <summary>
/// Tests for PatternBadge component.
/// Verifies rendering, styling, and signal strength display.
/// </summary>
public class PatternBadgeTests : TestContext
{
    [Fact]
    public void Component_WithPatternName_DisplaysName()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning"));

        // Assert
        cut.Find(".pattern-name").TextContent.Should().Be("gamma_positioning");
    }

    [Fact]
    public void Component_WhenPrimary_HasPrimaryClass()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.IsPrimary, true));

        // Assert
        cut.Find(".pattern-badge").ClassList.Should().Contain("primary");
    }

    [Fact]
    public void Component_WhenNotPrimary_DoesNotHavePrimaryClass()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "stock_pinning")
            .Add(p => p.IsPrimary, false));

        // Assert
        cut.Find(".pattern-badge").ClassList.Should().NotContain("primary");
    }

    [Fact]
    public void Component_WithSignalStrengthShown_DisplaysStrength()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.SignalStrength, 0.85m)
            .Add(p => p.ShowStrength, true));

        // Assert
        cut.Find(".strength-indicator").TextContent.Should().Be("85%");
    }

    [Fact]
    public void Component_WithSignalStrengthHidden_DoesNotDisplayStrength()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.SignalStrength, 0.85m)
            .Add(p => p.ShowStrength, false));

        // Assert
        cut.FindAll(".strength-indicator").Should().BeEmpty();
    }

    [Fact]
    public void Component_WithSmallSize_HasSmallClass()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.Size, "small"));

        // Assert
        cut.Find(".pattern-badge").ClassList.Should().Contain("size-sm");
    }

    [Fact]
    public void Component_WithMediumSize_HasMediumClass()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.Size, "medium"));

        // Assert
        cut.Find(".pattern-badge").ClassList.Should().Contain("size-md");
    }

    [Fact]
    public void Component_WithLargeSize_HasLargeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.Size, "large"));

        // Assert
        cut.Find(".pattern-badge").ClassList.Should().Contain("size-lg");
    }

    [Fact]
    public void Component_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.Icon, "fa-chart-line"));

        // Assert
        cut.Markup.Should().Contain("fa-chart-line");
    }

    [Fact]
    public void Component_WithoutIcon_DoesNotDisplayIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning"));

        // Assert
        cut.FindAll("i.fas").Should().BeEmpty();
    }

    [Fact]
    public void Component_WithZeroSignalStrength_FormatsAsZeroPercent()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.SignalStrength, 0m)
            .Add(p => p.ShowStrength, true));

        // Assert
        cut.Find(".strength-indicator").TextContent.Should().Be("0%");
    }

    [Fact]
    public void Component_WithFullSignalStrength_FormatsAs100Percent()
    {
        // Arrange & Act
        var cut = RenderComponent<PatternBadge>(parameters => parameters
            .Add(p => p.PatternName, "gamma_positioning")
            .Add(p => p.SignalStrength, 1.0m)
            .Add(p => p.ShowStrength, true));

        // Assert
        cut.Find(".strength-indicator").TextContent.Should().Be("100%");
    }
}
