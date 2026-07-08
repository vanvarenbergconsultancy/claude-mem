# ODataFilter.Core

Translates OData `$filter` / `$orderby` / `$top` / `$skip` expressions into parameterised SQL via [SqlKata](https://sqlkata.com/). Designed for Npgsql + Dapper workflows, but works with any SqlKata `Compiler`.

## Installation

```xml
<PackageReference Include="ODataFilter.Core" Version="x.x.x" />
```

## Quick Start — Raw SQL (PostgreSQL)

```csharp
// Register once in DI (singleton)
services.AddODataSqlTranslator(new PostgresCompiler());

// In your handler / repository
public sealed class TeamsRepository(IODataSqlTranslator translator, NpgsqlDataSource db)
{
    public async Task<IReadOnlyList<Team>> ListAsync(ODataQueryOptions options)
    {
        // Scope to the current tenant before user-controlled filter
        var scoped = options.AndFilter("tenantId eq 'abc'");

        var result = translator.Translate("teams", scoped);
        // result.Sql  → "SELECT * FROM "teams" WHERE "tenantId" = @p0 AND ..."
        // result.Parameters → { "@p0": "abc", ... }

        await using var conn = await db.OpenConnectionAsync();
        return await conn.QueryAsync<Team>(result.Sql, result.Parameters);
    }
}
```

## Quick Start — SQL Server

```csharp
services.AddODataSqlTranslator(new SqlServerCompiler());
```

Everything else is identical — SqlKata handles dialect differences.

## Field Mapping

Frontend field names often differ from database column names. Use `ODataFieldMap` to rename them before parsing:

```csharp
private static readonly ODataFieldMap FieldMap = ODataFieldMap.Create()
    .Map("customerId",   "client_identifier")   // rename
    .Map("displayName",  "full_name")
    .Build();

// In your handler:
var mapped = FieldMap.Apply(options);
var result = translator.Translate("teams", mapped);
```

Word-boundary matching ensures `id` inside `clientId` is not accidentally renamed when only `id` is mapped.

## Combining Filters

`AndFilter` adds a mandatory condition that the caller cannot override (e.g. tenant scoping):

```csharp
// User-supplied filter: name eq 'Acme'
// Mandatory scope:      tenantId eq 'abc'
// Combined:            (name eq 'Acme') and (tenantId eq 'abc')
var scoped = userOptions.AndFilter($"tenantId eq '{tenantId}'");
```

## SqlKata Query Composition

Use `TranslateToQuery` when you need to add JOINs or subqueries before compiling:

```csharp
var baseQuery = translator.TranslateToQuery("orders", options)
    .Join("customers", "customers.id", "orders.customerId")
    .Select("orders.*", "customers.name AS customerName");

var (sql, bindings) = new PostgresCompiler().Compile(baseQuery);
```

## Error Handling

An invalid OData expression (e.g. column name with `~`) throws `ODataFilterParseException`. Catch it at the API boundary and return HTTP 400:

```csharp
try
{
    var result = translator.Translate("teams", options);
    ...
}
catch (ODataFilterParseException ex)
{
    return Results.BadRequest(new { error = ex.Message });
}
```

Column names with special characters must be aliased via `ODataFieldMap` before they reach the parser.

## Supported OData Operators

| OData | SQL |
|---|---|
| `name eq 'Acme'` | `"name" = @p0` |
| `count ne 0` | `"count" != @p0` |
| `price gt 100` | `"price" > @p0` |
| `price ge 100` | `"price" >= @p0` |
| `price lt 100` | `"price" < @p0` |
| `price le 100` | `"price" <= @p0` |
| `contains(name,'ac')` | `"name" LIKE @p0` |
| `startswith(name,'ac')` | `"name" LIKE @p0` |
| `endswith(name,'me')` | `"name" LIKE @p0` |
| `A and B` | `... AND ...` |
| `A or B` | `... OR ...` |
| `id ne payorId` | `"id" != "payorId"` (column-to-column) |
| `$orderby=name asc,createdAt desc` | `ORDER BY "name", "createdAt" DESC` |
| `$top=20` | `LIMIT @p0` |

## Known Limitations

- `$expand`, `$search`, `$select`, `$apply` are not part of the public API.
- Column names containing `~` are not valid OData identifiers — alias them via `ODataFieldMap`.
- `contains()` is case-sensitive by default (generates `LIKE`); wrap with `tolower()` to get `ILIKE` (PostgreSQL only).
