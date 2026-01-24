// Copyright (c) GexVisor. All rights reserved.

namespace GexVisor.Api.Tests.LoadTests;

/// <summary>
/// Metrics collected during load tests.
/// </summary>
public class LoadTestMetrics
{
    public string TestName { get; set; } = string.Empty;

    public int TotalRequests { get; set; }

    public int SuccessCount { get; set; }

    public int ErrorCount { get; set; }

    public TimeSpan TotalDuration { get; set; }

    // Latency percentiles (in milliseconds)
    public double P50Ms { get; set; }

    public double P95Ms { get; set; }

    public double P99Ms { get; set; }

    public double MinMs { get; set; }

    public double MaxMs { get; set; }

    // Cache metrics
    public long CacheHitsBefore { get; set; }

    public long CacheMissesBefore { get; set; }

    public long CacheHitsAfter { get; set; }

    public long CacheMissesAfter { get; set; }

    // Derived metrics
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessCount / TotalRequests * 100 : 0;

    public double ErrorRate => TotalRequests > 0 ? (double)ErrorCount / TotalRequests * 100 : 0;

    public double RequestsPerSecond => TotalDuration.TotalSeconds > 0 ? TotalRequests / TotalDuration.TotalSeconds : 0;

    public double CacheHitRate
    {
        get
        {
            var newHits = CacheHitsAfter - CacheHitsBefore;
            var newMisses = CacheMissesAfter - CacheMissesBefore;
            var total = newHits + newMisses;
            return total > 0 ? (double)newHits / total * 100 : 0;
        }
    }

    /// <summary>
    /// Calculate percentiles from a list of latencies.
    /// </summary>
    /// <returns></returns>
    public static LoadTestMetrics FromLatencies(string testName, List<double> latenciesMs, TimeSpan duration)
    {
        if (latenciesMs.Count == 0)
        {
            return new LoadTestMetrics
            {
                TestName = testName,
                TotalDuration = duration,
            };
        }

        var sorted = latenciesMs.OrderBy(x => x).ToList();
        var count = sorted.Count;

        return new LoadTestMetrics
        {
            TestName = testName,
            TotalRequests = count,
            SuccessCount = count,
            TotalDuration = duration,
            MinMs = sorted[0],
            MaxMs = sorted[count - 1],
            P50Ms = Percentile(sorted, 50),
            P95Ms = Percentile(sorted, 95),
            P99Ms = Percentile(sorted, 99),
        };
    }

    private static double Percentile(List<double> sorted, int percentile)
    {
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    /// <summary>
    /// Format metrics for test output.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"""
            === Load Test Results: {TestName} ===
            Total Requests:  {TotalRequests}
            Success:         {SuccessCount} ({SuccessRate:F1}%)
            Errors:          {ErrorCount} ({ErrorRate:F1}%)
            Duration:        {TotalDuration.TotalSeconds:F2}s
            Throughput:      {RequestsPerSecond:F1} req/s

            Latency:
              Min:   {MinMs:F0}ms
              p50:   {P50Ms:F0}ms
              p95:   {P95Ms:F0}ms
              p99:   {P99Ms:F0}ms
              Max:   {MaxMs:F0}ms

            Cache:
              Hit Rate:      {CacheHitRate:F1}%
            """;
    }
}
