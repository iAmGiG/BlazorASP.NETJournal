# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the GexVisor project.

## What is an ADR?

An ADR documents a significant architectural decision along with its context and consequences. ADRs help future developers understand why certain decisions were made.

## ADR Template

New ADRs should follow this structure:

```markdown
# ADR-NNNN: Title

## Status

Proposed | Accepted | Deprecated | Superseded by ADR-XXXX

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?

## Alternatives Considered

What other options were evaluated? Why were they rejected?
```

## Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [0001](0001-sqlite-wasm-selection.md) | SQLite WASM Library Selection | Accepted | 2026-01-17 |
| [0002](0002-blazor-wasm-choice.md) | Blazor WebAssembly Architecture Choice | Accepted | 2021-12 |
| [0003](0003-localstorage-persistence.md) | LocalStorage vs. IndexedDB for Persistence | Accepted | 2021-12 |
| [0004](0004-service-layer-patterns.md) | Service Layer Design Patterns | Accepted | 2022-06 |
| [0005](0005-testing-strategy.md) | Testing Strategy (xUnit, Moq, bUnit) | Accepted | 2022-06 |
| [0006](0006-state-management.md) | State Management (Event-Driven vs. Redux) | Accepted | 2022-06 |
| [0007](0007-blazor-code-behind-pattern.md) | Blazor Code-Behind Pattern | Accepted | 2026-01-22 |
| [0008](0008-chart-library-selection.md) | Chart Library Selection (ApexCharts) | Accepted | 2026-01-22 |

## Creating a New ADR

1. Copy the template above
2. Number sequentially: `NNNN-short-title.md`
3. Fill in all sections
4. Add to the index table
5. Submit for review

## References

- [ADR GitHub Organization](https://adr.github.io/)
- [Michael Nygard's ADR Article](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
