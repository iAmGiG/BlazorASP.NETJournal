# GexVisor.NET

An interactive Gamma Exposure (GEX) analysis tool for financial research,
rebuilt from the ground up with a modern, cross-platform .NET stack.

This is the next-generation version of the GEX Visualizer, migrating from a
JavaScript/Python implementation to a high-performance, self-contained application
using .NET MAUI and Blazor WebAssembly.

## About GEX Analysis

GexVisor analyzes Gamma Exposure (GEX) across options markets to provide
insights into market dynamics and potential volatility drivers. The tool
visualizes complex derivatives data through interactive charts and simulation
tools.

## Tech Stack

- **Framework:** .NET MAUI (cross-platform desktop/mobile)
- **UI:** Blazor WebAssembly (components and layouts)
- **Language:** C# (.NET 8.0+)
- **Data Store:** Client-side SQLite (embedded database)
- **Charting:** JavaScript libraries via Blazor JS Interop

## Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- Git

### Installation

1. Clone the repository:

```bash
git clone https://github.com/iAmGiG/GexVisor.git
cd GexVisor
```

1. Restore dependencies:

```bash
dotnet restore
```

1. Build the project:

```bash
dotnet build
```

1. Run the application:

```bash
dotnet run
```

## Project Structure

```
GexVisor/
├── src/                          # Source code
│   ├── GexVisor.NET/             # Main application
│   ├── MyJournal.core/           # Core models and business logic
│   └── BlazorJournalApp/         # Blazor UI components
├── docs/                         # Documentation
│   ├── research-visuals/         # Interactive research visualizations
│   └── research-archive/         # Historical research materials
├── tests/                        # Unit and integration tests
├── .editorconfig                 # Code style settings
├── .pre-commit-config.yaml       # Git hooks configuration
├── CONTRIBUTING.md               # Development guide
└── README.md                     # This file
```

## Development

### Code Standards

This project enforces code quality through:

- **EditorConfig:** Consistent formatting across editors
  (.NET, Markdown, YAML, JSON)
- **Pre-commit hooks:** Automatic validation before commits
  (markdown, YAML, C#)
- **GitHub Actions:** CI pipeline (linting, build, code quality)

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed development instructions.

### Building and Testing

```bash
# Run tests
dotnet test

# Run specific test file
dotnet test tests/MyProject.Tests.csproj

# Build for release
dotnet build --configuration Release
```

### Git Workflow

Branch naming convention:

- `feature/description` - New features
- `fix/description` - Bug fixes
- `enhancement/description` - Improvements to existing features
- `research/description` - Research or experimental work

See [CONTRIBUTING.md](CONTRIBUTING.md) for commit message guidelines.

## Features (In Development)

- [x] Project infrastructure and CI/CD
- [x] .editorconfig and pre-commit hooks
- [ ] Core GEX visualization engine
- [ ] Real-time data simulation
- [ ] Interactive chart controls
- [ ] Client-side data persistence (localStorage)
- [ ] Advanced database persistence (IndexedDB, API backend)

## Research Visualizations

GexVisor includes interactive research visualizations showcasing the analysis tools
and methodologies. Access them at `/docs/research-visuals/`.

## Contributing

Contributions welcome! Please review [CONTRIBUTING.md](CONTRIBUTING.md) for:

- Development setup
- Code standards and naming conventions
- Pre-commit hooks and GitHub Actions workflow
- Testing requirements
- Commit message guidelines
- Pull request process

## Project Status

**Phase:** Active Development - Infrastructure & Core Migration

This project is currently in the infrastructure and migration phase, establishing
the .NET MAUI/Blazor foundation before implementing core GEX analysis features.

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Questions?

Review the [CONTRIBUTING.md](CONTRIBUTING.md) guide or check existing issues for
discussion threads.
