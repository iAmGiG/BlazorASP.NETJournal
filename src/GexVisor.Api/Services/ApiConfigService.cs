using System.Text.Json;
using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for loading and managing API configuration.
/// Loads from config/config.json with environment variable overrides.
/// </summary>
public interface IApiConfigService
{
    ApiConfiguration Configuration { get; }
    bool IsConfigured { get; }
    List<string> ValidationErrors { get; }
}

public class ApiConfigService : IApiConfigService
{
    private readonly ILogger<ApiConfigService> _logger;
    private readonly ApiConfiguration _config;
    private readonly List<string> _validationErrors;

    public ApiConfiguration Configuration => _config;
    public bool IsConfigured => _validationErrors.Count == 0;
    public List<string> ValidationErrors => _validationErrors;

    public ApiConfigService(IConfiguration configuration, ILogger<ApiConfigService> logger)
    {
        _logger = logger;
        _config = new ApiConfiguration();

        // 1. Try to load from config/config.json (project root)
        LoadFromConfigJson();

        // 2. Override with appsettings.json values
        LoadFromAppSettings(configuration);

        // 3. Override with environment variables
        LoadFromEnvironment();

        // 4. Validate
        _validationErrors = _config.Validate();

        if (_validationErrors.Count > 0)
        {
            foreach (var error in _validationErrors)
            {
                _logger.LogWarning("API Configuration: {Error}", error);
            }
        }
        else
        {
            LogConfiguredProviders();
        }
    }

    private void LoadFromConfigJson()
    {
        // Look for config.json in project root (2 levels up from bin)
        var paths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "config", "config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "config", "config.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config", "config.json")
        };

        foreach (var configPath in paths)
        {
            var fullPath = Path.GetFullPath(configPath);
            if (File.Exists(fullPath))
            {
                try
                {
                    var json = File.ReadAllText(fullPath);
                    var configDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (configDict != null)
                    {
                        MapConfigDictionary(configDict);
                        _logger.LogInformation("Loaded API configuration from {Path}", fullPath);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load config from {Path}", fullPath);
                }
            }
        }

        _logger.LogDebug("No config/config.json found, using appsettings and environment variables");
    }

    private void MapConfigDictionary(Dictionary<string, string> dict)
    {
        // Map from autogen-trader config.json format
        if (dict.TryGetValue("ALPHA_VANTAGE_KEY", out var av))
            _config.AlphaVantageKey = av;

        if (dict.TryGetValue("FINNHUB_KEY", out var fh))
            _config.FinnhubKey = fh;

        if (dict.TryGetValue("POLYGON_IO", out var pg))
            _config.PolygonKey = pg;

        if (dict.TryGetValue("ALPACA_ENDPOINT", out var ae))
            _config.AlpacaEndpoint = ae;

        if (dict.TryGetValue("ALPACA_PAPER_API_KEY", out var ak))
            _config.AlpacaApiKey = ak;

        if (dict.TryGetValue("ALPACA_PAPER_SECRET", out var asecret))
            _config.AlpacaSecret = asecret;

        if (dict.TryGetValue("FREDAPI", out var fred))
            _config.FredApiKey = fred;

        if (dict.TryGetValue("FMP", out var fmp))
            _config.FmpKey = fmp;

        if (dict.TryGetValue("NEWSAPI_KEY", out var news))
            _config.NewsApiKey = news;
    }

    private void LoadFromAppSettings(IConfiguration configuration)
    {
        // Load from ApiKeys section in appsettings.json
        var section = configuration.GetSection("ApiKeys");

        _config.AlphaVantageKey = section["AlphaVantage"] ?? _config.AlphaVantageKey;
        _config.FinnhubKey = section["Finnhub"] ?? _config.FinnhubKey;
        _config.PolygonKey = section["Polygon"] ?? _config.PolygonKey;
        _config.AlpacaEndpoint = section["AlpacaEndpoint"] ?? _config.AlpacaEndpoint;
        _config.AlpacaApiKey = section["AlpacaApiKey"] ?? _config.AlpacaApiKey;
        _config.AlpacaSecret = section["AlpacaSecret"] ?? _config.AlpacaSecret;
        _config.FredApiKey = section["Fred"] ?? _config.FredApiKey;
        _config.FmpKey = section["Fmp"] ?? _config.FmpKey;
        _config.NewsApiKey = section["NewsApi"] ?? _config.NewsApiKey;
    }

    private void LoadFromEnvironment()
    {
        // Environment variables take highest priority (production deployments)
        _config.AlphaVantageKey = Environment.GetEnvironmentVariable("ALPHA_VANTAGE_KEY") ?? _config.AlphaVantageKey;
        _config.FinnhubKey = Environment.GetEnvironmentVariable("FINNHUB_KEY") ?? _config.FinnhubKey;
        _config.PolygonKey = Environment.GetEnvironmentVariable("POLYGON_IO") ?? _config.PolygonKey;
        _config.AlpacaEndpoint = Environment.GetEnvironmentVariable("ALPACA_ENDPOINT") ?? _config.AlpacaEndpoint;
        _config.AlpacaApiKey = Environment.GetEnvironmentVariable("ALPACA_PAPER_API_KEY") ?? _config.AlpacaApiKey;
        _config.AlpacaSecret = Environment.GetEnvironmentVariable("ALPACA_PAPER_SECRET") ?? _config.AlpacaSecret;
        _config.FredApiKey = Environment.GetEnvironmentVariable("FREDAPI") ?? _config.FredApiKey;
        _config.FmpKey = Environment.GetEnvironmentVariable("FMP") ?? _config.FmpKey;
        _config.NewsApiKey = Environment.GetEnvironmentVariable("NEWSAPI_KEY") ?? _config.NewsApiKey;
    }

    private void LogConfiguredProviders()
    {
        var providers = new List<string>();

        if (_config.HasAlphaVantage) providers.Add("AlphaVantage");
        if (_config.HasFinnhub) providers.Add("Finnhub");
        if (_config.HasAlpaca) providers.Add("Alpaca");
        if (_config.HasPolygon) providers.Add("Polygon");
        if (_config.HasFred) providers.Add("FRED");

        _logger.LogInformation("API Configuration loaded. Configured providers: {Providers}",
            string.Join(", ", providers));
    }
}
