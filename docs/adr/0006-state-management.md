# ADR-0006: State Management (Event-Driven vs. Redux)

## Status

Accepted (2022-06)

## Context

Blazor components need to share state across the app:

- GexStateService: Current symbol, price, date
- ComparisonService: Selected assets, filters
- BoardStateService: GitHub projects, tasks

State management patterns evaluated:

- Event-driven (C# events)
- Redux-style (Fluxor library)
- Component parameters (prop drilling)

## Decision

Use event-driven state management with singleton/scoped services and C# events.

## Implementation

**Service Pattern:**

```csharp
// GexStateService.cs
public class GexStateService
{
    private GexState _state = new();
    public event Action? OnStateChanged;

    public GexState State => _state;

    public void UpdateState(GexState newState)
    {
        _state = newState;
        OnStateChanged?.Invoke();
    }
}
```

**Component Subscription:**

```csharp
@implements IDisposable
@inject GexStateService GexState

protected override void OnInitialized()
{
    GexState.OnStateChanged += StateHasChanged;
}

public void Dispose()
{
    GexState.OnStateChanged -= StateHasChanged;
}
```

## Consequences

### Positive

- Simple: No library dependencies, built-in C# events
- Reactive: Components auto-update with `StateHasChanged()`
- Type-safe: Compiler enforces state shape (no string actions)
- Debuggable: Breakpoints in `OnStateChanged` handlers

### Negative

- No time-travel debugging: Can't replay state changes
- Memory leaks: Must remember to unsubscribe (`IDisposable`)
- No middleware: Can't intercept state updates (logging, persistence)

## Alternatives Considered

### 1. Fluxor (Redux for Blazor)

- **Pros**: Predictable state, dev tools, time-travel debugging
- **Cons**: Boilerplate (actions, reducers, effects), 150 KB library
- **Rejected**: Overkill for 22 services, adds complexity

### 2. Component Parameters (Prop Drilling)

- **Pros**: Explicit data flow, no global state
- **Cons**: Tedious for deeply nested components, hard to refactor
- **Rejected**: GexVisor has 25 components, prop drilling infeasible

### 3. Cascading Parameters

- **Pros**: Built-in Blazor feature, no events
- **Cons**: Tightly couples parent/child, hard to test
- **Rejected**: Services with DI more flexible

## References

- Blazor state management: https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management
- Related: [ADR-0004: Service Layer Patterns](0004-service-layer-patterns.md) (Pattern 2)
