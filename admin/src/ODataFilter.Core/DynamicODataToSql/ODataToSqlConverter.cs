// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
// Fix #58: field name decoding extended to all _x[hex]_ patterns, not just _x0020_ (space).
// Fix #41: documented as thread-safe singleton.
// Fix #42: ODataException from the parser is caught and rethrown as ODataFilterParseException.
// Fix #33: improved error message for invalid $top value.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;
using SqlKata;
using SqlKata.Compilers;

namespace ODataFilter.Core.DynamicODataToSql;

/// <summary>Converts OData query strings into parameterised SQL via SqlKata.</summary>
/// <remarks>Thread-safe. Register as singleton in DI.</remarks>
internal sealed class ODataToSqlConverter(IEdmModelBuilder edmModelBuilder, Compiler sqlCompiler) : IODataToSqlConverter
{
    // Fix #58: replaced single-character space constant with a full hex-decode regex.
    private static readonly Regex HexEncodeRegex = new(@"_x(?<hex>[0-9A-Fa-f]{4})_", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    private readonly IEdmModelBuilder _edmModelBuilder = edmModelBuilder ?? throw new ArgumentNullException(nameof(edmModelBuilder));
    private readonly Compiler _sqlCompiler = sqlCompiler ?? throw new ArgumentNullException(nameof(sqlCompiler));

    /// <summary>Decodes all _x[hex]_ escape sequences in an OData field name.</summary>
    internal static string DecodeFieldName(string name)
    {
        return HexEncodeRegex.Replace(name, static m => ((char)Convert.ToInt32(m.Groups["hex"].Value, 16)).ToString());
    }

    /// <inheritdoc/>
    public (string, IDictionary<string, object>) ConvertToSql(string tableName, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true)
    {
        var query = BuildSqlKataQuery(tableName, odataQuery, count, tryToParseDates);

        return CompileSqlKataQuery(query);
    }

    /// <inheritdoc/>
    public Query ConvertToSqlKataQuery(string tableName, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true)
    {
        return BuildSqlKataQuery(tableName, odataQuery, count, tryToParseDates);
    }

    /// <inheritdoc/>
    public (string, IDictionary<string, object>) ConvertToSqlFromRawSql(string rawSql, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true)
    {
        var query = BuildSqlKataQueryFromRawSql(rawSql, odataQuery, count, tryToParseDates);
        
        return CompileSqlKataQuery(query);
    }

    /// <inheritdoc/>
    public Query ConvertToSqlKataQueryFromRawSql(string rawSql, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true)
    {
        return BuildSqlKataQueryFromRawSql(rawSql, odataQuery, count, tryToParseDates);
    }

    private Query BuildSqlKataQueryFromRawSql(string rawSql, IDictionary<string, string> odataQuery, bool count, bool tryToParseDates)
    {
        if (string.IsNullOrWhiteSpace(rawSql))
        {
            throw new ArgumentNullException(nameof(rawSql));
        }

        const string tableName = "RawSql";
        var query = new Query(tableName);
        query = BuildSqlKataQueryFromOdataParameters(query, tableName, odataQuery, count, tryToParseDates);
        query.WithRaw(tableName, rawSql);

        return query;
    }

    private Query BuildSqlKataQuery(string tableName, IDictionary<string, string> odataQuery, bool count, bool tryToParseDates)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentNullException(nameof(tableName));
        }

        return BuildSqlKataQueryFromOdataParameters(new Query(tableName), tableName, odataQuery, count, tryToParseDates);
    }

    private Query BuildSqlKataQueryFromOdataParameters(Query query, string modelName, IDictionary<string, string> odataQuery, bool count, bool tryToParseDates)
    {
        var parser = ParseODataQuery(modelName, odataQuery);

        ApplyClause? applyClause;
        FilterClause? filterClause;
        long? top;
        long? skip;
        OrderByClause? orderByClause;
        SelectExpandClause? selectClause;

        try
        {
            applyClause = parser.ParseApply();
            filterClause = parser.ParseFilter();
            top = ParseTop(parser, odataQuery);
            skip = parser.ParseSkip();
            orderByClause = parser.ParseOrderBy();
            selectClause = parser.ParseSelectAndExpand();
        }
        catch (ODataException ex)
        {
            throw new ODataFilterParseException($"Failed to parse OData query for '{modelName}': {ex.Message}", ex); }

        if (applyClause != null)
        {
            query = ApplyClauseBuilder.BuildApplyClause(query, applyClause, tryToParseDates);
            if (filterClause != null || selectClause != null)
            {
                query = new Query().From(query, "apply");
            }
        }

        if (filterClause != null)
        {
            query = filterClause.Expression.Accept(new FilterClauseBuilder(query, tryToParseDates));
        }

        return count ? query.AsCount() : ApplyPaginationAndSort(query, top, skip, orderByClause, selectClause);
    }

    private static Query ApplyPaginationAndSort(Query query, long? top, long? skip, OrderByClause? orderByClause, SelectExpandClause? selectClause)
    {
        if (top.HasValue)
        {
            query = query.Take(Convert.ToInt32(top.Value));
        }

        if (skip.HasValue)
        {
            query = query.Skip(Convert.ToInt32(skip.Value));
        }

        if (orderByClause != null)
        {
            query = BuildOrderByClause(query, orderByClause);
        }

        if (selectClause != null)
        {
            query = BuildSelectClause(query, selectClause);
        }

        return query;
    }

    private ODataQueryOptionParser ParseODataQuery(string name, IDictionary<string, string> odataQuery)
    {
        try
        {
            var result = _edmModelBuilder.BuildTableModel(name);
            var parser = new ODataQueryOptionParser(result.Item1, result.Item2, result.Item3, odataQuery)
                {
                    Resolver =
                    {
                        EnableCaseInsensitive = true,
                        EnableNoDollarQueryOptions = true
                    }
                };
            return parser;
        }
        catch (ODataException ex)
        {
            // Fix #42: rethrow as typed exception. Column names with special chars (e.g. ~)
            // are not valid OData identifiers — alias them via ODataFieldMap before reaching the parser.
            throw new ODataFilterParseException(
                $"Failed to parse OData query for '{name}': {ex.Message}. " +
                "Column names with special characters must be aliased via ODataFieldMap before parsing.",
                ex);
        }
    }

    private static long? ParseTop(ODataQueryOptionParser parser, IDictionary<string, string> odataQuery)
    {
        try
        {
            return parser.ParseTop();
        }
        catch (ODataException ex)
        {
            // Fix #33: improved error message for invalid $top
            odataQuery.TryGetValue("$top", out var topValue);

            throw new ODataFilterParseException($"Invalid $top value '{topValue}': must be a non-negative integer. Original error: {ex.Message}", ex);
        }
    }

    private (string, IDictionary<string, object>) CompileSqlKataQuery(Query query)
    {
        var sqlResult = _sqlCompiler.Compile(query);

        return (sqlResult.Sql, sqlResult.NamedBindings);
    }

    private static Query BuildOrderByClause(Query query, OrderByClause orderByClause)
    {
        while (orderByClause != null)
        {
            var expressionName = GetSingleValuePropertyAccessNodeName(orderByClause.Expression);
            if (expressionName is not null)
            {
                var columnName = DecodeFieldName(expressionName.Trim());
                query = orderByClause.Direction == OrderByDirection.Ascending
                    ? query.OrderBy(columnName)
                    : query.OrderByDesc(columnName);
            }

            orderByClause = orderByClause.ThenBy;
        }

        return query;
    }

    private static string? GetSingleValuePropertyAccessNodeName(SingleValueNode expression)
    {
        if (expression is SingleValueOpenPropertyAccessNode openProperty)
        {
            return openProperty.Name;
        }

        if (expression is SingleValuePropertyAccessNode property)
        {
            return property.Property.Name;
        }

        return null;
    }

    private static Query BuildSelectClause(Query query, SelectExpandClause selectClause)
    {
        if (!selectClause.AllSelected)
        {
            foreach (var selectItem in selectClause.SelectedItems)
            {
                if (selectItem is PathSelectItem path)
                {
                    query = query.Select(DecodeFieldName(path.SelectedPath.FirstSegment.Identifier.Trim()));
                }
            }
        }

        return query;
    }
}
