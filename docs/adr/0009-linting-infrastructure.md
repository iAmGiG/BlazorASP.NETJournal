# ADR-0009: Linting Infrastructure

## Status

Accepted (2026-01-22)

## Context

GexVisor needed a consistent code quality enforcement strategy across local development, CI/CD, and team collaboration. The codebase had grown to:

- 90+ C# source files across 3 projects
- 316 unit tests
- 21 markdown documentation files
- Multiple contributors with different IDE configurations

Requirements:
- Enforce consistent code style across the team
- Prevent bad commits from entering the repository
- Catch issues early (in IDE, not in CI)
- Minimal friction for developers
- Support for both C# and Markdown linting

## Decision

Implement a three-layer linting approach:

### Layer 1: EditorConfig + Roslyn Analyzers

- .editorconfig with 268 C# style rules
- Roslyn analyzers (3 packages):
  - Microsoft.CodeAnalysis.NetAnalyzers
  - StyleCop.Analyzers
  - Roslynator.Analyzers
- Configured via Directory.Build.props

### Layer 2: Husky.Net Pre-commit Hooks

- Husky.Net (dotnet tool) for git hook management
- Pre-commit tasks:
  1. dotnet format --verify-no-changes
  2. dotnet build /p:TreatWarningsAsErrors=true

### Layer 3: GitHub Actions CI

- .github/workflows/lint.yml workflow
- Jobs: C# formatting, build with analyzers, tests, markdown linting, EditorConfig compliance

### Key Rules

- File-scoped namespaces (warning)
- Private field naming: _camelCase (warning)
- Braces always required (warning)
- Null safety: CS8600-CS8604 as warnings
- Markdown: 120 char lines, ATX headers, dash lists

## Consequences

### Positive

- Consistency: All code follows same style
- Early feedback: IDE warnings while coding
- Bad commits prevented: Husky blocks violations
- CI safety net: Catches skipped hooks
- Auto-fix: dotnet format fixes most issues
- Documentation quality: Markdown linting
- Zero runtime impact: Build-time only

### Negative

- Initial setup time: ~30 minutes
- Longer builds: +5-10% build time
- Learning curve: EditorConfig rules
- Commit friction: +5-10 seconds per commit
- Cannot auto-fix all: Some StyleCop rules lack fixes

## Alternatives Considered

### 1. EditorConfig Only
- Pros: Simple, lightweight
- Cons: No CI enforcement
- Rejected: Cannot validate in CI

### 2. Pre-commit Framework (Python)
- Pros: Mature ecosystem
- Cons: Python dependency
- Rejected: Prefer .NET-native

### 3. Build-time Only
- Pros: No commit friction
- Cons: Late discovery
- Rejected: Want early catches

### 4. Strict Mode (All Errors)
- Pros: Zero tolerance
- Cons: Too strict
- Rejected: Prefer gradual adoption

## References

- EditorConfig: https://editorconfig.org/
- Roslyn Analyzers: https://github.com/dotnet/roslyn-analyzers
- StyleCop: https://github.com/DotNetAnalyzers/StyleCopAnalyzers
- Roslynator: https://github.com/dotnet/roslynator
- Husky.Net: https://github.com/alirezanet/Husky.Net
- Usage Guide: ../linting.md
