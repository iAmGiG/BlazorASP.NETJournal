using System.Text.Json;
using Microsoft.JSInterop;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for persisting data to browser localStorage via JS interop.
/// </summary>
public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _js;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Event raised when a localStorage operation fails.
    /// Consumers can subscribe to log or display errors.
    /// </summary>
    public event Action<string, Exception>? OnError;

    public LocalStorageService(IJSRuntime js)
    {
        _js = js;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private void LogError(string operation, string key, Exception ex)
    {
        var message = $"LocalStorage {operation} failed for key '{key}': {ex.Message}";
        Console.Error.WriteLine($"[LocalStorageService] {message}");
        OnError?.Invoke(message, ex);
    }

    /// <summary>
    /// Gets a value from localStorage and deserializes it.
    /// Returns default(T) if the key doesn't exist or an error occurs.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("GexInterop.localStorage.getItem", key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (JsonException jsonEx)
            {
                LogError("GET (deserialize)", key, jsonEx);
                return default;
            }
        }
        catch (JSException jsEx)
        {
            LogError("GET (JS interop)", key, jsEx);
            return default;
        }
        catch (Exception ex)
        {
            LogError("GET", key, ex);
            return default;
        }
    }

    /// <summary>
    /// Gets a raw string value from localStorage.
    /// Returns null if the key doesn't exist or an error occurs.
    /// </summary>
    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("GexInterop.localStorage.getItem", key);
        }
        catch (JSException jsEx)
        {
            LogError("GET STRING (JS interop)", key, jsEx);
            return null;
        }
        catch (Exception ex)
        {
            LogError("GET STRING", key, ex);
            return null;
        }
    }

    /// <summary>
    /// Serializes and stores a value in localStorage.
    /// Logs errors but does not throw exceptions.
    /// </summary>
    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _js.InvokeVoidAsync("GexInterop.localStorage.setItem", key, json);
        }
        catch (JsonException jsonEx)
        {
            LogError("SET (serialize)", key, jsonEx);
        }
        catch (JSException jsEx)
        {
            LogError("SET (JS interop)", key, jsEx);
        }
        catch (Exception ex)
        {
            LogError("SET", key, ex);
        }
    }

    /// <summary>
    /// Stores a raw string value in localStorage.
    /// Logs errors but does not throw exceptions.
    /// </summary>
    public async Task SetStringAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("GexInterop.localStorage.setItem", key, value);
        }
        catch (JSException jsEx)
        {
            LogError("SET STRING (JS interop)", key, jsEx);
        }
        catch (Exception ex)
        {
            LogError("SET STRING", key, ex);
        }
    }

    /// <summary>
    /// Removes an item from localStorage.
    /// Logs errors but does not throw exceptions.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("GexInterop.localStorage.removeItem", key);
        }
        catch (JSException jsEx)
        {
            LogError("REMOVE (JS interop)", key, jsEx);
        }
        catch (Exception ex)
        {
            LogError("REMOVE", key, ex);
        }
    }

    /// <summary>
    /// Clears all items from localStorage.
    /// Logs errors but does not throw exceptions.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("GexInterop.localStorage.clear");
        }
        catch (JSException jsEx)
        {
            LogError("CLEAR (JS interop)", "*", jsEx);
        }
        catch (Exception ex)
        {
            LogError("CLEAR", "*", ex);
        }
    }

    /// <summary>
    /// Gets all keys in localStorage.
    /// Returns empty array if an error occurs.
    /// </summary>
    public async Task<string[]> GetKeysAsync()
    {
        try
        {
            return await _js.InvokeAsync<string[]>("GexInterop.localStorage.getKeys");
        }
        catch (JSException jsEx)
        {
            LogError("GET KEYS (JS interop)", "*", jsEx);
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            LogError("GET KEYS", "*", ex);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Checks if a key exists in localStorage.
    /// Returns false if an error occurs.
    /// </summary>
    public async Task<bool> ContainsKeyAsync(string key)
    {
        var value = await GetStringAsync(key);
        return value != null;
    }
}
