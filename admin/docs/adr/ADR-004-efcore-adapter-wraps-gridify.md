# ADR-004: EF Core Adapter Wraps Gridify

**Status:** Accepted
**Date:** 2026-06-29
**Deciders:** Xavier

## Context

`ODataFilter.EfCore` needs to apply OData `$filter` and `$orderby` expressions to an `IQueryable<T>`. The two approaches are:

1. **Custom OData → LINQ expression visitor** — walk the OData AST from `Microsoft.OData.Core` and emit LINQ `Expression` trees directly.
2. **Translate OData → Gridify filter string, then apply Gridify** — Gridify is an actively maintained library that converts its own filter syntax to IQueryable. We add a translation layer on top.

Research showed a full-fidelity OData → LINQ visitor is approximately 500–800 lines of expression tree manipulation, with ongoing maintenance burden as operator coverage expands.

## Decision

Translate OData `$filter` AST to Gridify filter syntax inside `ODataToGridifyTranslator` (internal), then delegate to `Gridify.ApplyFiltering()` / `ApplyOrdering()` extension methods. The translation layer handles the OData-to-Gridify operator mapping and value escaping.

OData → Gridify operator map:

| OData | Gridify |
|---|---|
| `contains(X,'v')` | `X=*v` |
| `startswith(X,'v')` | `X^=v` |
| `endswith(X,'v')` | `X$=v` |
| `X eq 'v'` | `X==v` |
| `X ne 'v'` | `X!=v` |
| `X gt v` / `ge` / `lt` / `le` | `X>v` / `>=` / `<` / `<=` |
| `X and Y` | `X,Y` |
| `X or Y` | `X\|Y` |

Field aliasing uses Gridify's `IGridifyMapper<T>` — callers define a mapper that controls which fields can be filtered and how they map to entity properties.

## Consequences

**Positive:**
- Gridify handles the IQueryable expression tree complexity (well-tested, actively maintained).
- `IGridifyMapper<T>` provides field allowlisting as a security boundary — unmapped fields cannot be filtered.
- Works with any EF Core provider (PostgreSQL, SQL Server, SQLite).
- Translation layer is ~150 lines vs ~700 for a custom visitor.

**Negative:**
- Translation layer is a leaky abstraction: complex OData expressions that Gridify doesn't support (e.g., `$apply`, date functions) fall through silently.
- Two-step parsing (OData → Gridify string → IQueryable) adds latency (negligible in practice).
- Gridify filter syntax is Gridify-specific; if Gridify is ever replaced, the translator must change.

## Alternatives Considered

| Alternative | Why Rejected |
|---|---|
| Custom OData → LINQ expression visitor | High implementation cost (~700 lines), high maintenance burden as operators expand |
| Use `ODataFilter.Core` (raw SQL) for EF Core too | Bypasses EF Core change tracking and query composition; won't compose with Include(), AsNoTracking(), etc. |
| Direct Gridify with Gridify syntax at the API layer | Violates §237 (OData mandate); different filter format per endpoint |
