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

GEX data files are stored in `wwwroot/data/`. The application expects JSON files with the following structure:

```json
{
  "entries": [
    {
      "date": "2024-01-02",
      "gexValue": 1234567890,
      "price": 4750.25,
      "regime": "positive"
    }
  ]
}
```

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
