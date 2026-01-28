// Copyright (c) GexVisor. All rights reserved.

using System.Collections.Concurrent;
using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for backfilling historical GEX calculations.
/// Implements rate limiting, progress tracking, and resume capability.
/// </summary>
public class HistoricalBackfillService : IHistoricalBackfillService, IDisposable
{
    private readonly IGexCalculationService _gexService;
    private readonly IMarketDataService _marketDataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<HistoricalBackfillService> _logger;

    // Job tracking
    private readonly ConcurrentDictionary<string, BackfillJob> _jobs = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();

    // Rate limiting (Alpha Vantage: 75 calls/min, we use 70 to be safe)
    private const int RateLimitCallsPerMinute = 70;
    private const int RateLimitWindowMs = 60_000;
    private readonly SemaphoreSlim _rateLimitSemaphore = new(RateLimitCallsPerMinute, RateLimitCallsPerMinute);
    private readonly Timer _rateLimitRefreshTimer;

    // Cache key prefix for backfilled GEX data
    private const string GexCachePrefix = "gex:historical:";

    public HistoricalBackfillService(
        IGexCalculationService gexService,
        IMarketDataService marketDataService,
        ICacheService cacheService,
        ILogger<HistoricalBackfillService> logger)
    {
        _gexService = gexService;
        _marketDataService = marketDataService;
        _cacheService = cacheService;
        _logger = logger;

        // Refresh rate limit tokens every minute
        _rateLimitRefreshTimer = new Timer(
            RefreshRateLimitTokens,
            null,
            RateLimitWindowMs,
            RateLimitWindowMs);
    }

    public async Task<BackfillJobResult> StartBackfillAsync(
        IEnumerable<string> symbols,
        DateOnly startDate,
        DateOnly? endDate = null)
    {
        var symbolList = symbols.Select(s => s.ToUpperInvariant()).Distinct().ToList();

        if (symbolList.Count == 0)
        {
            return BackfillJobResult.Fail("No symbols provided");
        }

        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (startDate > end)
        {
            return BackfillJobResult.Fail("Start date must be before or equal to end date");
        }

        // Generate job ID
        var jobId = $"backfill-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..8]}";

        // Calculate total trading days (approximate - excludes weekends)
        var totalDays = CountTradingDays(startDate, end) * symbolList.Count;

        var job = new BackfillJob
        {
            JobId = jobId,
            Symbols = symbolList,
            StartDate = startDate,
            EndDate = end,
            TotalDays = totalDays,
            State = BackfillState.Running,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _jobs[jobId] = job;

        var cts = new CancellationTokenSource();
        _cancellationTokens[jobId] = cts;

        // Start background processing
        _ = Task.Run(() => ProcessBackfillAsync(job, cts.Token), cts.Token);

        _logger.LogInformation(
            "Started backfill job {JobId} for {SymbolCount} symbols from {StartDate} to {EndDate} (~{TotalDays} days)",
            jobId, symbolList.Count, startDate, end, totalDays);

        return BackfillJobResult.Success(jobId);
    }

    public BackfillStatus? GetStatus(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job.ToStatus() : null;
    }

    public IReadOnlyList<BackfillStatus> GetAllStatuses()
    {
        return _jobs.Values
            .OrderByDescending(j => j.StartedAt)
            .Select(j => j.ToStatus())
            .ToList();
    }

    public bool StopBackfill(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        if (job.State is BackfillState.Completed or BackfillState.Failed or BackfillState.Stopped)
        {
            return false;
        }

        if (_cancellationTokens.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            _cancellationTokens.TryRemove(jobId, out _);
        }

        job.State = BackfillState.Stopped;
        job.LastActivityAt = DateTime.UtcNow;

        _logger.LogInformation("Stopped backfill job {JobId}", jobId);
        return true;
    }

    public async Task<bool> ResumeBackfillAsync(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        if (job.State is not (BackfillState.Stopped or BackfillState.Failed or BackfillState.RateLimited))
        {
            return false;
        }

        var cts = new CancellationTokenSource();
        _cancellationTokens[jobId] = cts;

        job.State = BackfillState.Running;
        job.LastError = null;
        job.LastActivityAt = DateTime.UtcNow;

        // Resume from where we left off
        _ = Task.Run(() => ProcessBackfillAsync(job, cts.Token), cts.Token);

        _logger.LogInformation("Resumed backfill job {JobId}", jobId);
        return true;
    }

    private async Task ProcessBackfillAsync(BackfillJob job, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var symbol in job.Symbols)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                job.CurrentSymbol = symbol;

                // Iterate through trading days
                var currentDate = job.LastProcessedDate?.AddDays(1) ?? job.StartDate;

                while (currentDate <= job.EndDate)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Skip weekends
                    var dayOfWeek = currentDate.DayOfWeek;
                    if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    job.CurrentDate = currentDate;
                    job.LastActivityAt = DateTime.UtcNow;

                    try
                    {
                        // Wait for rate limit token
                        await AcquireRateLimitTokenAsync(job, cancellationToken);

                        // Fetch and calculate GEX for this date
                        await ProcessDateAsync(job, symbol, currentDate);

                        job.CompletedDays++;
                        job.ApiCallsMade++;
                        job.LastProcessedDate = currentDate;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        job.FailedDays++;
                        job.LastError = $"{symbol} {currentDate}: {ex.Message}";
                        _logger.LogWarning(ex, "Failed to process {Symbol} for {Date}", symbol, currentDate);

                        // Continue with next date on failure
                    }

                    currentDate = currentDate.AddDays(1);
                }

                // Reset for next symbol
                job.LastProcessedDate = null;
            }

            // Mark as completed
            if (!cancellationToken.IsCancellationRequested)
            {
                job.State = BackfillState.Completed;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Completed backfill job {JobId}: {Completed} days processed, {Failed} failed, {Records} records stored",
                    job.JobId, job.CompletedDays, job.FailedDays, job.GexRecordsStored);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Backfill job {JobId} was cancelled", job.JobId);
        }
        catch (Exception ex)
        {
            job.State = BackfillState.Failed;
            job.LastError = ex.Message;
            _logger.LogError(ex, "Backfill job {JobId} failed", job.JobId);
        }
        finally
        {
            job.CurrentSymbol = null;
            job.CurrentDate = null;
            job.LastActivityAt = DateTime.UtcNow;
        }
    }

    private async Task ProcessDateAsync(
        BackfillJob job,
        string symbol,
        DateOnly date)
    {
        // Get historical price data using daily bars
        var barsResult = await _marketDataService.GetBarsAsync(symbol, BarTimeframe.Day, 30);

        if (!barsResult.Success || barsResult.Data == null || barsResult.Data.Count == 0)
        {
            _logger.LogDebug("No bar data for {Symbol}", symbol);
            return;
        }

        // Find the bar for the target date (or closest available)
        var targetBar = barsResult.Data
            .Where(b => DateOnly.FromDateTime(b.Timestamp) <= date)
            .OrderByDescending(b => b.Timestamp)
            .FirstOrDefault();

        if (targetBar == null)
        {
            _logger.LogDebug("No bar data for {Symbol} on or before {Date}", symbol, date);
            return;
        }

        var spotPrice = targetBar.Close;

        // Calculate GEX using current options chain with historical spot price
        // Note: This uses current options data - for true historical GEX,
        // we would need historical options chain data (requires premium data source)
        var gexResult = await _gexService.CalculateGexAsync(symbol, spotPrice);

        if (!gexResult.Success || gexResult.Data == null)
        {
            _logger.LogDebug("No GEX calculation for {Symbol} on {Date}", symbol, date);
            return;
        }

        // Create historical record with adjusted timestamp
        var historicalGex = gexResult.Data with
        {
            Timestamp = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        };

        // Store in cache with long TTL (historical data is immutable)
        var cacheKey = $"{GexCachePrefix}{symbol}:{date:yyyy-MM-dd}";
        await _cacheService.SetAsync(cacheKey, historicalGex, TimeSpan.FromDays(3650));

        job.GexRecordsStored++;

        _logger.LogDebug(
            "Stored GEX for {Symbol} on {Date}: TotalGex={TotalGex:N0}, Regime={Regime}",
            symbol, date, historicalGex.TotalGex, historicalGex.Regime);
    }

    private async Task AcquireRateLimitTokenAsync(BackfillJob job, CancellationToken cancellationToken)
    {
        var acquired = await _rateLimitSemaphore.WaitAsync(0, cancellationToken);

        if (!acquired)
        {
            // Need to wait for rate limit
            job.State = BackfillState.RateLimited;
            job.ApiCallsRemaining = 0;

            _logger.LogDebug("Rate limited, waiting for token...");

            await _rateLimitSemaphore.WaitAsync(cancellationToken);

            job.State = BackfillState.Running;
        }

        job.ApiCallsRemaining = _rateLimitSemaphore.CurrentCount;
    }

    private void RefreshRateLimitTokens(object? state)
    {
        // Release all tokens back up to max
        var tokensToRelease = RateLimitCallsPerMinute - _rateLimitSemaphore.CurrentCount;
        if (tokensToRelease > 0)
        {
            _rateLimitSemaphore.Release(tokensToRelease);
            _logger.LogDebug("Refreshed {Tokens} rate limit tokens", tokensToRelease);
        }
    }

    private static int CountTradingDays(DateOnly start, DateOnly end)
    {
        var days = 0;
        var current = start;

        while (current <= end)
        {
            if (current.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                days++;
            }

            current = current.AddDays(1);
        }

        return days;
    }

    public void Dispose()
    {
        _rateLimitRefreshTimer.Dispose();
        _rateLimitSemaphore.Dispose();

        foreach (var cts in _cancellationTokens.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _cancellationTokens.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Internal job tracking class.
    /// </summary>
    private class BackfillJob
    {
        public required string JobId { get; init; }
        public required IReadOnlyList<string> Symbols { get; init; }
        public required DateOnly StartDate { get; init; }
        public required DateOnly EndDate { get; init; }
        public required int TotalDays { get; init; }
        public required DateTime StartedAt { get; init; }

        // Mutable state
        public BackfillState State { get; set; }
        public int CompletedDays { get; set; }
        public int FailedDays { get; set; }
        public int ApiCallsMade { get; set; }
        public int ApiCallsRemaining { get; set; }
        public int GexRecordsStored { get; set; }
        public string? CurrentSymbol { get; set; }
        public DateOnly? CurrentDate { get; set; }
        public DateOnly? LastProcessedDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string? LastError { get; set; }

        public BackfillStatus ToStatus() => new()
        {
            JobId = JobId,
            State = State,
            Symbols = Symbols,
            StartDate = StartDate,
            EndDate = EndDate,
            TotalDays = TotalDays,
            CompletedDays = CompletedDays,
            FailedDays = FailedDays,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            LastActivityAt = LastActivityAt,
            ApiCallsMade = ApiCallsMade,
            ApiCallsRemaining = ApiCallsRemaining,
            CurrentSymbol = CurrentSymbol,
            CurrentDate = CurrentDate,
            LastError = LastError,
            GexRecordsStored = GexRecordsStored
        };
    }
}
