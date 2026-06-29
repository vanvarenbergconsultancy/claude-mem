# ADR-001: OData $filter Syntax for Complex API Filtering

**Status:** Accepted
**Date:** 2026-06-29
**Deciders:** Xavier

## Context

The admin API exposes tables (teams, projects, audit log entries) that users need to filter by arbitrary combinations of fields, operators, and values. Simple named query parameters (`?name=x&status=y`) break down when the user needs expressions like "name contains 'acme' AND createdAt after 2024-01-01 OR status equals 'pending'".

Company API guidelines §237 explicitly mandate OData `$filter` syntax for complex filtering on list endpoints.

## Decision

Use OData `$filter` string as the HTTP transport format for all complex filtering. The query string parameter is `$filter` and its value is a valid OData v4 filter expression (e.g., `contains(name,'acme') and createdAt gt 2024-01-01T00:00:00Z`).

This is transport-only: we do not implement the full OData protocol (no `$expand`, no `$metadata`, no entity sets). Only `$filter`, `$orderby`, and `$top` are supported. `$skip` is explicitly excluded — it is incompatible with cursor-based pagination (see ADR-002).

## Consequences

**Positive:**
- Standardised, well-understood syntax with existing tooling and documentation.
- Single format works for any combination of fields and operators without adding new query parameters per field.
- Company guidelines compliance out of the box.
- MudBlazor UI can construct OData filter strings from its built-in `FilterOperator` constants.

**Negative:**
- Filter strings must be parsed and validated server-side (mitigated by `ODataFilter.Core`).
- Column names with special characters (e.g. `~`) are not valid OData identifiers and must be aliased via `ODataFieldMap` before they reach the parser.
- Clients constructing filter strings must handle OData escaping (single quotes doubled: `O''Brien`).

## Alternatives Considered

| Alternative | Why Rejected |
|---|---|
| Gridify native syntax (`name=*acme,active=true`) as the HTTP transport format | Not standardised; violates §237. Gridify is used as the EF Core adapter implementation (see ADR-004) — the rejection is of its filter syntax as the HTTP API contract, not of the library itself |
| Custom JSON array `[{field, op, value}]` | Verbose, no standard; still requires parsing |
| LHS bracket params (`filter[name][contains]=acme`) | Verbose, non-standard, hard to express OR/AND nesting |
