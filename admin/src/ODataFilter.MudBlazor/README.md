# ODataFilter.MudBlazor

Converts [MudBlazor](https://mudblazor.com/) `GridState<T>` filter and sort state into `ODataQueryOptions` for use with `ODataFilter.Core` or `ODataFilter.EfCore`.

Client-side only — pure string construction, no I/O, no EF Core dependency.

## Installation

```xml
<PackageReference Include="ODataFilter.MudBlazor" Version="x.x.x" />
```

## Quick Start — MudDataGrid Server-Side Mode

```razor
@inject ITeamsClient TeamsClient

<MudDataGrid T="TeamRow" ServerData="LoadData" Filterable="true" SortMode="SortMode.Multiple">
    <Columns>
        <PropertyColumn Property="t => t.Name" Title="Name" />
        <PropertyColumn Property="t => t.CreatedAt" Title="Created" />
    </Columns>
</MudDataGrid>

@code {
    private async Task<GridData<TeamRow>> LoadData(GridState<TeamRow> state)
    {
        var options = state.ToODataQueryOptions();
        // options.Filter  → "contains(Name,'acme') and ..."
        // options.OrderBy → "CreatedAt desc"

        var page = await TeamsClient.ListAsync(options);
        return new GridData<TeamRow> { Items = page.Items, TotalItems = 0 };
    }
}
```

## Converting Filter Definitions Only

```csharp
string? filter = state.FilterDefinitions.ToODataFilter<TeamRow>();
```

## Converting Sort Definitions Only

```csharp
string? orderBy = state.SortDefinitions.ToODataOrderBy();
```

## Supported Filter Operators

| MudBlazor `FilterOperator` | OData output |
|---|---|
| `String.Contains` | `contains(Field,'Value')` |
| `String.NotContains` | `not contains(Field,'Value')` |
| `String.Equal` | `Field eq 'Value'` |
| `String.NotEqual` | `Field ne 'Value'` |
| `String.StartsWith` | `startswith(Field,'Value')` |
| `String.EndsWith` | `endswith(Field,'Value')` |
| `String.Empty` | `Field eq null` |
| `String.NotEmpty` | `Field ne null` |
| `Number.Equal` | `Field eq Value` |
| `Number.NotEqual` | `Field ne Value` |
| `Number.GreaterThan` | `Field gt Value` |
| `Number.GreaterThanOrEqual` | `Field ge Value` |
| `Number.LessThan` | `Field lt Value` |
| `Number.LessThanOrEqual` | `Field le Value` |
| `DateTime.After` | `Field gt 2024-01-01T00:00:00Z` |
| `DateTime.OnOrAfter` | `Field ge 2024-01-01T00:00:00Z` |
| `DateTime.Before` | `Field lt 2024-01-01T00:00:00Z` |
| `DateTime.OnOrBefore` | `Field le 2024-01-01T00:00:00Z` |
| `Boolean.Is` | `Field eq true` / `Field eq false` |

Multiple active filters are combined with ` and `.

## Value Escaping

- **Strings** — single quotes are doubled: `O'Brien` → `'O''Brien'`
- **DateTime** — converted to UTC ISO 8601: `2024-03-15T09:30:00Z`
- **Numbers / booleans** — formatted with `InvariantCulture`

## Field Names

Filter field names come from `IFilterDefinition<T>.Column?.PropertyName`. In standard MudBlazor usage this is set automatically by the framework from `PropertyColumn.Property`. If `PropertyName` is null (e.g. in unit tests), `Title` is used as a fallback.

Sort field names come from `SortDefinition<T>.SortBy`.

## Notes

- Filter definitions with a null `Value` (except `Empty`/`NotEmpty` operators) are excluded from the output.
- An empty filter or sort list returns `null` (no `$filter` / `$orderby` parameter sent).
- `$top` and `$skip` are not populated from `GridState` — cursor-based pagination is handled separately (see `ODataQueryOptions` with `Top`/`Skip`).
