# Live Data Guide

The Live Data feature enables real-time GEX (Gamma Exposure) analysis using actual market data from options chains. This guide explains how to enable, interpret, and troubleshoot live GEX data in the visualizer.

## Why Use Live Data?

| Mode | Data Source | Use Case |
|------|-------------|----------|
| **Simulation** | Gaussian distribution based on historical patterns | Learning GEX concepts, offline analysis |
| **Live Data** | Real options chain from Alpha Vantage API | Real-time market analysis, trading decisions |

Live mode shows actual dealer hedging pressure derived from current open interest and Greeks, while simulation mode demonstrates conceptual GEX dynamics.

## Features

- Real-time options chain data from Alpha Vantage API
- Automatic 30-second refresh interval
- Live GEX calculations using actual gamma and open interest
- Regime detection (Short/Long gamma)
- Zero-gamma level identification (flip point)
- Strike-by-strike gamma breakdown
- Export to CSV/JSON formats

## Prerequisites

Before enabling live data, ensure:

1. **API Backend Running**: Start the GexVisor.Api backend
   ```bash
   dotnet run --project src/GexVisor.Api
   ```

2. **API Key Configured**: Alpha Vantage API key in `appsettings.json` or environment variable
   ```json
   {
     "AlphaVantage": {
       "ApiKey": "your-key-here"
     }
   }
   ```

3. **Internet Connection**: Required for API calls

## Enabling Live Data

1. **Select Data Source**: In the sidebar, switch from "Demo" to "Real Data" mode
2. **Choose Asset Class**: Select an asset class (Index, ETF, etc.)
3. **Select Symbol**: Choose a symbol (SPY, QQQ, etc.)
4. **Toggle Live Mode**: Click the **LIVE** button in the sidebar
5. **Wait for Data**: Initial fetch takes 2-5 seconds

The sidebar displays connection status:
- `LIVE` - Connected and polling
- `SIM` - Using simulation data
- `Fetching...` - API call in progress
- `Updated HH:mm:ss` - Last successful refresh timestamp

## Understanding the Display

### Header Metrics

| Metric | Description | Live Source |
|--------|-------------|-------------|
| **Price** | Current spot price | Market quote API |
| **S** | Price squared (used in GEX formula) | Calculated from spot |
| **Total GEX** | Net gamma exposure | `CallGEX - PutGEX` |

When live data is active, a bullet indicator () appears next to the price.

### Regime Indicator

The volume warning bar shows the current gamma regime:

| Indicator | Meaning | Market Implication |
|-----------|---------|-------------------|
| **SHORT** (red pulse) | Dealers are short gamma | Volatility amplification - hedging activity adds to price moves |
| **LONG** (blue pulse) | Dealers are long gamma | Volatility dampening - hedging activity opposes price moves |
| **NEUTRAL** | Balanced exposure | Normal market behavior |

**Volatility Multiplier**: Shows expected volatility impact (e.g., 1.5x for short gamma, 0.7x for long gamma).

### Zero-Gamma Level

The zero-gamma level (or "flip point") is the strike price where net gamma crosses zero.

- **Price above zero-gamma**: Market in short gamma territory (amplified moves)
- **Price below zero-gamma**: Market in long gamma territory (dampened moves)

This level appears as a marker on the chart and in the sidebar as "Gamma Flip Point".

## GEX Chart in Live Mode

When live data is active, the GEX chart displays actual strike-by-strike gamma:

| Element | Description |
|---------|-------------|
| **Bars** | Gamma exposure at each strike price |
| **Width** | Magnitude of gamma (wider = more exposure) |
| **Left side (red)** | Put gamma (negative GEX) |
| **Right side (green)** | Call gamma (positive GEX) |
| **Zero line** | Balance point between calls and puts |

### Absolute vs Normalized View

Toggle between views using the chart controls:

- **Absolute**: GEX values in billions (e.g., $2.1B)
- **Normalized**: Percentage of maximum GEX (for comparing patterns)

## GEX Calculation Formula

Live GEX is calculated using the standard dealer hedging formula:

```
GEX = gamma  open_interest  100  spot_price
```

Where:
- `gamma` = Rate of delta change per $1 move (from options chain)
- `open_interest` = Number of contracts outstanding
- `100` = Standard options contract multiplier
- `spot_price` = Current stock price squared

**Net GEX** = Total Call GEX - Total Put GEX

## Regime Classification

| Condition | Classification | Threshold |
|-----------|---------------|-----------|
| Net GEX > +10% of total | Long Gamma | Dealers short gamma |
| Net GEX < -10% of total | Short Gamma | Dealers long gamma |
| Within 10% | Neutral | Balanced |

## API Endpoints

The live data feature uses these backend endpoints:

| Endpoint | Description |
|----------|-------------|
| `GET /api/gex/{symbol}` | Calculate GEX with current spot price |
| `GET /api/gex/{symbol}/at/{price}` | Calculate GEX at specific price |
| `GET /api/options/chain/{symbol}` | Raw options chain data |
| `GET /api/market/quote/{symbol}` | Current spot price |

## Export Options

Export live strike gamma data for further analysis:

1. Click the **Export** menu in the header
2. Select format:
   - **CSV**: Spreadsheet-compatible, one row per strike
   - **JSON**: Full `GexCalculationResult` object

Exported data includes:
- Strike price
- Call gamma, Put gamma, Net gamma
- Open interest by side
- Calculated GEX values

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| **Warning in sidebar** | API call failed | Check API configuration, verify network connection |
| **No data loading** | Symbol not found | Verify symbol exists and has options |
| **Stale timestamp** | Polling stopped | Re-toggle live mode button |
| **"Rate limited" error** | Too many API calls | Wait 1 minute (Alpha Vantage: 5 calls/min) |
| **Empty chart** | No options data | Symbol may not have options, or market closed |
| **Backend not reachable** | API not running | Start `dotnet run --project src/GexVisor.Api` |

### API Rate Limits

| Provider | Rate Limit | Notes |
|----------|------------|-------|
| Alpha Vantage | 5 calls/min | Premium key removes limit |
| Alpaca | 200 calls/min | Requires account |
| Finnhub | 60 calls/min | Free tier |

## Related Documentation

- [Architecture](architecture.md) - Live Data Services technical details
- [Getting Started](getting-started.md) - Initial setup and configuration
- [GEX Calculation Pipeline](architecture.md#gex-calculation-pipeline) - Formula implementation details

## Related Issues

- [#145 Epic: Live Market Data](https://github.com/iAmGiG/GexVisor/issues/145) - Parent epic
- [#149 GEX Calculation Engine](https://github.com/iAmGiG/GexVisor/issues/149) - Core calculation service
- [#147 Market Data Service](https://github.com/iAmGiG/GexVisor/issues/147) - Multi-provider quote fetching
