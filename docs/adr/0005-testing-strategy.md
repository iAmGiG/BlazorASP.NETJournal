# ADR-0005: Testing Strategy (xUnit, Moq, bUnit)

## Status

Accepted (2022-06)

## Context

GexVisor needs automated testing to prevent regressions. Project has 2 test projects:

- `GexVisor.Core.Tests` (17 tests) - Model/logic tests
- `GexVisor.UI.Tests` (172 tests) - Service/component tests

Testing requirements:

- Unit tests for services (business logic)
- Component tests for Blazor UI
- Mock external dependencies (LocalStorage, GitHub API)
- Fast test execution (<5 seconds total)

## Decision

Use xUnit as test framework, Moq for mocking, bUnit for Blazor component testing.

## Test Organization

```
tests/
├── GexVisor.Core.Tests/
│   ├── JournalFrameworkTests.cs       # Model tests (OptionsLog, TradeLog)
│   └── [other model tests]
│
└── GexVisor.UI.Tests/
    ├── Services/
    │   ├── GexDataServiceTests.cs     # Service logic tests
    │   ├── NotebookServiceTests.cs
    │   └── PaperTradeServiceTests.cs
    │
    └── Components/
        ├── GexChartTests.cs           # bUnit component tests
        └── TradeDetailModalTests.cs
```

## Testing Patterns

### Pattern 1: Service Tests (Moq for Mocking)

**Example:** `NotebookServiceTests.cs`

```csharp
public class NotebookServiceTests
{
    private readonly Mock<ILocalStorageService> _mockStorage;
    private readonly NotebookService _service;

    public NotebookServiceTests()
    {
        _mockStorage = new Mock<ILocalStorageService>();
        _service = new NotebookService(_mockStorage.Object);
    }

    [Fact]
    public async Task LoadAsync_EmptyStorage_ReturnsEmptyList()
    {
        // Arrange
        _mockStorage.Setup(x => x.GetItemAsync<string>(It.IsAny<string>()))
                    .ReturnsAsync((string?)null);

        // Act
        var result = await _service.LoadAsync();

        // Assert
        Assert.Empty(result);
    }
}
```

**Rationale:**

- Moq: Familiar to .NET devs, fluent API
- Avoids real LocalStorage: Tests don't depend on browser

### Pattern 2: Component Tests (bUnit)

**Example:** `GexChartTests.cs`

```csharp
public class GexChartTests : TestContext
{
    [Fact]
    public void Render_ValidData_DisplaysChart()
    {
        // Arrange
        var mockState = new Mock<GexStateService>();
        Services.AddSingleton(mockState.Object);

        // Act
        var cut = RenderComponent<GexChart>();

        // Assert
        cut.MarkupMatches(@"<div class=""chart-panel"">...</div>");
    }
}
```

**Rationale:**

- bUnit: Official Blazor testing library
- Renders components in test context (no browser required)
- Markup assertions: Verify HTML output

## Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

**Examples:**

- `LoadAsync_EmptyStorage_ReturnsEmptyList`
- `AddAsync_ValidEntry_SavesAndNotifies`
- `FilterByTags_MultipleMatches_ReturnsFiltered`

## Consequences

### Positive

- 189 tests passing: High confidence in refactorings
- Fast execution: 2-3 seconds total (no browser startup)
- Easy mocking: ILocalStorageService mockable with Moq
- Component tests: bUnit prevents UI regressions

### Negative

- No E2E tests: Manual testing required for full user flows
- No integration tests: LocalStorage not tested against real browser

## Alternatives Considered

### 1. NUnit

- **Pros**: Familiar to some devs, mature
- **Cons**: xUnit more modern, better async support
- **Rejected**: xUnit preferred by Blazor community

### 2. Playwright E2E Tests

- **Pros**: Tests real browser, full user flows
- **Cons**: Slow (30+ seconds), brittle, requires browser installation
- **Rejected**: Unit tests sufficient, E2E overkill for current project size

### 3. Manual Testing Only

- **Pros**: No test maintenance
- **Cons**: Regressions frequent, time-consuming
- **Rejected**: 189 tests save hours of manual testing

## References

- xUnit: https://xunit.net/
- Moq: https://github.com/moq/moq4
- bUnit: https://bunit.dev/
