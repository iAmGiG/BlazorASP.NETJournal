// Copyright (c) GexVisor. All rights reserved.

using System.Globalization;
using Bunit;
using FluentAssertions;
using GexVisor.UI.Components.Comparison;
using GexVisor.UI.Models;

namespace GexVisor.UI.Tests.Components;

/// <summary>
/// Tests for RegimeDivergenceList component caching optimization (#80).
/// Verifies that GetSortedEvents() caching works correctly.
/// </summary>
public class RegimeDivergenceListTests : TestContext
{
    [Fact]
    public void Component_WithNoEvents_DisplaysEmptyState()
    {
        // Arrange & Act
        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, new List<RegimeDivergenceEvent>()));

        // Assert
        cut.Find(".empty-state").Should().NotBeNull();
        cut.Markup.Should().Contain("No regime divergence events detected");
    }

    [Fact]
    public void Component_WithEvents_DisplaysEventList()
    {
        // Arrange
        var events = this.CreateTestEvents(3);

        // Act
        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.FindAll(".divergence-event").Count.Should().Be(3);
        cut.Markup.Should().Contain("3 events");
    }

    [Fact]
    public void SortByDate_OrdersEventsDescending()
    {
        // Arrange
        var events = new List<RegimeDivergenceEvent>
        {
            this.CreateEvent("2024-01-01", 50m),
            this.CreateEvent("2024-01-03", 60m),
            this.CreateEvent("2024-01-02", 70m),
        };

        // Act
        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert - should be sorted by date descending (2024-01-03, 2024-01-02, 2024-01-01)
        // DateOnly renders in locale format (e.g., "1/3/2024")
        var eventDates = cut.FindAll(".event-date");
        eventDates[0].TextContent.Should().Contain("1/3/2024");
        eventDates[1].TextContent.Should().Contain("1/2/2024");
        eventDates[2].TextContent.Should().Contain("1/1/2024");
    }

    [Fact]
    public void SortByMagnitude_OrdersEventsByMagnitudeDescending()
    {
        // Arrange - create events with different divergence magnitudes
        var events = new List<RegimeDivergenceEvent>
        {
            // 2 assets different = 50% (min(1,1)/2)
            this.CreateEventWithMagnitude("2024-01-01", 2, 1, 1),

            // 4 assets 2-2 split = 50% (min(2,2)/4)
            this.CreateEventWithMagnitude("2024-01-02", 4, 2, 2),

            // 3 assets 2-1 split = 33% (min(2,1)/3)
            this.CreateEventWithMagnitude("2024-01-03", 3, 2, 1),
        };

        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Act - change to sort by magnitude
        var sortSelect = cut.Find(".sort-select");
        sortSelect.Change("magnitude");

        // Assert - should be sorted by magnitude descending (0.5, 0.5, 0.3)
        // Note: DivergenceMagnitude returns 0-1 range, not 0-100
        var magnitudes = cut.FindAll(".event-magnitude");

        // First two could be either 2024-01-01 or 2024-01-02 (both 0.5)
        magnitudes[0].TextContent.Should().Contain("0.5");
        magnitudes[1].TextContent.Should().Contain("0.5");
        magnitudes[2].TextContent.Should().Contain("0.3");
    }

    [Fact]
    public void Pagination_ShowsFirst10ByDefault()
    {
        // Arrange
        var events = this.CreateTestEvents(15);

        // Act
        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.FindAll(".divergence-event").Count.Should().Be(10);
        cut.Markup.Should().Contain("Show All 15 Events");
    }

    [Fact]
    public void Pagination_ShowAllButton_DisplaysAllEvents()
    {
        // Arrange
        var events = this.CreateTestEvents(15);
        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Act - click "Show All" button
        var showAllButton = cut.Find("button.btn-secondary");
        showAllButton.Click();

        // Assert
        cut.FindAll(".divergence-event").Count.Should().Be(15);
        cut.Markup.Should().Contain("Show Less");
    }

    [Fact]
    public void Pagination_ShowLessButton_DisplaysFirst10()
    {
        // Arrange
        var events = this.CreateTestEvents(15);
        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Act - click "Show All" then "Show Less"
        var showAllButton = cut.Find("button.btn-secondary");
        showAllButton.Click();
        cut.FindAll(".divergence-event").Count.Should().Be(15);

        var showLessButton = cut.Find("button.btn-secondary");
        showLessButton.Click();

        // Assert
        cut.FindAll(".divergence-event").Count.Should().Be(10);
    }

    [Fact]
    public void CacheInvalidation_OnParameterChange_UpdatesDisplay()
    {
        // Arrange - render with initial events
        var initialEvents = new List<RegimeDivergenceEvent>
        {
            this.CreateEvent("2024-01-01", 50m),
        };

        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, initialEvents));

        cut.FindAll(".divergence-event").Count.Should().Be(1);

        // Act - change Events parameter
        var newEvents = new List<RegimeDivergenceEvent>
        {
            this.CreateEvent("2024-01-01", 50m),
            this.CreateEvent("2024-01-02", 60m),
            this.CreateEvent("2024-01-03", 70m),
        };

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Events, newEvents));

        // Assert - should display new events
        cut.FindAll(".divergence-event").Count.Should().Be(3);
    }

    [Fact]
    public void CacheInvalidation_OnSortChange_ReordersEvents()
    {
        // Arrange - create events with different divergence magnitudes
        var events = new List<RegimeDivergenceEvent>
        {
            // 3 assets 2-1 split = 33% (min(2,1)/3)
            this.CreateEventWithMagnitude("2024-01-01", 3, 2, 1),

            // 4 assets 2-2 split = 50% (min(2,2)/4)
            this.CreateEventWithMagnitude("2024-01-02", 4, 2, 2),
        };

        var cut = this.RenderComponent<RegimeDivergenceList>(parameters => parameters
            .Add(p => p.Events, events));

        // Initial order: date descending (2024-01-02 first)
        // DateOnly renders in locale format (e.g., "1/2/2024")
        var eventDates = cut.FindAll(".event-date");
        eventDates[0].TextContent.Should().Contain("1/2/2024");

        // Act - change sort to magnitude
        var sortSelect = cut.Find(".sort-select");
        sortSelect.Change("magnitude");

        // Assert - should now be sorted by magnitude descending (0.5 first, then 0.3)
        // Note: DivergenceMagnitude returns 0-1 range, not 0-100
        var magnitudes = cut.FindAll(".event-magnitude");
        magnitudes[0].TextContent.Should().Contain("0.5");
        magnitudes[1].TextContent.Should().Contain("0.3");
    }

    private List<RegimeDivergenceEvent> CreateTestEvents(int count)
    {
        var events = new List<RegimeDivergenceEvent>();
        for (int i = 1; i <= count; i++)
        {
            events.Add(this.CreateEvent($"2024-01-{i:D2}", 50m + i));
        }

        return events;
    }

    private RegimeDivergenceEvent CreateEvent(string date, decimal gexValue)
    {
        return new RegimeDivergenceEvent
        {
            Date = DateOnly.Parse(date, CultureInfo.InvariantCulture),
            AssetRegimes = new Dictionary<string, GammaRegime>
            {
                { "SPY", GammaRegime.Positive },
                { "QQQ", GammaRegime.Negative },
            },
            AssetGexValues = new Dictionary<string, decimal>
            {
                { "SPY", gexValue },
                { "QQQ", -gexValue },
            },
            IsDispersionOpportunity = false,
        };
    }

    private RegimeDivergenceEvent CreateEventWithMagnitude(string date, int totalAssets, int positiveCount, int negativeCount)
    {
        var assetRegimes = new Dictionary<string, GammaRegime>();
        var assetGexValues = new Dictionary<string, decimal>();

        // Add positive gamma assets
        for (int i = 0; i < positiveCount; i++)
        {
            var symbol = $"POS{i + 1}";
            assetRegimes[symbol] = GammaRegime.Positive;
            assetGexValues[symbol] = 1000m + (i * 100);
        }

        // Add negative gamma assets
        for (int i = 0; i < negativeCount; i++)
        {
            var symbol = $"NEG{i + 1}";
            assetRegimes[symbol] = GammaRegime.Negative;
            assetGexValues[symbol] = -(500m + (i * 50));
        }

        return new RegimeDivergenceEvent
        {
            Date = DateOnly.Parse(date, CultureInfo.InvariantCulture),
            AssetRegimes = assetRegimes,
            AssetGexValues = assetGexValues,
            IsDispersionOpportunity = false,
        };
    }
}
