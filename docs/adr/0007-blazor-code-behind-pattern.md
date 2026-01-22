# ADR 0007: Adopt Blazor Code-Behind Pattern for Complex Components

**Status**: Accepted
**Date**: 2026-01-22
**Deciders**: Development Team

## Context

Blazor components traditionally use `@code` blocks within `.razor` files. For simple components this works well, but complex components like GexChart.razor accumulated:

- 15+ magic numbers embedded in calculations
- 300+ lines of logic in `@code` block
- Difficult-to-test calculation methods
- DRY violations (value 50 appears 3 times)
- Mixed concerns (markup + logic + constants)

## Decision

Adopt **partial class code-behind pattern** for complex components:

1. **Markup**: `Component.razor` (HTML/Razor syntax only)
2. **Logic**: `Component.razor.cs` (C# partial class)
3. **Styles**: `Component.razor.css` (scoped CSS)

**Criteria for using code-behind**:
- Component has 100+ lines of logic
- Contains magic numbers or complex calculations
- Requires unit testing of logic
- Has reusable helper methods

**Simple components** can continue using `@code` blocks.

## Consequences

**Positive**:
- Magic numbers extracted to named constants
- Testable calculation logic (can unit test without rendering)
- Better separation of concerns
- IntelliSense support in C# files
- Follows ASP.NET Core MVC pattern

**Negative**:
- File count increases (3 files per component vs 2)
- Requires discipline to maintain pattern
- Slight learning curve for team members

## Implementation

**Template**:
```csharp
// Component.razor.cs
namespace GexVisor.UI.Components;

public partial class Component : ComponentBase
{
    private static class Constants
    {
        public const int MagicNumber = 42;
    }

    private void CalculationMethod() { }
}
```

**First Application**: GexChart.razor (as part of Issue #149)

## References

- Microsoft Docs: [ASP.NET Core Blazor component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)
- Issue #169: Magic Numbers Refactoring
- Issue #149: GEX Calculation Engine
