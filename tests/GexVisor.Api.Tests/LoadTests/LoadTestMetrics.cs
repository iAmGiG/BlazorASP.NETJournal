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
    public double SuccessRate => this.TotalRequests > 0 ? (double)this.SuccessCount / this.TotalRequests * 100 : 0;

    public double ErrorRate => this.TotalRequests > 0 ? (double)this.ErrorCount / this.TotalRequests * 100 : 0;

    public double RequestsPerSecond => this.TotalDuration.TotalSeconds > 0 ? this.TotalRequests / this.TotalDuration.TotalSeconds : 0;

    public double CacheHitRate
    {
        get
        {
            var newHits = this.CacheHitsAfter - this.CacheHitsBefore;
            var newMisses = this.CacheMissesAfter - this.CacheMissesBefore;
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
            === Load Test Results: {this.TestName} ===
            Total Requests:  {this.TotalRequests}
            Success:         {this.SuccessCount} ({this.SuccessRate:F1}%)
            Errors:          {this.ErrorCount} ({this.ErrorRate:F1}%)
            Duration:        {this.TotalDuration.TotalSeconds:F2}s
            Throughput:      {this.RequestsPerSecond:F1} req/s

            Latency:
              Min:   {this.MinMs:F0}ms
              p50:   {this.P50Ms:F0}ms
              p95:   {this.P95Ms:F0}ms
              p99:   {this.P99Ms:F0}ms
              Max:   {this.MaxMs:F0}ms

            Cache:
              Hit Rate:      {this.CacheHitRate:F1}%
            """;
    }
}
