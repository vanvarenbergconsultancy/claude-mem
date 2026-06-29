# ADR-005: MudBlazor Adapter as a Separate Optional Package

**Status:** Accepted
**Date:** 2026-06-29
**Deciders:** Xavier

## Context

`ODataFilter.Core` is a server-side library. MudBlazor is a Blazor UI component library. A converter from MudBlazor `GridState<T>` → `ODataQueryOptions` is useful, but bundling it into Core or EfCore would force a MudBlazor dependency on all consumers, including pure server-side or non-Blazor projects.

## Decision

`ODataFilter.MudBlazor` is a separate NuGet package with `ODataFilter.Core` as its only project reference. MudBlazor is a peer dependency (consumers must bring their own MudBlazor reference).

The package exposes:
- `MudBlazorODataExtensions.ToODataQueryOptions<T>(this GridState<T>)` — full state conversion
- `ToODataFilter<T>(this IEnumerable<IFilterDefinition<T>>)` — filter definitions only
- `ToODataOrderBy<T>(this IEnumerable<SortDefinition<T>>)` — sort definitions only

All methods are pure client-side string construction with no I/O. The resulting `ODataQueryOptions` is serialized as query string parameters when calling the server-side API.

## Consequences

**Positive:**
- `ODataFilter.Core` and `ODataFilter.EfCore` have zero UI dependencies.
- Projects using a different UI framework (Telerik, DevExpress, custom) are not penalized.
- MudBlazor version upgrades only affect `ODataFilter.MudBlazor`.

**Negative:**
- Three packages to publish/manage instead of one.
- Consumers using MudBlazor must add a second package reference.

## Alternatives Considered

| Alternative | Why Rejected |
|---|---|
| Bundle MudBlazor adapter in `ODataFilter.Core` | Forces MudBlazor dependency on all consumers, including server-only projects |
| Bundle in `ODataFilter.EfCore` | Semantically wrong (EfCore is server-side; MudBlazor is client-side); same dependency pollution |
| Per-project extension methods (no shared package) | Duplicated conversion logic across every admin project that uses MudBlazor |
