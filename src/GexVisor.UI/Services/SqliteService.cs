using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for executing SQLite queries in the browser using sql.js.
/// Provides a C# wrapper around the JavaScript sql.js library.
/// </summary>
public class SqliteService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly List<string> _openDatabases = [];
    private Task? _initTask;
    private bool _initialized;

    public SqliteService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Initialize the sql.js library. Must be called before any database operations.
    /// Thread-safe: caches the initialization Task to prevent duplicate JS calls.
    /// </summary>
    public Task InitAsync()
    {
        return _initTask ??= InitCoreAsync();
    }

    private async Task InitCoreAsync()
    {
        await _js.InvokeVoidAsync("SqlJsInterop.init");
        _initialized = true;
    }

    /// <summary>
    /// Open a database from a byte array.
    /// </summary>
    /// <param name="bytes">Database file bytes, or null for an empty database.</param>
    /// <returns>Database ID for subsequent operations.</returns>
    public async Task<string> OpenAsync(byte[]? bytes = null)
    {
        EnsureInitialized();

        var id = await _js.InvokeAsync<string>("SqlJsInterop.openDb", bytes);
        _openDatabases.Add(id);
        return id;
    }

    /// <summary>
    /// Execute a SQL query and return results as a list of dictionaries.
    /// </summary>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <param name="sql">SQL query to execute.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <returns>List of row objects as dictionaries.</returns>
    public async Task<List<Dictionary<string, object?>>> QueryAsync(
        string dbId,
        string sql,
        object[]? parameters = null)
    {
        EnsureInitialized();

        var result = await _js.InvokeAsync<JsonElement>("SqlJsInterop.exec", dbId, sql, parameters);
        return ParseJsonResult(result);
    }

    /// <summary>
    /// Execute a SQL query and return results as typed objects.
    /// </summary>
    /// <typeparam name="T">Type to deserialize rows into.</typeparam>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <param name="sql">SQL query to execute.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <returns>List of typed objects.</returns>
    public async Task<List<T>> QueryAsync<T>(
        string dbId,
        string sql,
        object[]? parameters = null) where T : class
    {
        EnsureInitialized();

        var json = await _js.InvokeAsync<JsonElement>("SqlJsInterop.exec", dbId, sql, parameters);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return json.Deserialize<List<T>>(options) ?? [];
    }

    /// <summary>
    /// Execute a SQL statement that doesn't return results (INSERT, UPDATE, DELETE, CREATE).
    /// </summary>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <param name="sql">SQL statement to execute.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <returns>Number of rows affected.</returns>
    public async Task<int> ExecuteAsync(
        string dbId,
        string sql,
        object[]? parameters = null)
    {
        EnsureInitialized();

        return await _js.InvokeAsync<int>("SqlJsInterop.run", dbId, sql, parameters);
    }

    /// <summary>
    /// Get list of tables in the database.
    /// </summary>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <returns>List of table information.</returns>
    public async Task<List<TableInfo>> GetTablesAsync(string dbId)
    {
        EnsureInitialized();

        return await QueryAsync<TableInfo>(dbId,
            "SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') AND name NOT LIKE 'sqlite_%' ORDER BY name");
    }

    /// <summary>
    /// Get column information for a table.
    /// </summary>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <param name="tableName">Name of the table.</param>
    /// <returns>List of column information.</returns>
    public async Task<List<ColumnInfo>> GetColumnsAsync(string dbId, string tableName)
    {
        EnsureInitialized();

        return await QueryAsync<ColumnInfo>(dbId, $"PRAGMA table_info('{tableName}')");
    }

    /// <summary>
    /// Export the database as a byte array.
    /// </summary>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <returns>Database file bytes.</returns>
    public async Task<byte[]> ExportAsync(string dbId)
    {
        EnsureInitialized();

        return await _js.InvokeAsync<byte[]>("SqlJsInterop.export", dbId);
    }

    /// <summary>
    /// Close a database and free resources.
    /// </summary>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    public async Task CloseAsync(string dbId)
    {
        if (!_initialized)
        {
            return;
        }

        await _js.InvokeVoidAsync("SqlJsInterop.close", dbId);
        _openDatabases.Remove(dbId);
    }

    /// <summary>
    /// Execute a scalar query and return a single value.
    /// </summary>
    /// <typeparam name="T">Expected type of the result.</typeparam>
    /// <param name="dbId">Database ID from OpenAsync.</param>
    /// <param name="sql">SQL query that returns a single value.</param>
    /// <returns>The scalar result, or default if no result.</returns>
    public async Task<T?> ExecuteScalarAsync<T>(string dbId, string sql)
    {
        var results = await QueryAsync(dbId, sql);
        if (results.Count == 0)
        {
            return default;
        }

        var firstRow = results[0];
        if (firstRow.Count == 0)
        {
            return default;
        }

        var value = firstRow.Values.First();
        if (value is JsonElement element)
        {
            return element.Deserialize<T>();
        }

        // Handle null values
        if (value == null)
        {
            return default;
        }

        // Direct cast if types match
        if (value is T typed)
        {
            return typed;
        }

        // Use Convert.ChangeType for numeric conversions (e.g., long -> int, double -> decimal)
        // This handles cases where QueryAsync returns long but user expects int
        try
        {
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (InvalidCastException)
        {
            // Type conversion failed, return default
            return default;
        }
        catch (FormatException)
        {
            // Value format incompatible with target type
            return default;
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("SqliteService not initialized. Call InitAsync() first.");
        }
    }

    private static List<Dictionary<string, object?>> ParseJsonResult(JsonElement json)
    {
        var results = new List<Dictionary<string, object?>>();

        if (json.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var row in json.EnumerateArray())
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in row.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.GetRawText()
                };
            }
            results.Add(dict);
        }

        return results;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_initialized)
        {
            return;
        }

        foreach (var dbId in _openDatabases.ToList())
        {
            try
            {
                await CloseAsync(dbId);
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        GC.SuppressFinalize(this);
    }
}

// === DTOs ===

/// <summary>
/// Information about a database table or view.
/// </summary>
public record TableInfo
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
}

/// <summary>
/// Information about a table column (from PRAGMA table_info).
/// </summary>
public record ColumnInfo
{
    public int Cid { get; init; }
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public int Notnull { get; init; }
    public string? Dflt_value { get; init; }
    public int Pk { get; init; }
}
