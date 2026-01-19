namespace GexVisor.UI.Services;

/// <summary>
/// Interface for browser localStorage operations via JS interop.
/// Enables dependency injection and unit testing.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Gets a value from localStorage and deserializes it.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets a value in localStorage after serializing it.
    /// </summary>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// Removes a value from localStorage.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Clears all values from localStorage.
    /// </summary>
    Task ClearAsync();
}
