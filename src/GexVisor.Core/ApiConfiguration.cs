namespace GexVisor.Core;

/// <summary>
/// Configuration for external market data API providers.
/// Supports loading from JSON config files with environment variable overrides.
/// </summary>
public class ApiConfiguration
{
    // Market Data Providers
    public string? AlphaVantageKey { get; set; }
    public string? FinnhubKey { get; set; }
    public string? PolygonKey { get; set; }

    // Alpaca Trading API
    public string? AlpacaEndpoint { get; set; }
    public string? AlpacaApiKey { get; set; }
    public string? AlpacaSecret { get; set; }

    // Economic Data
    public string? FredApiKey { get; set; }

    // Additional Providers
    public string? FmpKey { get; set; }
    public string? NewsApiKey { get; set; }

    /// <summary>
    /// Rate limits per provider (calls per minute).
    /// </summary>
    public static class RateLimits
    {
        public const int AlphaVantage = 75;   // Free tier: 75 calls/min
        public const int Finnhub = 60;        // Free tier: 60 calls/min
        public const int Alpaca = 200;        // Paper: 200 calls/min
        public const int Polygon = 5;         // Free tier: 5 calls/min
    }

    /// <summary>
    /// Validates that required API keys are configured.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // At minimum, we need one market data provider
        if (string.IsNullOrWhiteSpace(AlphaVantageKey) &&
            string.IsNullOrWhiteSpace(FinnhubKey) &&
            string.IsNullOrWhiteSpace(AlpacaApiKey))
        {
            errors.Add("At least one market data provider API key is required (AlphaVantage, Finnhub, or Alpaca)");
        }

        // Alpaca requires both key and secret
        if (!string.IsNullOrWhiteSpace(AlpacaApiKey) && string.IsNullOrWhiteSpace(AlpacaSecret))
        {
            errors.Add("Alpaca API key provided but secret is missing");
        }

        if (!string.IsNullOrWhiteSpace(AlpacaSecret) && string.IsNullOrWhiteSpace(AlpacaApiKey))
        {
            errors.Add("Alpaca secret provided but API key is missing");
        }

        return errors;
    }

    /// <summary>
    /// Checks if a specific provider is configured.
    /// </summary>
    public bool HasAlphaVantage => !string.IsNullOrWhiteSpace(AlphaVantageKey);
    public bool HasFinnhub => !string.IsNullOrWhiteSpace(FinnhubKey);
    public bool HasAlpaca => !string.IsNullOrWhiteSpace(AlpacaApiKey) && !string.IsNullOrWhiteSpace(AlpacaSecret);
    public bool HasPolygon => !string.IsNullOrWhiteSpace(PolygonKey);
    public bool HasFred => !string.IsNullOrWhiteSpace(FredApiKey);
}
