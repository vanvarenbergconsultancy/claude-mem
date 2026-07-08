using System.Linq;
using Gridify;
using ODataFilter.Core;

namespace ODataFilter.EfCore;

/// <summary>Extension methods for applying <see cref="ODataQueryOptions"/> to <see cref="IQueryable{T}"/> sources via Gridify.</summary>
public static class ODataFilterQueryableExtensions
{
    /// <summary>Applies all non-null options (filter, orderby, top) from <paramref name="options"/> to <paramref name="source"/>.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="source">The queryable source to filter and sort.</param>
    /// <param name="options">Query options to apply. Null or empty options leave <paramref name="source"/> unchanged.</param>
    /// <param name="mapper">
    /// Optional Gridify mapper for field aliasing and column mapping.
    /// When <see langword="null"/>, Gridify's default reflection-based mapping is used.
    /// </param>
    public static IQueryable<T> ApplyODataOptions<T>(this IQueryable<T> source, ODataQueryOptions? options, IGridifyMapper<T>? mapper = null)
    {
        if (options is null || options == ODataQueryOptions.Empty)
        {
            return source;
        }

        source = source.ApplyODataFilter(options.Filter, mapper);
        source = source.ApplyODataOrderBy(options.OrderBy, mapper);

        if (options.Top.HasValue)
        {
            source = source.Take(options.Top.Value);
        }

        return source;
    }

    /// <summary>Applies an OData <c>$filter</c> expression to <paramref name="source"/>.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="odataFilter">OData filter string. Returns <paramref name="source"/> unchanged when null or empty.</param>
    /// <param name="mapper">Optional Gridify field mapper.</param>
    public static IQueryable<T> ApplyODataFilter<T>(this IQueryable<T> source, string? odataFilter, IGridifyMapper<T>? mapper = null)
    {
        var gridifyFilter = ODataToGridifyTranslator.ToGridifyFilter(odataFilter);
        if (string.IsNullOrEmpty(gridifyFilter))
        {
            return source;
        }

        return mapper is null
            ? source.ApplyFiltering(gridifyFilter)
            : source.ApplyFiltering(gridifyFilter, mapper);
    }

    /// <summary>Applies an OData <c>$orderby</c> expression to <paramref name="source"/>.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="odataOrderBy">OData orderby string. Returns <paramref name="source"/> unchanged when null or empty.</param>
    /// <param name="mapper">Optional Gridify field mapper.</param>
    public static IQueryable<T> ApplyODataOrderBy<T>(this IQueryable<T> source, string? odataOrderBy, IGridifyMapper<T>? mapper = null)
    {
        var gridifyOrdering = ODataToGridifyTranslator.ToGridifyOrdering(odataOrderBy);
        if (string.IsNullOrEmpty(gridifyOrdering))
        {
            return source;
        }

        return mapper is null
            ? source.ApplyOrdering(gridifyOrdering)
            : source.ApplyOrdering(gridifyOrdering, mapper);
    }
}
