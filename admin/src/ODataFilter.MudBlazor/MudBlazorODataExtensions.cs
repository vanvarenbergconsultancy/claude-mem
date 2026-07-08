using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MudBlazor;
using ODataFilter.Core;
using ODataFilter.Core.DynamicODataToSql;

namespace ODataFilter.MudBlazor;

/// <summary>Extension methods that convert MudBlazor grid state to <see cref="ODataQueryOptions"/>.</summary>
public static class MudBlazorODataExtensions
{
    /// <summary>Converts a MudBlazor <see cref="GridState{T}"/> to <see cref="ODataQueryOptions"/>.</summary>
    public static ODataQueryOptions ToODataQueryOptions<T>(this GridState<T> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new ODataQueryOptions
        {
            Filter = state.FilterDefinitions.ToODataFilter(),
            OrderBy = state.SortDefinitions.ToODataOrderBy()
        };
    }

    /// <summary>Converts a collection of MudBlazor filter definitions to an OData <c>$filter</c> string.</summary>
    /// <returns>OData filter string, or <see langword="null"/> when the collection is empty or all entries produce no clause.</returns>
    public static string? ToODataFilter<T>(this IEnumerable<IFilterDefinition<T>> filterDefinitions)
    {
        ArgumentNullException.ThrowIfNull(filterDefinitions);

        var clauses = filterDefinitions
            .Select(FilterDefinitionToODataClause)
            .Where(c => c is not null)
            .ToList();

        return clauses.Count == 0 ? null : string.Join($" {Constants.ODataOperations.LogicalOperator.And} ", clauses);
    }

    /// <summary>Converts a collection of MudBlazor <see cref="SortDefinition{T}"/> to an OData <c>$orderby</c> string.</summary>
    /// <returns>OData orderby string, or <see langword="null"/> when the collection is empty.</returns>
    public static string? ToODataOrderBy<T>(this IEnumerable<SortDefinition<T>> sortDefinitions)
    {
        ArgumentNullException.ThrowIfNull(sortDefinitions);

        var parts = sortDefinitions
            .Where(sd => !string.IsNullOrWhiteSpace(sd.SortBy))
            .Select(sd => $"{sd.SortBy} {(sd.Descending ? "desc" : "asc")}")
            .ToList();

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static string? FilterDefinitionToODataClause<T>(IFilterDefinition<T> fd)
    {
        // fd.Title is a fallback for test scenarios where Column cannot be instantiated.
        // In production Blazor always populates Column.PropertyName when a filter is active.
        var field = fd.Column?.PropertyName ?? fd.Title;
        if (string.IsNullOrWhiteSpace(field))
        {
            return null;
        }

        var op = fd.Operator;
        if (string.Equals(op, FilterOperator.String.Empty, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.Equal} null";
        }

        if (string.Equals(op, FilterOperator.String.NotEmpty, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.NotEqual} null";
        }

        var value = fd.Value;
        if (value is null)
        {
            return null;
        }

        var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(stringValue))
        {
            return null;
        }

        return BuildStringClause(field, op, stringValue)
            ?? BuildNumericClause(field, op, value)
            ?? BuildDateTimeClause(field, op, value)
            ?? BuildBooleanClause(field, op, value);
    }

    private static string? BuildStringClause(string field, string? op, string stringValue)
    {
        if (string.Equals(op, FilterOperator.String.Contains, StringComparison.Ordinal))
        {
            return $"{Constants.ODataOperations.ComparisonOperator.Contains}({field},'{EscapeStringValue(stringValue)}')";
        }

        if (string.Equals(op, FilterOperator.String.NotContains, StringComparison.Ordinal))
        {
            return $"{Constants.ODataOperations.ComparisonOperator.NotContains}({field},'{EscapeStringValue(stringValue)}')";
        }

        if (string.Equals(op, FilterOperator.String.StartsWith, StringComparison.Ordinal))
        {
            return $"{Constants.ODataOperations.ComparisonOperator.StartsWith}({field},'{EscapeStringValue(stringValue)}')";
        }

        if (string.Equals(op, FilterOperator.String.EndsWith, StringComparison.Ordinal))
        {
            return $"{Constants.ODataOperations.ComparisonOperator.EndsWith}({field},'{EscapeStringValue(stringValue)}')";
        }

        if (string.Equals(op, FilterOperator.String.Equal, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.Equal} '{EscapeStringValue(stringValue)}'";
        }

        if (string.Equals(op, FilterOperator.String.NotEqual, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.NotEqual} '{EscapeStringValue(stringValue)}'";
        }

        return null;
    }

    private static string? BuildNumericClause(string field, string? op, object value)
    {
        var formatted = FormatNumericValue(value);
        if (string.Equals(op, FilterOperator.Number.Equal, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.Equal} {formatted}";
        }

        if (string.Equals(op, FilterOperator.Number.NotEqual, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.NotEqual} {formatted}";
        }

        if (string.Equals(op, FilterOperator.Number.GreaterThan, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.GreaterThan} {formatted}";
        }

        if (string.Equals(op, FilterOperator.Number.GreaterThanOrEqual, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.GreaterThanOrEqual} {formatted}";
        }

        if (string.Equals(op, FilterOperator.Number.LessThan, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.LessThan} {formatted}";
        }

        if (string.Equals(op, FilterOperator.Number.LessThanOrEqual, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.LessThanOrEqual} {formatted}";
        }

        return null;
    }

    private static string? BuildDateTimeClause(string field, string? op, object value)
    {
        var formatted = FormatDateTimeValue(value);
        if (string.Equals(op, FilterOperator.DateTime.After, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.GreaterThan} {formatted}";
        }

        if (string.Equals(op, FilterOperator.DateTime.OnOrAfter, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.GreaterThanOrEqual} {formatted}";
        }

        if (string.Equals(op, FilterOperator.DateTime.Before, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.LessThan} {formatted}";
        }

        if (string.Equals(op, FilterOperator.DateTime.OnOrBefore, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.LessThanOrEqual} {formatted}";
        }

        return null;
    }

    private static string? BuildBooleanClause(string field, string? op, object value)
    {
        if (string.Equals(op, FilterOperator.Boolean.Is, StringComparison.Ordinal))
        {
            return $"{field} {Constants.ODataOperations.ComparisonOperator.Equal} {FormatNumericValue(value)}";
        }

        return null;
    }

    private static string EscapeStringValue(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static string FormatNumericValue(object value) => value switch
    {
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static string FormatDateTimeValue(object value) => value switch
    {
        DateTime dt => dt.ToUniversalTime().ToString(Constants.Formatting.DateTimeOffsetFormat, CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToUniversalTime().ToString(Constants.Formatting.DateTimeOffsetFormat, CultureInfo.InvariantCulture),
        _ => FormatNumericValue(value)
    };
}
