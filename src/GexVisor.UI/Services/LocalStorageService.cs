using Microsoft.JSInterop;
using System.Text.Json;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for persisting data to browser localStorage via JS interop.
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _js;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService(IJSRuntime js)
    {
        _js = js;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gets a value from localStorage and deserializes it.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _js.InvokeAsync<string?>("GexInterop.localStorage.getItem", key);
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Gets a raw string value from localStorage.
    /// </summary>
    public async Task<string?> GetStringAsync(string key)
    {
        return await _js.InvokeAsync<string?>("GexInterop.localStorage.getItem", key);
    }

    /// <summary>
    /// Serializes and stores a value in localStorage.
    /// </summary>
    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _js.InvokeVoidAsync("GexInterop.localStorage.setItem", key, json);
    }

    /// <summary>
    /// Stores a raw string value in localStorage.
    /// </summary>
    public async Task SetStringAsync(string key, string value)
    {
        await _js.InvokeVoidAsync("GexInterop.localStorage.setItem", key, value);
    }

    /// <summary>
    /// Removes an item from localStorage.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        await _js.InvokeVoidAsync("GexInterop.localStorage.removeItem", key);
    }

    /// <summary>
    /// Clears all items from localStorage.
    /// </summary>
    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("GexInterop.localStorage.clear");
    }

    /// <summary>
    /// Gets all keys in localStorage.
    /// </summary>
    public async Task<string[]> GetKeysAsync()
    {
        return await _js.InvokeAsync<string[]>("GexInterop.localStorage.getKeys");
    }

    /// <summary>
    /// Checks if a key exists in localStorage.
    /// </summary>
    public async Task<bool> ContainsKeyAsync(string key)
    {
        var value = await GetStringAsync(key);
        return value != null;
    }
}

/// <summary>
/// Storage keys used by the application.
/// </summary>
public static class StorageKeys
{
    public const string AppSettings = "gexvisor.settings";
    public const string LastSymbol = "gexvisor.lastSymbol";
    public const string PlaybackSpeed = "gexvisor.playbackSpeed";
    public const string AxisScales = "gexvisor.axisScales";

    // Future feature keys
    public const string NotebookEntries = "gexvisor.notebook";
    public const string PaperTrades = "gexvisor.paperTrades";
    public const string Annotations = "gexvisor.annotations";
    public const string BacktestResults = "gexvisor.backtests";
    public const string TaskBoard = "gexvisor.tasks";
}
