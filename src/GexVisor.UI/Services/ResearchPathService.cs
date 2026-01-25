using System.Text.Json;
using System.Text.Json.Serialization;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for loading research path data from JSON file.
/// Handles caching and enum deserialization.
/// </summary>
public class ResearchPathService : IResearchPathService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    private ResearchPath[]? _paths;

    public ResearchPath[] Paths => _paths ?? [];

    public ResearchPathService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Load all research paths from JSON file.
    /// Caches result after first load.
    /// </summary>
    public async Task<ResearchPath[]> LoadPathsAsync()
    {
        if (_paths != null)
        {
            return _paths;
        }

        try
        {
            var response = await _httpClient.GetAsync("data/research-paths.json");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Could not load research paths: HTTP error");
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ResearchPathsWrapper>(json, _jsonOptions);

            _paths = wrapper?.Paths ?? [];
            return _paths;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load research paths: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Get a specific research path by ID.
    /// </summary>
    public async Task<ResearchPath?> GetPathByIdAsync(string id)
    {
        var paths = await LoadPathsAsync();
        return paths.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Wrapper class for JSON deserialization.
    /// </summary>
    private record ResearchPathsWrapper
    {
        public ResearchPath[] Paths { get; init; } = [];
    }
}
