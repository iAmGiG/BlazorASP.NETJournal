# Getting Started

This guide covers how to build, run, and develop GexVisor locally.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A modern web browser (Chrome, Firefox, Edge)
- Git

## Quick Start

```bash
# Clone the repository
git clone https://github.com/iAmGiG/GexVisor.git
cd GexVisor

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/GexVisor.UI
```

The application will start at `https://localhost:5001` (or the port shown in the console).

## Project Structure

```
GexVisor/
├── src/
│   └── GexVisor.UI/           # Blazor WebAssembly application
│       ├── Components/        # Reusable Blazor components
│       ├── Models/            # Data models and types
│       ├── Pages/             # Routable page components
│       ├── Services/          # Application services
│       └── wwwroot/           # Static assets (CSS, data files)
├── tests/
│   └── GexVisor.UI.Tests/     # Unit and component tests
└── docs/                      # Documentation
```

## Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish for deployment
dotnet publish src/GexVisor.UI -c Release -o ./publish
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Development Workflow

### Hot Reload

For development with hot reload:

```bash
dotnet watch run --project src/GexVisor.UI
```

### Code Formatting

The project uses `dotnet format` for consistent code style:

```bash
dotnet format
```

### Versioning

The project uses [GitVersion](https://gitversion.net/) for semantic versioning. Tags and branch names determine the version:

- `main` branch: Release versions
- `development` branch: Alpha versions
- `feature/*` branches: Inherit from parent
- Tags: `v1.0.0` format

## Configuration

### Data Files

GEX data is stored in `wwwroot/data/` using the following structure:

**Index file** (`wwwroot/data/index.json`):

```json
{
  "asset_classes": {
    "equity": ["SPY", "QQQ"],
    "etf": ["GLD", "TLT"]
  },
  "symbols": [
    {
      "symbol": "SPY",
      "asset_class": "equity",
      "count": 1010,
      "date_range": {
        "start": "2021-12-07",
        "end": "2025-12-15"
      }
    }
  ]
}
```

**Symbol data files** (`wwwroot/data/SPY.json`):

```json
{
  "symbol": "SPY",
  "asset_class": "equity",
  "date_range": {
    "start": "2021-12-07",
    "end": "2025-12-15"
  },
  "count": 1010,
  "timeline": [
    {
      "date": "2021-12-07",
      "price": 469.50,
      "gex_value": 1234567890,
      "call_gex": 800000000,
      "put_gex": 434567890,
      "total_volume": 50000
    }
  ]
}
```

**C# Models:**
- `GexIndex` - Index file with asset classes and symbol metadata
- `SymbolInfo` - Symbol metadata with nested `DateRange`
- `DateRange` - Start/end dates (DateOnly type)
- `GexTimeline` - Full symbol dataset
- `GexDataPoint` - Individual day's GEX data

### Local Storage

The application uses browser localStorage for:

- User preferences
- Paper trading journal entries
- Backtest results
- Pattern annotations
- GitHub OAuth tokens (if using GitHub integration)

## Troubleshooting

### Build Errors

**GitVersion errors**: Ensure you have full git history:

```bash
git fetch --unshallow
```

**Package restore failures**: Clear the NuGet cache:

```bash
dotnet nuget locals all --clear
dotnet restore
```

### Runtime Issues

**Blank page on load**: Check browser console for WASM loading errors. Ensure you're using a supported browser.

**Data not loading**: Verify JSON files exist in `wwwroot/data/` and are valid JSON.

## Next Steps

- [GitHub Integration](github-integration.md) - Connect to GitHub Projects
- [Research Roadmap](research-roadmap.md) - Understand the research context
- [Architecture Decisions](adr/) - Review technical decisions
