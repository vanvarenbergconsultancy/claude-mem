namespace ODataFilter.Core;

/// <summary>Unified query DTO that carries OData query parameters across all layer boundaries.</summary>
/// <remarks>
/// Immutable record — use <c>with</c> expressions to derive new instances.
/// The <see cref="AndFilter"/> helper combines an additional filter condition without overwriting the caller's filter.
/// </remarks>
public sealed record ODataQueryOptions
{
    /// <summary>OData <c>$filter</c> expression (e.g. <c>contains(name,'acme') and active eq true</c>).</summary>
    public string? Filter { get; init; }

    /// <summary>OData <c>$orderby</c> expression (e.g. <c>name asc, createdAt desc</c>).</summary>
    public string? OrderBy { get; init; }

    /// <summary>OData <c>$top</c> — maximum number of rows to return. Use for non-paginated limits (e.g. autocomplete).</summary>
    public int? Top { get; init; }

    /// <summary>An empty instance with no filter, sort, or paging applied.</summary>
    public static ODataQueryOptions Empty { get; } = new();

    /// <summary>
    /// Returns a new instance with <paramref name="additionalFilter"/> ANDed into the current filter.
    /// Use this in service/handler layers to inject mandatory scope conditions (e.g. tenant isolation).
    /// </summary>
    public ODataQueryOptions AndFilter(string additionalFilter)
    {
        if (string.IsNullOrWhiteSpace(additionalFilter))
        {
            return this;
        }

        var combined = string.IsNullOrEmpty(Filter)
            ? additionalFilter
            : $"({Filter}) and ({additionalFilter})";

        return this with { Filter = combined };
    }
}
