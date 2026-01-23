namespace GexVisor.UI.Configuration;

/// <summary>
/// Helper for determining US equity market hours.
/// Used for smart cache TTL decisions.
/// </summary>
public static class MarketHoursHelper
{
    // Market hours: 9:30 AM - 4:00 PM ET, Mon-Fri
    private static readonly TimeOnly MarketOpen = new(9, 30);
    private static readonly TimeOnly MarketClose = new(16, 0);

    /// <summary>
    /// Check if US equity markets are currently open.
    /// Returns false for weekends and outside 9:30-4:00 ET.
    /// </summary>
    /// <remarks>
    /// Does not account for market holidays. For production use,
    /// consider integrating a market calendar service.
    /// </remarks>
    public static bool IsMarketOpen()
    {
        var eastern = GetEasternTime();
        var dayOfWeek = eastern.DayOfWeek;
        var timeOfDay = TimeOnly.FromDateTime(eastern);

        // Weekend check
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        // Market hours check
        return timeOfDay >= MarketOpen && timeOfDay < MarketClose;
    }

    /// <summary>
    /// Get appropriate cache TTL based on market status.
    /// During market hours: 5 minutes (data changes frequently).
    /// After hours: 24 hours (no new data until next session).
    /// </summary>
    public static TimeSpan GetCacheTtl()
    {
        return IsMarketOpen()
            ? TimeSpan.FromMinutes(5)
            : TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Get current time in US Eastern timezone.
    /// </summary>
    private static DateTime GetEasternTime()
    {
        try
        {
            // Windows timezone ID
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            // Linux/macOS timezone ID
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
    }
}
