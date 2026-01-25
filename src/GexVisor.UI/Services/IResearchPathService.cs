using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Interface for loading research path data from JSON.
/// Enables dependency injection and unit testing.
/// </summary>
public interface IResearchPathService
{
    /// <summary>
    /// Gets the loaded research paths.
    /// Returns empty array if not yet loaded.
    /// </summary>
    ResearchPath[] Paths { get; }

    /// <summary>
    /// Load all research paths from JSON file.
    /// Caches result after first load.
    /// </summary>
    Task<ResearchPath[]> LoadPathsAsync();

    /// <summary>
    /// Get a specific research path by ID.
    /// </summary>
    Task<ResearchPath?> GetPathByIdAsync(string id);
}
