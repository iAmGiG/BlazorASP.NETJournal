# Contributing to GexVisor

Thank you for your interest in contributing! This guide explains how to set
up your development environment and follow our code standards.

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- Git

### Initial Setup

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

## Code Standards

This project enforces code quality through:

### EditorConfig

All C# code follows the `.editorconfig` specification:

- 4-space indentation for C#
- 2-space indentation for Markdown, YAML, JSON
- Consistent naming conventions (interfaces start with `I`)
- Controlled whitespace and formatting

**Most editors support EditorConfig natively.** If yours doesn't, install a plugin.

### Pre-Commit Hooks

Pre-commit hooks validate code before you push it. This prevents bad commits from reaching the remote repository.

#### Installation

Install pre-commit framework:

**macOS/Linux:**

```bash
brew install pre-commit
```

**Windows:**

```bash
pip install pre-commit
```

or with pipx:

```bash
pipx install pre-commit
```

Then install the hooks:

```bash
pre-commit install
```

#### What Gets Checked

When you run `git commit`, these validations run automatically:

- Markdown files validated against `.markdownlint.json`
- YAML files checked for valid syntax
- Trailing whitespace removed
- Large files blocked (>1MB)
- C# code formatting verified with `dotnet format`

#### Running Manually

To run pre-commit checks on all files:

```bash
pre-commit run --all-files
```

To run on specific files:

```bash
pre-commit run --files src/**/*.cs
```

#### Bypassing Hooks (Not Recommended)

If you absolutely need to bypass hooks:

```bash
git commit --no-verify
```

**Only do this if you have a good reason, and expect CI to catch issues.**

## GitHub Actions Workflow

Pull requests are automatically validated by GitHub Actions:

1. **Markdown Linting** - Ensures documentation quality
2. **Build Verification** - Confirms code compiles with .NET 8
3. **Code Formatting** - Verifies consistent C# style
4. **Code Quality** - Static analysis with StyleCop

All checks must pass before merging. If a check fails:

- Click "Details" next to the failed check
- Review the output
- Fix issues locally
- Commit and push again

## Making Changes

### Branch Naming

- Feature: `feature/description`
- Bug fix: `fix/description`
- Enhancement: `enhancement/description`
- Research: `research/description`

Example: `feature/add-navigation-component`

### Commit Messages

Write clear, concise commit messages:

```
Short summary (50 chars or less)

Optional longer description explaining the change.
Include context about why the change was needed.

- Bullet points for multiple changes
- Keep each line under 80 characters
```

Example:

```
Fix ToDoForm undefined Tasks reference

- Import ToDoTask from MyJournal.core
- Implement SaveTasks() method
- Ensure page renders without errors
```

### Pull Request Guidelines

1. Keep PRs focused on a single feature or fix
2. Include a clear description of changes
3. Reference related GitHub issues
4. Ensure all CI checks pass
5. Request review from team members

## C# Code Style

### Naming Conventions

- **Classes/Records:** `PascalCase` (`JournalFramework`)
- **Methods:** `PascalCase` (`SaveTasks()`)
- **Properties:** `PascalCase` (`CurrentYear`)
- **Interfaces:** `IPascalCase` (`IDataService`)
- **Private fields:** `_camelCase` (`_backingField`)
- **Constants:** `UPPER_SNAKE_CASE` (`MAX_RETRY_COUNT`)

### Structure Example

```csharp
namespace MyJournal.Core;

public class JournalFramework
{
    // Properties first
    public Guid Id { get; set; }
    public string Title { get; set; }

    // Constructors
    public JournalFramework()
    {
        Id = Guid.NewGuid();
    }

    // Public methods
    public void Save()
    {
        // Implementation
    }

    // Private methods
    private void Validate()
    {
        // Implementation
    }
}
```

## Testing

### Running Tests

```bash
dotnet test
```

### Writing Tests

Place tests in `*.Tests` projects with naming convention:

- File: `[FeatureName]Tests.cs`
- Class: `[Feature]Tests`
- Method: `[Method]_[Condition]_[Expected]()`

Example:

```csharp
[Fact]
public void SaveTasks_WithValidTasks_SavesSuccessfully()
{
    // Arrange
    var tasks = new List<ToDoTask> { /* ... */ };

    // Act
    var result = service.SaveTasks(tasks);

    // Assert
    Assert.True(result);
}
```

## Documentation

- Update `README.md` for major feature additions
- Document complex logic with comments
- Keep `ISSUES_BACKLOG.md` current with project status
- Add docstrings to public API methods

## Getting Help

- Check existing GitHub issues
- Review `ISSUES_BACKLOG.md` for context
- Ask questions in pull request discussions
- Check documentation in `docs/` folder

## License

By contributing, you agree that your contributions will be licensed under the project's license.

---

**Happy coding! Thanks for contributing to GexVisor.**
