# API Backend Setup

The GexVisor API backend provides live market data, options chains, and real-time GEX calculations. This guide covers installation, configuration, and troubleshooting.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (version 10.0.102 or later)
- API keys from one or more market data providers (see [Supported Providers](#supported-providers))
- Internet connection for API calls

## Quick Start

```bash
# From repository root
cd src/GexVisor.Api

# Run the API (defaults to http://localhost:5000)
dotnet run
```

The API will start and log configured providers:

```
info: GexVisor.Api.Services.ApiConfigService[0]
      API Configuration loaded. Configured providers: AlphaVantage, Finnhub
```

## Configuration

The API supports three configuration methods (in priority order):

1. **Environment Variables** (highest priority, production)
2. **appsettings.json** (recommended for development)
3. **config/config.json** (legacy autogen-trader format)

### Method 1: appsettings.json (Recommended)

Create or edit `src/GexVisor.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiKeys": {
    "AlphaVantage": "your-alpha-vantage-key-here",
    "Finnhub": "your-finnhub-key-here",
    "Polygon": "your-polygon-key-here",
    "AlpacaEndpoint": "https://paper-api.alpaca.markets",
    "AlpacaApiKey": "your-alpaca-key-here",
    "AlpacaSecret": "your-alpaca-secret-here",
    "Fred": "your-fred-key-here",
    "Fmp": "your-fmp-key-here",
    "NewsApi": "your-newsapi-key-here"
  }
}
```

**Note:** Add `appsettings.json` to `.gitignore` if storing real API keys.

### Method 2: Environment Variables (Production)

Set environment variables before running the API:

**Windows (PowerShell):**

```powershell
$env:ALPHA_VANTAGE_KEY="your-key-here"
$env:FINNHUB_KEY="your-key-here"
dotnet run --project src/GexVisor.Api
```

**Linux/macOS:**

```bash
export ALPHA_VANTAGE_KEY="your-key-here"
export FINNHUB_KEY="your-key-here"
dotnet run --project src/GexVisor.Api
```

### Method 3: config/config.json (Legacy)

Create `config/config.json` in repository root:

```json
{
  "ALPHA_VANTAGE_KEY": "your-key-here",
  "FINNHUB_KEY": "your-key-here",
  "POLYGON_IO": "your-polygon-key-here",
  "ALPACA_ENDPOINT": "https://paper-api.alpaca.markets",
  "ALPACA_PAPER_API_KEY": "your-alpaca-key-here",
  "ALPACA_PAPER_SECRET": "your-alpaca-secret-here",
  "FREDAPI": "your-fred-key-here",
  "FMP": "your-fmp-key-here",
  "NEWSAPI_KEY": "your-newsapi-key-here"
}
```

## Supported Providers

| Provider | Free Tier | Rate Limit | Use Case |
|----------|-----------|------------|----------|
| **Alpha Vantage** | Yes (5 calls/min) | 5/min (free), 75/min (premium) | Options chains, quotes |
| **Finnhub** | Yes (60 calls/min) | 60/min | Real-time quotes, candles |
| **Polygon.io** | Paid only | 5 calls/sec | Professional-grade data |
| **Alpaca** | Yes (200 calls/min) | 200/min | Paper trading, live quotes |
| **FRED** | Yes | 120 calls/min | Economic indicators |
| **FMP** | Paid only | Varies | Fundamentals, financials |
| **NewsAPI** | Yes (100 requests/day) | 100/day | Market news |

**Minimum Requirement:** At least one of Alpha Vantage, Finnhub, or Polygon for GEX calculations.

### Getting API Keys

- **Alpha Vantage**: [Free key](https://www.alphavantage.co/support/#api-key) (recommended for getting started)
- **Finnhub**: [Free key](https://finnhub.io/register)
- **Polygon.io**: [Paid plans](https://polygon.io/pricing)
- **Alpaca**: [Free paper trading account](https://alpaca.markets/docs/get-started/)
- **FRED**: [Free API key](https://fred.stlouisfed.org/docs/api/api_key.html)

## Verifying Configuration

Check API configuration status:

```bash
curl http://localhost:5000/api/config/status
```

**Response:**

```json
{
  "isConfigured": true,
  "providers": {
    "alphaVantage": true,
    "finnhub": true,
    "alpaca": false,
    "polygon": false,
    "fred": false
  },
  "errors": []
}
```

**Configuration Errors:** If `isConfigured` is `false`, check the `errors` array for missing keys.

## API Endpoints

### Market Data

| Endpoint | Description | Example |
|----------|-------------|---------|
| `GET /api/market/quote/{symbol}` | Current spot price | `/api/market/quote/SPY` |
| `GET /api/market/quotes?symbols=...` | Multi-symbol quotes | `/api/market/quotes?symbols=SPY,QQQ,IWM` |
| `GET /api/market/bars/{symbol}?timeframe=1d&limit=100` | OHLCV candles | `/api/market/bars/SPY?timeframe=1h&limit=50` |

### Options Chain

| Endpoint | Description | Example |
|----------|-------------|---------|
| `GET /api/options/chain/{symbol}` | Full options chain | `/api/options/chain/SPY` |
| `GET /api/options/chain/{symbol}?expiration=2026-02-20` | Specific expiration | `/api/options/chain/SPY?expiration=2026-02-20` |
| `GET /api/options/expirations/{symbol}` | Available expirations | `/api/options/expirations/SPY` |
| `POST /api/options/cache/invalidate/{symbol}` | Clear cache | `/api/options/cache/invalidate/SPY` |

### GEX Calculation

| Endpoint | Description | Example |
|----------|-------------|---------|
| `GET /api/gex/{symbol}` | GEX with current spot | `/api/gex/SPY` |
| `GET /api/gex/{symbol}/at/{price}` | GEX at specific price | `/api/gex/SPY/at/580.50` |

**Response Format:**

```json
{
  "symbol": "SPY",
  "spotPrice": 580.50,
  "totalGex": -1234567890,
  "callGex": 800000000,
  "putGex": 2034567890,
  "regime": "Short",
  "zeroGammaLevel": 585.20,
  "strikes": [
    {
      "strike": 580.0,
      "callGamma": 0.15,
      "putGamma": 0.08,
      "netGamma": 0.07,
      "callOi": 5000,
      "putOi": 3200,
      "gex": 123456789
    }
  ]
}
```

### Gamma Wall Analysis

| Endpoint | Description | Example |
|----------|-------------|---------|
| `GET /api/gex/{symbol}/walls` | Analyze gamma walls at current spot price | `/api/gex/SPY/walls` |
| `GET /api/gex/{symbol}/walls/at/{price}` | Analyze gamma walls at specific price | `/api/gex/SPY/walls/at/580.50` |

**Response Format:**

```json
{
  "supportLevels": [
    {
      "strikePrice": 575.0,
      "netGex": 1234567890,
      "gexConcentrationPercent": 12.5,
      "wallType": "Support",
      "distanceFromSpot": -5.5,
      "distancePercent": -0.95,
      "magnetismScore": 0.85
    }
  ],
  "resistanceLevels": [
    {
      "strikePrice": 590.0,
      "netGex": -987654321,
      "gexConcentrationPercent": 10.2,
      "wallType": "Resistance",
      "distanceFromSpot": 9.5,
      "distancePercent": 1.64,
      "magnetismScore": 0.72
    }
  ],
  "flipPoints": [
    {
      "strikePrice": 582.35,
      "netGex": 0,
      "wallType": "FlipPoint",
      "distanceFromSpot": 1.85,
      "distancePercent": 0.32
    }
  ],
  "maxPositiveGammaStrike": { "strikePrice": 575.0, "netGex": 1234567890 },
  "maxNegativeGammaStrike": { "strikePrice": 590.0, "netGex": -987654321 },
  "gexAboveSpot": -500000000,
  "gexBelowSpot": 800000000,
  "gexAsymmetry": -0.23,
  "timestamp": "2026-01-28T10:30:00Z"
}
```

**Wall Types:**

- `Support`: Positive gamma below spot (dealers buy dips)
- `Resistance`: Negative gamma above spot (dealers sell rallies)
- `FlipPoint`: Zero-crossing level where net GEX changes sign

**Response Fields:**

- `supportLevels`: Top 5 positive gamma strikes below spot price
- `resistanceLevels`: Top 5 negative gamma strikes above spot price
- `flipPoints`: Interpolated levels where net GEX crosses zero
- `gexAsymmetry`: Directional bias from -1 (bearish) to +1 (bullish)
- `magnetismScore`: Price attraction strength (0-1), higher = stronger pull

### Historical Data Backfill

| Endpoint | Description | Example |
|----------|-------------|---------|
| `POST /api/backfill/start` | Start a historical GEX backfill job | `/api/backfill/start` |
| `GET /api/backfill/status` | Get all active and recent backfill jobs | `/api/backfill/status` |
| `GET /api/backfill/status/{jobId}` | Get status of a specific backfill job | `/api/backfill/status/backfill-20260127-101500-a1b2c3d4` |
| `POST /api/backfill/stop/{jobId}` | Stop a running backfill job | `/api/backfill/stop/backfill-20260127-101500-a1b2c3d4` |
| `POST /api/backfill/resume/{jobId}` | Resume a stopped or failed job | `/api/backfill/resume/backfill-20260127-101500-a1b2c3d4` |

**Request Format (POST /api/backfill/start):**

```json
{
  "symbols": ["SPY", "QQQ", "IWM"],
  "startDate": "2025-01-01",
  "endDate": "2026-01-27"
}
```

**Response Format:**

```json
{
  "jobId": "backfill-20260127-101500-a1b2c3d4",
  "state": "Running",
  "symbols": ["SPY", "QQQ", "IWM"],
  "startDate": "2025-01-01",
  "endDate": "2026-01-27",
  "totalDays": 252,
  "completedDays": 45,
  "failedDays": 0,
  "progressPercent": 17.86,
  "startedAt": "2026-01-27T10:15:00Z",
  "completedAt": null,
  "lastActivityAt": "2026-01-27T10:30:45Z",
  "apiCallsMade": 450,
  "apiCallsRemaining": 320,
  "currentSymbol": "SPY",
  "currentDate": "2025-02-01",
  "lastError": null,
  "gexRecordsStored": 756
}
```

**Job States:**

- `Pending` - Job is queued but not yet started
- `Running` - Job is actively processing
- `RateLimited` - Job is paused due to rate limiting
- `Stopped` - Job was manually stopped by user
- `Completed` - Job finished successfully
- `Failed` - Job failed with errors

**Features:**

- **Rate Limiting**: 70 calls/min for Alpha Vantage API compliance
- **Progress Tracking**: Real-time updates on completed/failed days
- **Resume Capability**: Jobs can be resumed after stopping or failures
- **Weekend Skipping**: Automatically skips non-trading days
- **Concurrent Jobs**: Multiple backfill jobs can run simultaneously
- **Cache Storage**: Historical GEX data cached with 10-year TTL

**Cache Keys:** `gex:historical:{symbol}:{yyyy-MM-dd}`

**Job ID Format:** `backfill-{yyyyMMdd-HHmmss}-{guid8}`

### Cache Management

| Endpoint | Description |
|----------|-------------|
| `GET /api/cache/stats` | Cache statistics (hit rate, size) |
| `POST /api/cache/cleanup` | Remove expired entries |
| `POST /api/cache/invalidate?pattern=SPY*` | Invalidate by pattern |

### GitHub Integration

See [GitHub Integration Guide](github-integration.md) for OAuth and GraphQL proxy endpoints.

## CORS Configuration

The API allows requests from these localhost ports (configured in [Program.cs:34-42](../src/GexVisor.Api/Program.cs#L34-L42)):

- `http://localhost:5000` - Default UI dev server
- `http://localhost:5001` - HTTPS UI dev server
- `http://localhost:5246` - Additional dev port
- `https://localhost:7161` - HTTPS API port
- `http://localhost:10354` - Test runner port

**Adding Ports:** Edit `Program.cs` and add to the `WithOrigins()` array if using custom ports.

## Running in Development

### Hot Reload

```bash
dotnet watch run --project src/GexVisor.Api
```

Changes to `.cs` files trigger automatic rebuild and restart.

### Custom Port

```bash
dotnet run --project src/GexVisor.Api --urls "http://localhost:8080"
```

### Logging Levels

Edit `appsettings.json` to adjust verbosity:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "GexVisor.Api.Services": "Debug"
    }
  }
}
```

## Caching

The API uses SQLite for caching market data and options chains:

- **Location**: `.cache/gexvisor.db` (created automatically)
- **Default TTL**: 60 seconds for quotes, 5 minutes for options chains
- **Benefits**: Reduces API calls, improves performance under repeated requests

**Cache Stats:**

```bash
curl http://localhost:5000/api/cache/stats
```

**Response:**

```json
{
  "totalEntries": 42,
  "totalSizeBytes": 1048576,
  "hitRate": 0.85,
  "oldestEntry": "2026-01-24T10:30:00Z"
}
```

## Troubleshooting

### API Key Errors

**Symptom:** `isConfigured: false` or `errors: ["AlphaVantage API key not configured"]`

**Solution:**

1. Verify API key is valid (test at provider's website)
2. Check spelling in `appsettings.json` (case-sensitive section names)
3. Ensure no trailing spaces in key values
4. Try environment variable override: `$env:ALPHA_VANTAGE_KEY="key"`

### CORS Errors

**Symptom:** Browser console shows `CORS policy: No 'Access-Control-Allow-Origin' header`

**Solution:**

1. Verify API is running (`curl http://localhost:5000/api/config/status`)
2. Check UI is running on an allowed port (see [CORS Configuration](#cors-configuration))
3. Add your port to `Program.cs` → `WithOrigins()` array
4. Restart API after `Program.cs` changes

### Rate Limiting

**Symptom:** `429 Too Many Requests` or `Rate limit exceeded`

**Solution:**

1. **Alpha Vantage (5/min)**: Implement client-side delays or upgrade to premium
2. **Cache hit improvement**: Use the same symbol/expiration repeatedly to leverage caching
3. **Multiple providers**: Configure fallback providers (Finnhub, Polygon)

### Empty Options Chain

**Symptom:** `/api/options/chain/XYZ` returns empty `strikes` array

**Possible Causes:**

- Symbol has no options (check on provider's website)
- Market is closed (options data may be delayed)
- Symbol not recognized (verify spelling, use uppercase)
- Provider doesn't support options for this asset class

**Debug:**

```bash
# Check if symbol has ANY data
curl http://localhost:5000/api/market/quote/XYZ

# Check available expirations
curl http://localhost:5000/api/options/expirations/XYZ
```

### Build Errors

**Symptom:** `dotnet run` fails with compilation errors

**Solution:**

```bash
# Restore packages
dotnet restore

# Clean build
dotnet clean
dotnet build
```

## Production Deployment

### Environment Variables Only

**Never commit API keys to appsettings.json.** Use environment variables in production:

```bash
# Azure App Service (example)
az webapp config appsettings set \
  --name gexvisor-api \
  --resource-group myResourceGroup \
  --settings ALPHA_VANTAGE_KEY="key" FINNHUB_KEY="key"
```

### HTTPS Configuration

For production, use HTTPS and update CORS origins:

```csharp
// Program.cs
policy.WithOrigins(
    "https://gexvisor.com",
    "https://www.gexvisor.com")
```

### Cache Persistence

Default SQLite cache is ephemeral (`.cache/` folder). For persistent caching:

1. Mount `.cache/` to a volume (Docker/Kubernetes)
2. Or configure external cache (Redis, Memcached)

## Testing

Run API unit tests:

```bash
dotnet test tests/GexVisor.Api.Tests
```

**Test Coverage:** 74 API tests covering GEX calculation, caching, and service layer.

## Related Documentation

- [Live Data Guide](live-data.md) - Using live data in the UI
- [Architecture](architecture.md) - Live Data Services technical details
- [GEX Calculation Pipeline](architecture.md#gex-calculation-pipeline) - Formula implementation

---

_Last Updated: 2026-01-24_
