# GEX Data Files

This folder contains GEX timeline data for the "Real Data" mode.

## Setup

The JSON files are **not committed** (proprietary data exports). To enable Real Data mode:

### Option 1: Export Directly Here

```bash
cd docs/gex-visualizer
python export_data.py --blazor
```

### Option 2: Export Specific Symbols

```bash
cd docs/gex-visualizer
python export_data.py --blazor SPY QQQ IWM
```

### Option 3: Copy Manually

If you've already exported to `docs/gex-visualizer/data/`, copy files here:

```bash
cp docs/gex-visualizer/data/*.json src/GexVisor.UI/wwwroot/data/
```

## Demo Mode

Demo mode works without any data files - it uses a hardcoded SPY timeline (2020-2025).
