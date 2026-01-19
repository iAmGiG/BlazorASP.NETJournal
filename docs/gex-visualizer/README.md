# GEX Data Export Tool

This directory contains the data export script for converting GEX research data from SQLite to JSON format.

> **Note**: The original JavaScript visualizer has been removed. The application is now fully
> implemented in Blazor WebAssembly - run `dotnet run --project src/GexVisor.UI` and navigate to `/gex`.

## Data Export Script

The `export_data.py` script exports historical GEX data from the research database to JSON files
for use with the Blazor application.

### Prerequisites

- Python 3.8+
- Access to `.cache/gex_research.db` (premium API data - not included in repo)

### Usage

```bash
# Export all symbols to Blazor wwwroot/data
python export_data.py --blazor

# Export specific symbols
python export_data.py --blazor SPY QQQ IWM

# Export to legacy data/ directory
python export_data.py
python export_data.py SPY QQQ

# Export to custom directory
python export_data.py --output /path/to/dir
```

### Output Format

Each symbol generates a JSON file with this structure:

```json
{
  "symbol": "SPY",
  "asset_class": "Index",
  "date_range": { "start": "2020-01-02", "end": "2025-12-31" },
  "count": 1258,
  "timeline": [
    {
      "date": "2020-01-02",
      "price": 321.75,
      "gex": 1234.56,
      "call_gex": 800.00,
      "put_gex": 434.56,
      "zero_gamma": 318.50,
      "max_gamma": 325.00,
      "regime": "POSITIVE_GAMMA",
      "call_oi": 0.35,
      "put_oi": 0.28,
      "contracts": 150000,
      "quality": 0.95
    }
  ]
}
```

An `index.json` file is also generated listing all exported symbols grouped by asset class.

### Data Privacy

The `data/` folder is gitignored. Exported JSON files contain proprietary historical options data
and should remain **LOCAL ONLY** - do not commit to public repositories.

### Demo Mode

The Blazor application includes a built-in demo mode with simulated SPY data (2020-2025) that works without any data files.

## Blazor Application Features

The GEX Visualizer (Blazor) includes:

- **Dual View Comparison**: Normalized (Practitioner) vs Absolute (S² Scaled) GEX
- **Pattern Annotations**: Mark patterns on the timeline for LLM training data
- **Keyboard Shortcuts**: Press `?` to see all shortcuts
- **Settings Persistence**: Playback speed and axis scales saved to localStorage
- **Export as PNG**: Export charts via camera button in header
- **Accessibility**: Full ARIA labels for screen readers

### Research Tools (via Home Page)

- **Research Arcade** (`/arcade`) - Research visualizations hub
- **Research Notebook** (`/notebook`) - Document observations and hypotheses
- **Paper Trading** (`/trading`) - Log theoretical trades with P&L tracking
- **Backtest Results** (`/backtests`) - Compare strategy performance
- **Research Tasks** (`/tasks`) - Kanban-style task board

## File Structure

```text
├── export_data.py   # SQLite → JSON export script
├── data/            # (gitignored) Exported JSON files
└── README.md        # This file
```

## Related

- [Research Visuals](../research-visuals/) - Interactive research visualizations
- [Main README](../../README.md) - Project overview and setup
