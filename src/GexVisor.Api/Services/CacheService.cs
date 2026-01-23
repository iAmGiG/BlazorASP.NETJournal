using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace GexVisor.Api.Services;

/// <summary>
/// Cache statistics for monitoring and debugging.
/// </summary>
public record CacheStats
{
    public long TotalEntries { get; init; }
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public double HitRate => TotalHits + TotalMisses > 0
        ? (double)TotalHits / (TotalHits + TotalMisses) * 100
        : 0;
    public long ExpiredEntries { get; init; }
    public long DatabaseSizeBytes { get; init; }
}

/// <summary>
/// Generic cache service interface for market data caching.
/// Designed for future extensibility (PostgreSQL, Redis, etc.).
/// </summary>
public interface ICacheService
{
    /// <summary>Get a cached value by key.</summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>Set a cached value with optional TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;

    /// <summary>Get or fetch: returns cached value, or calls fetcher and caches result.</summary>
    Task<T> GetOrFetchAsync<T>(string key, Func<Task<T>> fetcher, TimeSpan? ttl = null) where T : class;

    /// <summary>Invalidate cache entries matching a pattern (supports * wildcard).</summary>
    Task<int> InvalidateAsync(string keyPattern);

    /// <summary>Get cache statistics.</summary>
    Task<CacheStats> GetStatsAsync();

    /// <summary>Clean up expired entries.</summary>
    Task<int> CleanupExpiredAsync();
}

/// <summary>
/// SQLite-based cache service for local market data caching.
/// Thread-safe with SemaphoreSlim for write serialization.
/// Ported from gex-llm-patterns Python implementation.
/// </summary>
public sealed class SqliteCacheService : ICacheService, IDisposable
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ILogger<SqliteCacheService>? _logger;
    private long _hits;
    private long _misses;
    private bool _disposed;

    // Smart TTL defaults (ported from Python gex-llm-patterns)
    public static readonly TimeSpan DefaultRecentTtl = TimeSpan.FromHours(24);
    public static readonly TimeSpan DefaultHistoricalTtl = TimeSpan.FromDays(3650); // 10 years

    public SqliteCacheService(string dbPath, ILogger<SqliteCacheService>? logger = null)
    {
        _dbPath = dbPath;
        _logger = logger;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        InitializeDatabase();
    }

    private string ConnectionString => $"Data Source={_dbPath};Pooling=false";

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS cache_entries (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key TEXT NOT NULL UNIQUE,
                value_json TEXT NOT NULL,
                value_type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT,
                hit_count INTEGER DEFAULT 0,
                last_accessed TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_cache_key ON cache_entries(key);
            CREATE INDEX IF NOT EXISTS idx_cache_expires ON cache_entries(expires_at);
            """;
        command.ExecuteNonQuery();

        _logger?.LogInformation("SQLite cache initialized at {Path}", _dbPath);
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT value_json, expires_at FROM cache_entries
            WHERE key = $key
            """;
        command.Parameters.AddWithValue("$key", key);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            Interlocked.Increment(ref _misses);
            return null;
        }

        var expiresAtStr = reader.IsDBNull(1) ? null : reader.GetString(1);
        if (expiresAtStr != null && DateTime.Parse(expiresAtStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind) < DateTime.UtcNow)
        {
            // Entry expired
            Interlocked.Increment(ref _misses);
            _ = InvalidateAsync(key); // Fire-and-forget cleanup
            return null;
        }

        var json = reader.GetString(0);
        Interlocked.Increment(ref _hits);

        // Update hit count and last accessed (fire-and-forget)
        _ = UpdateAccessStatsAsync(key);

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Failed to deserialize cache entry for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var json = JsonSerializer.Serialize(value);
        var now = DateTime.UtcNow;
        var expiresAt = ttl.HasValue ? now.Add(ttl.Value) : (DateTime?)null;

        await _writeLock.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT OR REPLACE INTO cache_entries
                (key, value_json, value_type, created_at, expires_at, hit_count, last_accessed)
                VALUES ($key, $value_json, $value_type, $created_at, $expires_at, 0, $last_accessed)
                """;
            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$value_json", json);
            command.Parameters.AddWithValue("$value_type", typeof(T).FullName ?? typeof(T).Name);
            command.Parameters.AddWithValue("$created_at", now.ToString("O"));
            command.Parameters.AddWithValue("$expires_at", expiresAt?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$last_accessed", now.ToString("O"));

            await command.ExecuteNonQueryAsync();
            _logger?.LogDebug("Cached {Key} with TTL {Ttl}", key, ttl);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<T> GetOrFetchAsync<T>(string key, Func<Task<T>> fetcher, TimeSpan? ttl = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        var value = await fetcher();
        await SetAsync(key, value, ttl);
        return value;
    }

    public async Task<int> InvalidateAsync(string keyPattern)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _writeLock.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            // Convert wildcard pattern to SQL LIKE pattern
            var sqlPattern = keyPattern.Replace("*", "%");

            command.CommandText = "DELETE FROM cache_entries WHERE key LIKE $pattern";
            command.Parameters.AddWithValue("$pattern", sqlPattern);

            var deleted = await command.ExecuteNonQueryAsync();
            _logger?.LogInformation("Invalidated {Count} cache entries matching {Pattern}", deleted, keyPattern);
            return deleted;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<CacheStats> GetStatsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        // Get entry counts
        using var countCmd = connection.CreateCommand();
        countCmd.CommandText = """
            SELECT
                COUNT(*) as total,
                SUM(hit_count) as total_hits,
                SUM(CASE WHEN expires_at IS NOT NULL AND expires_at < $now THEN 1 ELSE 0 END) as expired
            FROM cache_entries
            """;
        countCmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));

        long totalEntries = 0, totalHits = 0, expiredEntries = 0;
        using (var reader = await countCmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                totalEntries = reader.GetInt64(0);
                totalHits = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                expiredEntries = reader.GetInt64(2);
            }
        }

        // Get database size
        long dbSize = 0;
        if (File.Exists(_dbPath))
        {
            dbSize = new FileInfo(_dbPath).Length;
        }

        return new CacheStats
        {
            TotalEntries = totalEntries,
            TotalHits = Interlocked.Read(ref _hits),
            TotalMisses = Interlocked.Read(ref _misses),
            ExpiredEntries = expiredEntries,
            DatabaseSizeBytes = dbSize
        };
    }

    public async Task<int> CleanupExpiredAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _writeLock.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM cache_entries
                WHERE expires_at IS NOT NULL AND expires_at < $now
                """;
            command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));

            var deleted = await command.ExecuteNonQueryAsync();
            _logger?.LogInformation("Cleaned up {Count} expired cache entries", deleted);
            return deleted;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task UpdateAccessStatsAsync(string key)
    {
        try
        {
            await _writeLock.WaitAsync();
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = """
                    UPDATE cache_entries
                    SET hit_count = hit_count + 1, last_accessed = $now
                    WHERE key = $key
                    """;
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to update access stats for {Key}", key);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writeLock.Dispose();
    }
}
