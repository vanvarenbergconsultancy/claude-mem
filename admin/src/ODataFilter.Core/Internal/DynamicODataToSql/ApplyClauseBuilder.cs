// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
// Fix #58: field name decoding extended to all _x[hex]_ patterns via ODataToSqlConverter.DecodeFieldName().
#nullable disable
#pragma warning disable CA1859 // parameter types: QueryNode is correct here since Parameters/Expression properties are typed QueryNode
namespace ODataFilter.Core.Internal.DynamicODataToSql;

using System;
using System.Linq;

using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

using SqlKata;

internal static class ApplyClauseBuilder
{
    public static Query BuildApplyClause(Query queryIn, ApplyClause applyClause, bool tryToParseDates)
    {
        ArgumentNullException.ThrowIfNull(queryIn);
        ArgumentNullException.ThrowIfNull(applyClause);

        var i = 0;
        foreach (var node in applyClause.Transformations)
        {
            // Supported patterns: Aggregate, GroupBy, GroupBy+Aggregate,
            // Filter+GroupBy, Filter+Aggregate, Filter+GroupBy+Filter, Filter+GroupBy+Aggregate.
            if (i > 0 && applyClause.Transformations.ElementAt(i - 1).Kind != TransformationNodeKind.Filter)
            {
                queryIn = new Query().From(queryIn);
            }

            switch (node.Kind)
            {
                case TransformationNodeKind.Aggregate:
                    return VisitAggregate(queryIn, node as AggregateTransformationNode);
                case TransformationNodeKind.GroupBy:
                    queryIn = VisitGroupBy(queryIn, node as GroupByTransformationNode);
                    break;
                case TransformationNodeKind.Filter:
                    queryIn = VisitFilter(queryIn, node as FilterTransformationNode, tryToParseDates);
                    break;
                case TransformationNodeKind.Compute:
                    queryIn = VisitCompute(queryIn, node as ComputeTransformationNode);
                    break;
                default:
                    throw new NotSupportedException($"TransformationNode kind {node.Kind:g} is not supported.");
            }
            i++;
        }
        return queryIn;
    }

    private static Query VisitAggregate(Query queryIn, AggregateTransformationNode nodeIn)
    {
        foreach (var expr in nodeIn.AggregateExpressions.OfType<AggregateExpression>())
        {
            if (expr.AggregateKind == AggregateExpressionKind.PropertyAggregate)
            {
                var col = GetColumnName(expr.Expression);
                var alias = expr.Alias;
                queryIn = expr.Method switch
                {
                    AggregationMethod.Sum or AggregationMethod.Min or AggregationMethod.Max =>
                        queryIn.SelectRaw($"{expr.Method:g}(\"{col}\") AS \"{alias}\""),
                    AggregationMethod.Average =>
                        queryIn.SelectRaw($"AVG(\"{col}\") AS \"{alias}\""),
                    AggregationMethod.CountDistinct =>
                        queryIn.SelectRaw($"COUNT(DISTINCT \"{col}\") AS \"{alias}\""),
                    AggregationMethod.VirtualPropertyCount =>
                        queryIn.SelectRaw($"COUNT(1) AS \"{alias}\""),
                    AggregationMethod.Custom =>
                        throw new NotSupportedException("Custom aggregate expressions are not supported."),
                    _ =>
                        throw new NotSupportedException($"Aggregate method {expr.Method:g} is not supported."),
                };
            }
        }

        return queryIn;
    }

    private static Query VisitGroupBy(Query queryIn, GroupByTransformationNode nodeIn)
    {
        foreach (var groupByProperty in nodeIn.GroupingProperties)
        {
            var columnName = GetColumnName(groupByProperty.Expression);
            queryIn = queryIn.Select(columnName).GroupBy(columnName);
        }

        if (nodeIn.ChildTransformations?.Kind == TransformationNodeKind.Aggregate)
        {
            queryIn = VisitAggregate(queryIn, nodeIn.ChildTransformations as AggregateTransformationNode);
        }

        return queryIn;
    }

    private static Query VisitCompute(Query queryIn, ComputeTransformationNode nodeIn)
    {
        queryIn = queryIn.SelectRaw("*");
        foreach (var computeExpression in nodeIn.Expressions)
        {
            if (computeExpression.Expression is SingleValueFunctionCallNode se)
            {
                switch (se.Name.ToUpperInvariant())
                {
                    case "YEAR":
                    case "MONTH":
                    case "DAY":
                    case "HOUR":
                    case "MINUTE":
                        var columnName = GetColumnName(se.Parameters.Single());
                        queryIn = queryIn.SelectRaw($"{se.Name}({columnName}) as {computeExpression.Alias}");
                        break;
                    default:
                        throw new NotSupportedException($"Compute function '{se.Name}' is not supported.");
                }
            }
            else
            {
                throw new NotSupportedException($"Compute expression type {computeExpression.Expression.GetType().Name} is not supported.");
            }
        }

        return queryIn;
    }

    private static Query VisitFilter(Query queryIn, FilterTransformationNode nodeIn, bool tryToParseDates)
    {
        var filterClauseBuilder = new FilterClauseBuilder(queryIn, tryToParseDates);
        return nodeIn.FilterClause.Expression.Accept(filterClauseBuilder);
    }

    private static string GetColumnName(QueryNode node)
    {
        var column = string.Empty;
        if (node.Kind == QueryNodeKind.Convert)
        {
            node = (node as ConvertNode).Source;
        }

        if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
        {
            column = (node as SingleValuePropertyAccessNode).Property.Name.Trim();
        }

        if (node.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
        {
            column = (node as SingleValueOpenPropertyAccessNode).Name.Trim();
        }

        // Fix #58: decode all _x[hex]_ escape sequences, not just space (_x0020_)
        return ODataToSqlConverter.DecodeFieldName(column);
    }
}
