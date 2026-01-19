# GEX Data Files

This folder contains GEX timeline data for the "Real Data" mode.

## Setup

The JSON files are **not committed** (proprietary data exports). To enable Real Data mode:

### Export Using tools/export_data.py

```bash
# Export all symbols (default output to this folder)
python tools/export_data.py

# Export specific symbols
python tools/export_data.py SPY QQQ IWM

# Export to custom directory
python tools/export_data.py --output /path/to/dir
```

See [tools/README.md](../../../tools/README.md) for full documentation.

## Demo Mode

Demo mode works without any data files - it uses a hardcoded SPY timeline (2020-2025).
