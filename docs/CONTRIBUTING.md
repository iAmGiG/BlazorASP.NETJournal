# Contributing to GexVisor

Thank you for your interest in contributing to GexVisor! This guide covers documentation standards and development workflows.

## Documentation Standards

When adding or updating features, follow these documentation requirements:

### Required Documentation Updates

- [ ] Update `docs/architecture.md` with new services/components
- [ ] Add "Last Updated" date to modified documentation files
- [ ] Update test counts in `.claude/CLAUDE.md` if adding tests
- [ ] Create user guide for user-facing features (see existing guides in `docs/`)
- [ ] Update `docs/README.md` routes table if adding new pages

### Architecture Decision Records (ADRs)

Create an ADR in `docs/adr/` for:

- **New architectural patterns** - Novel approaches not documented in existing ADRs
- **Technology selection decisions** - Choosing between competing libraries or frameworks
- **Breaking changes to existing patterns** - Major refactorings affecting multiple components

**ADR Template:** See `docs/adr/README.md` for the standard format.

### Documentation Review Checklist

Before submitting documentation changes:

- [ ] All links verified (no broken references)
- [ ] Code examples tested and functional
- [ ] Cross-references between docs updated
- [ ] Screenshots current (if applicable)
- [ ] Markdown linting passes (`markdownlint docs/**/*.md`)

## Code Quality Standards

### Pre-Commit Checks

The repository uses Husky.Net pre-commit hooks that run:

1. **Format check** - `dotnet format --verify-no-changes`
2. **Build with analyzers** - Full build with warnings as errors

**Bypassing hooks:** Only bypass with `--no-verify` for documentation-only commits or when analyzer warnings are pre-existing and tracked separately.

### Analyzer Rules

- **Microsoft.CodeAnalysis.NetAnalyzers** (v10.0.100) - Performance, security, design rules
- **StyleCop.Analyzers** (v1.2.0-beta.556) - Code style, documentation, naming conventions
- **Roslynator.Analyzers** (v4.12.0) - Code simplification, best practices

See [docs/linting.md](linting.md) for configuration details.

### Testing Requirements

- **All tests must pass** - Current: 352/352 (35 Core + 243 UI + 74 API)
- **New features require tests** - Unit tests for services, component tests for UI
- **Test coverage targets** - Maintain existing coverage levels

Run tests: `dotnet test`

## Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```bash
<type>: <description>

[optional body]
```

**Types:**

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `refactor:` - Code refactoring (no functional changes)
- `test:` - Adding or updating tests
- `chore:` - Build process, dependencies, tooling

**Examples:**

```text
feat: Add real-time GEX alerts with configurable thresholds

docs: Update architecture.md with AlertService documentation

fix: Correct zero-gamma calculation for negative GEX regimes
```

**Important:** Do not include AI attribution signatures in commit messages.

## Development Workflow

### Initial Setup

1. **Prerequisites:**
   - .NET 10 SDK (10.0.102 or later)
   - Node.js 18+ (for development tools)

2. **Clone and build:**

   ```bash
   git clone https://github.com/iAmGiG/GexVisor.git
   cd GexVisor
   dotnet restore
   dotnet build
   ```

3. **Run tests:**

   ```bash
   dotnet test
   ```

### Making Changes

1. **Create a feature branch:**

   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/bug-description
   ```

2. **Make your changes** with appropriate tests and documentation

3. **Verify quality:**

   ```bash
   dotnet format --verify-no-changes
   dotnet build
   dotnet test
   ```

4. **Commit with conventional format:**

   ```bash
   git add .
   git commit -m "feat: Add your feature description"
   ```

5. **Push and create PR:**

   ```bash
   git push -u origin feature/your-feature-name
   ```

   Then create a pull request on GitHub.

## Project Structure

```bash
GexVisor/
├── src/
│   ├── GexVisor.UI/           # Blazor WebAssembly application
│   ├── GexVisor.Api/          # Backend API for live data
│   └── GexVisor.Core/         # Shared models and interfaces
├── tests/
│   ├── GexVisor.UI.Tests/     # UI unit and component tests (243 tests)
│   ├── GexVisor.Api.Tests/    # API unit tests (74 tests)
│   └── GexVisor.Core.Tests/   # Core unit tests (35 tests)
└── docs/                      # Documentation
```

## Getting Help

- **Documentation:** Check [docs/](.) for architecture, guides, and ADRs
- **Issues:** Browse [existing issues](https://github.com/iAmGiG/GexVisor/issues) or create a new one
- **Questions:** Open a discussion or ask in an issue

## Code of Conduct

- Be respectful and constructive in all interactions
- Focus on technical merit and project goals
- Welcome newcomers and help them contribute successfully

---

_Last Updated: 2026-01-24_
