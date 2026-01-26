// Copyright (c) GexVisor. All rights reserved.

using GexVisor.Api.Services;
using Microsoft.Data.Sqlite;

namespace GexVisor.Api.Tests;

/// <summary>
/// Unit tests for SqliteCacheService.
/// Tests cache operations, TTL handling, and thread safety.
/// </summary>
public sealed class SqliteCacheServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteCacheService _cache;

    public SqliteCacheServiceTests()
    {
        // Create unique test database for each test run
        _testDbPath = Path.Combine(Path.GetTempPath(), $"gexvisor_test_{Guid.NewGuid()}.db");
        _cache = new SqliteCacheService(_testDbPath);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cache.Dispose();

        // Clear SQLite connection pools to release file locks
        SqliteConnection.ClearAllPools();

        // Clean up test database with retry
        for (int i = 0; i < 3; i++)
        {
            try
            {
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                }

                break;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _cache.GetAsync<TestData>("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        // Arrange
        var data = new TestData { Id = 1, Name = "Test" };

        // Act
        await _cache.SetAsync("test-key", data);
        var result = await _cache.GetAsync<TestData>("test-key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        var data1 = new TestData { Id = 1, Name = "First" };
        var data2 = new TestData { Id = 2, Name = "Second" };

        // Act
        await _cache.SetAsync("key", data1);
        await _cache.SetAsync("key", data2);
        var result = await _cache.GetAsync<TestData>("key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Second", result.Name);
    }

    [Fact]
    public async Task GetOrFetchAsync_MissingKey_FetchesAndCaches()
    {
        // Arrange
        var fetchCount = 0;
        Task<TestData> Fetcher()
        {
            fetchCount++;
            return Task.FromResult(new TestData { Id = 42, Name = "Fetched" });
        }

        // Act
        var result1 = await _cache.GetOrFetchAsync("fetch-key", Fetcher);
        var result2 = await _cache.GetOrFetchAsync("fetch-key", Fetcher);

        // Assert
        Assert.Equal(1, fetchCount); // Should only fetch once
        Assert.NotNull(result1);
        Assert.Equal(42, result1.Id);
        Assert.Equal(42, result2.Id);
    }

    [Fact]
    public async Task GetOrFetchAsync_ExistingKey_ReturnsCachedWithoutFetch()
    {
        // Arrange
        var data = new TestData { Id = 1, Name = "Cached" };
        await _cache.SetAsync("existing-key", data);

        var fetchCalled = false;
        Task<TestData> Fetcher()
        {
            fetchCalled = true;
            return Task.FromResult(new TestData { Id = 999, Name = "Should Not Be Called" });
        }

        // Act
        var result = await _cache.GetOrFetchAsync("existing-key", Fetcher);

        // Assert
        Assert.False(fetchCalled);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task SetAsync_WithTtl_ExpiresAfterDuration()
    {
        // Arrange
        var data = new TestData { Id = 1, Name = "Short-lived" };
        var shortTtl = TimeSpan.FromSeconds(2);

        // Act
        await _cache.SetAsync("expiring-key", data, shortTtl);

        // Should exist immediately
        var beforeExpiry = await _cache.GetAsync<TestData>("expiring-key");
        Assert.NotNull(beforeExpiry);

        // Wait for expiration
        await Task.Delay(2500);

        // Should be expired now
        var afterExpiry = await _cache.GetAsync<TestData>("expiring-key");
        Assert.Null(afterExpiry);
    }

    [Fact]
    public async Task SetAsync_WithoutTtl_DoesNotExpire()
    {
        // Arrange
        var data = new TestData { Id = 1, Name = "Permanent" };

        // Act
        await _cache.SetAsync("permanent-key", data);

        // Assert (no TTL means no expiration)
        var result = await _cache.GetAsync<TestData>("permanent-key");
        Assert.NotNull(result);
    }

    [Fact]
    public void SmartTtl_RecentData_Uses24Hours()
    {
        Assert.Equal(TimeSpan.FromHours(24), SqliteCacheService.DefaultRecentTtl);
    }

    [Fact]
    public void SmartTtl_HistoricalData_Uses10Years()
    {
        Assert.Equal(TimeSpan.FromDays(3650), SqliteCacheService.DefaultHistoricalTtl);
    }

    [Fact]
    public async Task InvalidateAsync_ExactMatch_RemovesEntry()
    {
        // Arrange
        await _cache.SetAsync("to-delete", new TestData { Id = 1 });

        // Act
        var deleted = await _cache.InvalidateAsync("to-delete");

        // Assert
        Assert.Equal(1, deleted);
        var result = await _cache.GetAsync<TestData>("to-delete");
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_WildcardPattern_RemovesMatchingEntries()
    {
        // Arrange
        await _cache.SetAsync("quote:SPY", new TestData { Id = 1 });
        await _cache.SetAsync("quote:QQQ", new TestData { Id = 2 });
        await _cache.SetAsync("bars:SPY", new TestData { Id = 3 });

        // Act
        var deleted = await _cache.InvalidateAsync("quote:*");

        // Assert
        Assert.Equal(2, deleted);
        Assert.Null(await _cache.GetAsync<TestData>("quote:SPY"));
        Assert.Null(await _cache.GetAsync<TestData>("quote:QQQ"));
        Assert.NotNull(await _cache.GetAsync<TestData>("bars:SPY"));
    }

    [Fact]
    public async Task CleanupExpiredAsync_RemovesExpiredEntries()
    {
        // Arrange
        await _cache.SetAsync("expired", new TestData { Id = 1 }, TimeSpan.FromSeconds(1));
        await _cache.SetAsync("valid", new TestData { Id = 2 }, TimeSpan.FromHours(1));

        // Wait for first to expire
        await Task.Delay(1500);

        // Act
        var deleted = await _cache.CleanupExpiredAsync();

        // Assert
        Assert.Equal(1, deleted);
        Assert.Null(await _cache.GetAsync<TestData>("expired"));
        Assert.NotNull(await _cache.GetAsync<TestData>("valid"));
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        await _cache.SetAsync("key1", new TestData { Id = 1 });
        await _cache.SetAsync("key2", new TestData { Id = 2 });

        // Trigger some hits and misses
        await _cache.GetAsync<TestData>("key1"); // Hit
        await _cache.GetAsync<TestData>("key1"); // Hit
        await _cache.GetAsync<TestData>("nonexistent"); // Miss

        // Act
        var stats = await _cache.GetStatsAsync();

        // Assert
        Assert.Equal(2, stats.TotalEntries);
        Assert.Equal(2, stats.TotalHits);
        Assert.Equal(1, stats.TotalMisses);
        Assert.True(stats.HitRate > 60); // ~66.7%
    }

    [Fact]
    public async Task GetStatsAsync_ReportsDatabaseSize()
    {
        // Arrange
        await _cache.SetAsync("data", new TestData { Id = 1, Name = "Some data" });

        // Act
        var stats = await _cache.GetStatsAsync();

        // Assert
        Assert.True(stats.DatabaseSizeBytes > 0);
    }

    [Fact]
    public async Task ConcurrentWrites_NoCorruption()
    {
        // Arrange
        var tasks = new List<Task>();
        var errors = 0;

        // Act - Many concurrent writes
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _cache.SetAsync($"concurrent-{index}", new TestData { Id = index });
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(0, errors);

        // Verify all writes succeeded
        var stats = await _cache.GetStatsAsync();
        Assert.Equal(100, stats.TotalEntries);
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_NoCorruption()
    {
        // Arrange
        await _cache.SetAsync("shared-key", new TestData { Id = 0 });
        var tasks = new List<Task>();
        var errors = 0;

        // Act - Many concurrent reads and writes
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _cache.GetAsync<TestData>("shared-key");
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _cache.SetAsync($"write-{index}", new TestData { Id = index });
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(0, errors);
    }

    // Test data class for serialization
    private sealed class TestData
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
