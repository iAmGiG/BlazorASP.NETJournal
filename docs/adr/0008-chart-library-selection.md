# ADR-0008: Chart Library Selection

## Status

Accepted (2026-01-22)

## Context

GexVisor needed candlestick charting capabilities for the Trade Journal feature to visualize price action around trades. Requirements:

- Candlestick (OHLCV) chart support
- Annotations for trade entry/exit markers
- Interactive tools (zoom, pan, crosshair)
- Export capabilities (PNG, SVG)
- Blazor WebAssembly compatibility
- MIT license or permissive open-source

## Decision

Use **Blazor-ApexCharts 6.1.0** as the charting library.

```xml
<PackageReference Include="Blazor-ApexCharts" Version="6.1.0" />
```

## Consequences

### Positive

- **Native Blazor components**: No JavaScript interop required for basic usage
- **Comprehensive candlestick support**: `ApexCandleSeries` with proper OHLC mapping
- **Rich annotation API**: `AnnotationsPoint`, `AnnotationsYAxis` for markers and price lines
- **Built-in toolbar**: Zoom, pan, selection, export (PNG/SVG) included
- **Active maintenance**: Regular updates, responsive issue handling
- **Good documentation**: Extensive examples for Blazor-specific patterns

### Negative

- **Bundle size**: ~200KB additional JavaScript payload
- **SVG rendering**: Slower than Canvas for large datasets (1000+ points)
- **ApexCharts types**: Some type names differ from JS documentation (e.g., `AnnotationMarker` not `AnnotationsPointMarker`)
- **Limited customization**: Some advanced features require JS interop

## Alternatives Considered

### 1. TradingView Lightweight Charts

- **Pros**: Purpose-built for financial charts, best trading UX, Canvas-based (fast)
- **Cons**: Requires JavaScript interop, no native Blazor wrapper, Apache 2.0 license
- **Rejected**: Integration complexity outweighed benefits for our use case

### 2. Plotly.NET

- **Pros**: Mature library, native .NET, scientific visualization focus
- **Cons**: Heavier bundle (~500KB), candlestick support is secondary, steeper learning curve
- **Rejected**: Overkill for OHLC charting needs

### 3. Custom SVG Rendering

- **Pros**: Full control, no dependencies, consistent with existing SVG components
- **Cons**: High development effort, need to implement zoom/pan/export from scratch
- **Rejected**: Time-to-market priority, feature parity too expensive

### 4. Chart.js via Interop

- **Pros**: Lightweight, wide adoption, good performance
- **Cons**: No native Blazor wrapper, limited candlestick support (requires plugin)
- **Rejected**: Plugin dependency and interop complexity

## Implementation Notes

### Key Types (correct names)

```csharp
// Point markers (entry/exit)
new AnnotationsPoint
{
    Marker = new AnnotationMarker
    {
        Shape = AnnotationMarkerShape.Circle
    },
    Label = new Label { Text = "BTO" }
};

// Y-axis lines (price levels)
new AnnotationsYAxis
{
    Y = 150.00,
    BorderColor = "#00c853"
};
```

### Candlestick Series

```razor
<ApexChart TItem="OhlcvBar">
    <ApexCandleSeries
        TItem="OhlcvBar"
        Items="Bars"
        XValue="@(b => b.Timestamp)"
        Open="@(b => b.Open)"
        High="@(b => b.High)"
        Low="@(b => b.Low)"
        Close="@(b => b.Close)" />
</ApexChart>
```

## References

- Issue #129: Chart Library Selection
- Commit: f1db986
- [Blazor-ApexCharts Documentation](https://apexcharts.github.io/Blazor-ApexCharts/)
- [ApexCharts.js Documentation](https://apexcharts.com/docs/)
