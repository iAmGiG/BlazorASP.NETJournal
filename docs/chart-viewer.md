# Chart Viewer User Guide

The Chart Viewer provides visual price charting with candlestick displays and trade overlay support. This guide explains how to use the chart features, export data, and share chart configurations.

## Features

| Feature | Description |
|---------|-------------|
| **Candlestick Charts** | OHLCV price visualization using ApexCharts |
| **Trade Markers** | Entry/exit annotations with P&L display |
| **Multi-Timeframe** | Daily, hourly, 15-minute, 5-minute views |
| **Interactive Tools** | Zoom, pan, crosshair, selection tools |
| **Export** | PNG, SVG (via toolbar), CSV (via button) |
| **URL Sharing** | Shareable links with chart state |

## Chart Test Page

Navigate to `/chart-test` to access the standalone chart testing page.

### Controls

| Control | Options | Description |
|---------|---------|-------------|
| **Symbol** | Any ticker (SPY, QQQ, AAPL...) | Stock symbol to chart |
| **Timeframe** | 1d, 1h, 15m, 5m | Bar period |
| **Bars** | 30, 60, 100, 200 | Number of data points |
| **Data Source** | Live API, Sample Data | Real or simulated data |

### Toolbar Actions

| Button | Icon | Action |
|--------|------|--------|
| **Load Data** | Refresh | Fetch/refresh chart data |
| **Export CSV** | Download | Download bar data as CSV |
| **Share** | Share | Copy shareable URL to clipboard |

### ApexCharts Toolbar

The chart includes ApexCharts' built-in toolbar (top-right):

| Icon | Function |
|------|----------|
| Zoom In | Magnify selected area |
| Zoom Out | Reduce magnification |
| Pan | Drag to scroll chart |
| Selection | Select data range |
| Home | Reset to original view |
| Download | Export as PNG/SVG |

## Trade Journal Charts

From the Trade Journal, open any trade's detail modal to see an embedded chart showing price action around the trade period.

### Accessing Trade Charts

1. Navigate to **Trade Journal** in the sidebar
2. Click on any trade row to open the detail modal
3. The **Price Action** section shows a candlestick chart

### Chart Features in Trade Modal

| Feature | Description |
|---------|-------------|
| **Entry Marker** | Green (long) or red (short) dot at entry |
| **Exit Marker** | Colored by P&L (green = profit, red = loss) |
| **Entry Price Line** | Dashed horizontal line at entry level |
| **Exit Price Line** | Dashed horizontal line at exit level |
| **Timeframe Selector** | Switch between daily/hourly/15-min |
| **Collapse Toggle** | Hide/show chart to save space |

### Trade Marker Colors

| Marker | Long Trade | Short Trade |
|--------|------------|-------------|
| Entry | Green (#00c853) | Red (#ff5252) |
| Exit (profit) | Green (#00c853) | Green (#00c853) |
| Exit (loss) | Red (#ff5252) | Red (#ff5252) |

## Export Options

### CSV Export

Click the download button to export bar data as CSV:

```csv
Date,Symbol,Open,High,Low,Close,Volume
2026-01-20 09:30,SPY,475.50,476.25,474.80,475.90,15000000
2026-01-21 09:30,SPY,475.90,477.10,475.20,476.80,12500000
```

**Filename format**: `{SYMBOL}_{timeframe}_{timestamp}.csv`

### Image Export

Use the ApexCharts toolbar to export:

- **PNG**: Bitmap image for presentations
- **SVG**: Vector format for high-quality printing

### URL Sharing

Click the share button to copy a URL with chart parameters:

```
https://localhost:5001/chart-test?symbol=SPY&tf=1d
```

Share this URL to show others the same chart configuration.

**URL Parameters**:

| Parameter | Example | Description |
|-----------|---------|-------------|
| `symbol` | SPY | Stock ticker |
| `tf` | 1d | Timeframe (1d, 1h, 15m, 5m) |

## Data Sources

### Live API

When "Live API" is selected, data is fetched from the configured market data provider.

**Supported Providers** (configured in backend):

- Alpha Vantage
- Alpaca Markets
- Finnhub

**Note**: Requires API backend running and valid API key configured.

### Sample Data

Generates realistic OHLCV data using random walk simulation:

- Uses symbol-specific base prices (SPY: $475, QQQ: $400, etc.)
- Simulates 2% daily volatility
- Skips weekends
- Useful for testing when API unavailable

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| **No data displayed** | API not running or rate limited | Check backend, wait, or use Sample Data |
| **Stale data** | Cache not refreshed | Click Load Data to refresh |
| **Chart doesn't resize** | Browser rendering issue | Refresh page |
| **Export fails** | Browser popup blocker | Allow popups for site |
| **Share URL doesn't work** | Missing parameters | Ensure symbol/timeframe are set |

### API Rate Limits

Same limits apply as Live Data feature:

| Provider | Rate Limit |
|----------|------------|
| Alpha Vantage | 5 calls/min |
| Alpaca | 200 calls/min |
| Finnhub | 60 calls/min |

## Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `CandlestickChart` | Components/Charts/ | Base ApexCharts wrapper |
| `TradeChart` | Components/Charts/ | Candlestick + trade markers |
| `TradeMarker` | Components/Charts/ | Trade annotation builder |
| `ChartExport` | Components/Charts/ | CSV/URL export utilities |

## Related Documentation

- [Architecture](architecture.md) - PriceDataService and chart pipeline
- [Live Data](live-data.md) - Market data providers
- [ADR-0008](adr/0008-chart-library-selection.md) - Chart library selection rationale

## Related Issues

- [#111 Epic: Simple Chart Viewer](https://github.com/iAmGiG/GexVisor/issues/111) - Parent epic
- [#129 Chart Library Selection](https://github.com/iAmGiG/GexVisor/issues/129) - Library evaluation
- [#130 Price Data Service](https://github.com/iAmGiG/GexVisor/issues/130) - Data fetching service
- [#131 Trade Chart Component](https://github.com/iAmGiG/GexVisor/issues/131) - Chart with trade markers
- [#132 Trade Journal Integration](https://github.com/iAmGiG/GexVisor/issues/132) - Modal integration
- [#133 Export and Sharing](https://github.com/iAmGiG/GexVisor/issues/133) - Export features

---

_Last Updated: 2026-01-24_
