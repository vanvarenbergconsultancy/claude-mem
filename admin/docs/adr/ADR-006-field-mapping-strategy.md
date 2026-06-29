# ADR-006: Field Mapping Strategy

**Status:** Accepted
**Date:** 2026-06-29
**Deciders:** Xavier

## Context

Frontend field names diverge from database column names in several ways:

1. **Alias** — UI sends `customerId`, database column is `client_identifier`
2. **Computed fields** — UI sends `fullName`, database has `first_name` + `last_name`
3. **Security** — some database columns must not be filterable at all

The OData filter string contains frontend field names. Before the string reaches the OData parser or SQL generator, those names must be translated to safe, real column names.

Two layers exist where mapping can happen:
- **String preprocessing** — replace field names in the raw OData string before parsing
- **Post-parse rewrite** — walk the parsed OData AST and rename nodes

Additionally, each adapter has its own mapping mechanism:
- **Raw SQL** (`ODataFilter.Core`) — `ODataFieldMap` (string preprocessing)
- **EF Core** (`ODataFilter.EfCore`) — Gridify's `IGridifyMapper<T>` (expression-level mapping)

## Decision

### Simple renames: `ODataFieldMap` (string preprocessing)

`ODataFieldMap` applies word-boundary regex replacements to the raw `$filter` and `$orderby` strings before they reach the parser. Word-boundary matching (`\bcustomerId\b`) prevents partial matches (e.g., replacing `id` inside `clientId`).

```csharp
var map = ODataFieldMap.Create()
    .Map("customerId", "client_identifier")
    .Map("displayName", "full_name")
    .Build();

var mapped = map.Apply(options);  // rewrite field names in filter/orderby strings
var result = translator.Translate("orders", mapped);
```

### Computed columns: PostgreSQL `GENERATED ALWAYS AS ... STORED`

Fields like `fullName` that require SQL expressions (`first_name || ' ' || last_name`) are exposed as real columns via PostgreSQL generated columns. This keeps the filter/sort logic in the database and avoids complex AST rewriting:

```sql
ALTER TABLE users ADD COLUMN full_name TEXT
    GENERATED ALWAYS AS (first_name || ' ' || last_name) STORED;
```

The frontend sends `fullName`, `ODataFieldMap` rewrites to `full_name`, and the SQL filter targets the generated column directly.

### EF Core field security: `IGridifyMapper<T>`

Gridify's mapper is the security boundary for EF Core queries. Fields not added to the mapper cannot be filtered or sorted — they are silently ignored. This prevents arbitrary column access.

```csharp
public sealed class TeamMapper : GridifyMapper<Team>
{
    public TeamMapper()
    {
        AddMap("name",       t => t.Name);
        AddMap("createdAt",  t => t.CreatedAt);
        AddMap("customerId", t => t.ClientIdentifier);
        // Sensitive fields (e.g. internalNotes) are simply not added
    }
}
```

## Consequences

**Positive:**
- String preprocessing is simple to understand and test.
- No AST rewriting complexity.
- Generated columns keep computed field logic in the database (indexed, consistent).
- Gridify mapper provides a natural security allowlist for EF Core.

**Negative:**
- `ODataFieldMap` rewrites strings before parse — a field name that is a substring of another field name requires careful word-boundary matching.
- Generated columns require a migration for each computed field.
- Raw SQL callers must manage the `ODataFieldMap` themselves (not automatic).

## Alternatives Considered

| Alternative | Why Rejected |
|---|---|
| Post-parse AST rewrite | Complex OData AST visitor; same maintenance burden as the custom LINQ visitor from ADR-004 |
| EDM schema-level property aliases | Requires EDM model registration per field; not supported in the open-type model we use |
| Single mapper for all adapters | Core (raw SQL) and EfCore (Gridify) have incompatible mapper APIs; a unified mapper abstraction adds complexity without benefit |
