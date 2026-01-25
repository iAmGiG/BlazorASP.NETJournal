# Linting Setup for GexVisor

This document explains the linting infrastructure for the GexVisor project.

## Overview

GexVisor uses a three-layer linting approach:

1. **EditorConfig + Analyzers** → IDE warnings while coding
2. **Husky.Net** → Pre-commit checks (blocks bad commits)
3. **GitHub Actions** → CI/CD validation (catches skipped hooks)

## Files Created

### Configuration Files

- **`.editorconfig`** - Code style rules for C#, Markdown, JSON, YAML
- **`Directory.Build.props`** - Shared MSBuild properties with Roslyn analyzers
- **`stylecop.json`** - StyleCop analyzer configuration
- **`.markdownlint.json`** - Markdown linting rules

### Pre-Commit Hooks

- **`.husky/task-runner.json`** - Husky tasks configuration
- **`.husky/pre-commit`** - Pre-commit hook script
- **`.config/dotnet-tools.json`** - Dotnet tool manifest (Husky)

### CI/CD

- **`.github/workflows/lint.yml`** - GitHub Actions workflow for automated linting

## Usage

### Local Development

#### Format Code

```bash
# Format all C# files
dotnet format

# Check formatting without making changes
dotnet format --verify-no-changes
```

#### Run Analyzers

```bash
# Build with all analyzers enabled
dotnet build /p:TreatWarningsAsErrors=true
```

#### Lint Markdown

```bash
# Install markdownlint-cli (one-time)
npm install -g markdownlint-cli

# Lint markdown files
markdownlint 'docs/**/*.md' --config .markdownlint.json
```

### Pre-Commit Hooks

Pre-commit hooks run automatically when you commit:

```bash
git add .
git commit -m "Your commit message"
# Husky runs dotnet format check and build automatically
```

**If pre-commit checks fail:**

1. Fix formatting: `dotnet format`
2. Fix analyzer warnings (shown in build output)
3. Re-stage files: `git add .`
4. Try commit again

**Skip hooks (not recommended):**

```bash
git commit --no-verify -m "Emergency fix"
```

### CI/CD

GitHub Actions automatically run on:
- Push to `development` or `main` branches
- Pull requests targeting `development` or `main`

**Checks performed:**
- C# code formatting (`dotnet format --verify-no-changes`)
- Build with analyzers as errors (`/p:TreatWarningsAsErrors=true`)
- All unit tests (`dotnet test`)
- Markdown linting (`markdownlint`)
- EditorConfig compliance

## Analyzers Installed

### Microsoft.CodeAnalyzers.NetAnalyzers (v10.0.100)
- Built-in .NET code analysis
- Performance, security, design rules
- Updated from v8.0.0 to resolve SDK 10.0.102 compatibility warnings

### StyleCop.Analyzers (v1.2.0-beta.556)
- Code style enforcement
- Documentation rules
- Naming conventions

### Roslynator.Analyzers (v4.12.0)
- Additional code analysis
- Code simplification suggestions
- Best practice recommendations

## EditorConfig Rules

Key rules enforced:

### C# Code Style
- **File-scoped namespaces**: `namespace Foo;` (not `namespace Foo { }`)
- **Private fields**: Must start with `_` (e.g., `_myField`)
- **Interfaces**: Must start with `I` (e.g., `IMyInterface`)
- **Braces**: Always required for if/for/while blocks
- **var usage**: Only when type is apparent

### Naming Conventions
- **Interfaces**: `IPascalCase`
- **Private fields**: `_camelCase`
- **Constants**: `PascalCase`
- **Types**: `PascalCase`
- **Methods/Properties**: `PascalCase`

### Null Safety
- Nullable reference types enabled globally
- CS8600, CS8602, CS8603, CS8604 warnings enforced

## Troubleshooting

### "Husky command not found"

```bash
# Restore dotnet tools
dotnet tool restore
```

### "Pre-commit hook failed"

1. Check what failed (formatting or build):
   ```bash
   dotnet format --verify-no-changes
   dotnet build /p:TreatWarningsAsErrors=true
   ```

2. Fix issues and retry commit

### "Analyzer warnings in IDE but not in build"

- Restart IDE to reload `.editorconfig`
- Run `dotnet build-server shutdown`
- Clean and rebuild: `dotnet clean && dotnet build`

### "Markdown linting fails"

```bash
# See specific markdown errors
markdownlint 'docs/**/*.md' --config .markdownlint.json

# Auto-fix markdown (where possible)
markdownlint 'docs/**/*.md' --fix
```

## Customization

### Disable Specific Rules

Edit `.editorconfig` to change severity:

```ini
# Disable a specific rule
dotnet_diagnostic.CA1062.severity = none

# Change to suggestion instead of warning
dotnet_diagnostic.IDE0055.severity = suggestion
```

### Exclude Files from Formatting

Create `.editorconfig` in specific directory with:

```ini
[*]
generated_code = true
```

### Skip Specific Analyzers

Add to `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CA1062;IDE0058</NoWarn>
</PropertyGroup>
```

## References

- [EditorConfig Documentation](https://editorconfig.org/)
- [Roslyn Analyzers](https://github.com/dotnet/roslyn-analyzers)
- [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Roslynator](https://github.com/dotnet/roslynator)
- [Husky.Net](https://github.com/alirezanet/Husky.Net)
- [markdownlint](https://github.com/DavidAnson/markdownlint)
