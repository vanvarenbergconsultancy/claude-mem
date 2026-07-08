# ODataFilter.EfCore

Applies OData `$filter`, `$orderby`, `$top`, and `$skip` expressions to an `IQueryable<T>` via [Gridify](https://alirezanet.github.io/Gridify/). Works with EF Core + any supported provider (PostgreSQL, SQL Server, SQLite, etc.).

## Installation

```xml
<PackageReference Include="ODataFilter.EfCore" Version="x.x.x" />
```

## Quick Start — EF Core + PostgreSQL

```csharp
// In your handler / repository
public async Task<IReadOnlyList<Team>> ListAsync(ODataQueryOptions options, CancellationToken ct)
{
    return await _context.Teams
        .ApplyODataOptions(options)
        .ToListAsync(ct);
}
```

`ApplyODataOptions` translates the OData AST to Gridify filter syntax and applies `ApplyFiltering()` / `ApplyOrdering()` / `Take()` in order.

## Field Aliasing and Security with GridifyMapper

Gridify's `IGridifyMapper<T>` controls which fields are filterable and how they map to entity properties. Fields not in the mapper are silently ignored — this is your security boundary.

```csharp
public sealed class TeamMapper : GridifyMapper<Team>
{
    public TeamMapper()
    {
        AddMap("name",       t => t.Name);
        AddMap("createdAt",  t => t.CreatedAt);
        AddMap("customerId", t => t.ClientIdentifier);  // frontend alias → property
        // Sensitive fields (e.g. internalNotes) are simply not added
    }
}

// Pass the mapper to ApplyODataOptions:
return await _context.Teams
    .ApplyODataOptions(options, new TeamMapper())
    .ToListAsync(ct);
```

## Filtering Only

```csharp
query = query.ApplyODataFilter<Team>("contains(name,'acme') and active eq true");
```

## Sorting Only

```csharp
query = query.ApplyODataOrderBy<Team>("createdAt desc, name asc");
```

## Null / Empty Options

Passing `null` or `ODataQueryOptions.Empty` returns the source queryable unchanged — no WHERE, no ORDER BY, no LIMIT:

```csharp
query.ApplyODataOptions(null)    // ← safe, returns source
query.ApplyODataOptions(ODataQueryOptions.Empty)  // ← same
```

## Multi-Layer Architecture

`ODataQueryOptions` is a plain serialisable record. Pass it through HTTP from UI → BFF → service:

```csharp
// BFF receives from MudBlazor UI, adds a mandatory tenant scope, forwards to data service
var scoped = userOptions.AndFilter($"tenantId eq '{tenantId}'");
var response = await dataClient.ListTeamsAsync(scoped);

// Data service applies to EF Core
return await _context.Teams.ApplyODataOptions(scoped).ToListAsync(ct);
```

## Supported OData Operators

| OData | Gridify (internal) |
|---|---|
| `X eq 'v'` | `X==v` |
| `X ne 'v'` | `X!=v` |
| `X gt v` | `X>v` |
| `X ge v` | `X>=v` |
| `X lt v` | `X<v` |
| `X le v` | `X<=v` |
| `contains(X,'v')` | `X=*v` |
| `startswith(X,'v')` | `X^=v` |
| `endswith(X,'v')` | `X$=v` |
| `A and B` | `A,B` |
| `A or B` | `A\|B` |

## Known Limitations

- `$apply`, `$expand`, date functions (`year()`, `month()`) are not translated and are ignored.
- Gridify mapper is the authoritative field allowlist; ODataFieldMap string preprocessing is not needed for the EF Core adapter.
