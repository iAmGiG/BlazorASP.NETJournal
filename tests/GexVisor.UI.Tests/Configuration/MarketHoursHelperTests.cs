// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.UI.Configuration;

namespace GexVisor.UI.Tests.Configuration;

/// <summary>
/// Unit tests for MarketHoursHelper market hours detection.
/// </summary>
public class MarketHoursHelperTests
{
    [Fact]
    public void IsMarketOpen_DoesNotThrow()
    {
        // Act & Assert - verify it executes without throwing
        var action = () => MarketHoursHelper.IsMarketOpen();
        action.Should().NotThrow();
    }

    [Fact]
    public void GetCacheTtl_ReturnsExpectedValues()
    {
        // This test verifies the TTL logic
        // During market hours: 5 minutes, After hours: 24 hours
        var ttl = MarketHoursHelper.GetCacheTtl();

        // Either 5 minutes (market open) or 24 hours (market closed)
        var is5Min = ttl == TimeSpan.FromMinutes(5);
        var is24Hr = ttl == TimeSpan.FromHours(24);
        (is5Min || is24Hr).Should().BeTrue($"TTL was {ttl}, expected 5 min or 24 hours");
    }

    [Fact]
    public void GetCacheTtl_ReturnsReasonableDuration()
    {
        // Act
        var ttl = MarketHoursHelper.GetCacheTtl();

        // Assert - TTL should be positive and reasonable
        ttl.Should().BeGreaterThan(TimeSpan.Zero);
        ttl.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(24));
    }

    [Fact]
    public void GetCacheTtl_ReturnsConsistentValue()
    {
        // Act - call twice in quick succession
        var ttl1 = MarketHoursHelper.GetCacheTtl();
        var ttl2 = MarketHoursHelper.GetCacheTtl();

        // Assert - should return same value for same instant
        ttl1.Should().Be(ttl2);
    }
}
