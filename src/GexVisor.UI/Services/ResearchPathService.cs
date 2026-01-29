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
            Console.WriteLine("📡 ResearchPathService: Attempting to load data/research-paths.json");
            Console.WriteLine($"   HttpClient BaseAddress: {_httpClient.BaseAddress}");

            var response = await _httpClient.GetAsync("data/research-paths.json");

            Console.WriteLine($"   HTTP Response: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ ResearchPathService: HTTP {response.StatusCode} for data/research-paths.json");
                Console.WriteLine($"   Full URL attempted: {response.RequestMessage?.RequestUri}");
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✓ ResearchPathService: Received {json.Length} bytes");
            Console.WriteLine($"   First 100 chars: {json.Substring(0, Math.Min(100, json.Length))}");

            var wrapper = JsonSerializer.Deserialize<ResearchPathsWrapper>(json, _jsonOptions);

            if (wrapper == null)
            {
                Console.WriteLine("❌ ResearchPathService: Deserialization returned null wrapper");
                return [];
            }

            if (wrapper.Paths == null || wrapper.Paths.Length == 0)
            {
                Console.WriteLine("⚠️ ResearchPathService: Paths array is null or empty");
                Console.WriteLine($"   Wrapper type: {wrapper.GetType().FullName}");
            }
            else
            {
                Console.WriteLine($"✓ ResearchPathService: Successfully loaded {wrapper.Paths.Length} research paths");
                Console.WriteLine($"   First path ID: {wrapper.Paths[0].Id}");
            }

            _paths = wrapper.Paths ?? [];
            return _paths;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ResearchPathService EXCEPTION: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"   Stack trace:");
            Console.WriteLine(ex.StackTrace);
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
