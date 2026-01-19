# Tools

Utility scripts for the GexVisor project.

## export_data.py

Exports GEX research data from SQLite to JSON for the Blazor visualizer.

### Prerequisites

- Python 3.8+
- Access to `.cache/gex_research.db` (premium API data - not included in repo)

### Usage

```bash
# Export all symbols to src/GexVisor.UI/wwwroot/data/
python tools/export_data.py

# Export specific symbols
python tools/export_data.py SPY QQQ IWM

# Export to custom directory
python tools/export_data.py --output /path/to/dir
```

### Output Format

Each symbol generates a JSON file with GEX timeline data. An `index.json` file
is also generated listing all exported symbols grouped by asset class.

### Data Privacy

The exported JSON files are gitignored and should remain **LOCAL ONLY** - they
contain proprietary historical options data.

### Demo Mode

The Blazor application includes a built-in demo mode with simulated SPY data
(2020-2025) that works without any data files.
