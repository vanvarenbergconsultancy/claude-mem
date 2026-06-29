// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
// Fix #52: right-side property access (column-to-column comparisons now handled).
// Fix #46: leading/trailing spaces in string constants are now preserved.
// Fix #14: column names erroneously wrapped in single quotes are stripped before use.
#pragma warning disable CA1859 // parameter types: QueryNode is correct here since Parameters collections are IEnumerable<QueryNode>
namespace ODataFilter.Core.Internal.DynamicODataToSql;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.OData.UriParser;

using SqlKata;

internal sealed class FilterClauseBuilder(Query query, bool tryToParseDates) : QueryNodeVisitor<Query>
{
    private const DateTimeStyles DATETIMESTYLES = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;
    private Query _query = query;
    private readonly bool _tryToParseDates = tryToParseDates;

    /// <inheritdoc/>
    public override Query Visit(InNode nodeIn)
    {
        if (nodeIn.Right.Kind != QueryNodeKind.CollectionConstant)
        {
            throw new NotSupportedException("Non-constant collection nodes are not supported by the 'in' operator.");
        }

        var leftColumnName = GetColumnName(nodeIn.Left);
        var rightValues = GetCollectionConstantValues((CollectionConstantNode)nodeIn.Right);
        return _query.WhereIn(leftColumnName, rightValues);
    }

    /// <inheritdoc/>
    public override Query Visit(BinaryOperatorNode nodeIn)
    {
        var left = Unwrap(nodeIn.Left);
        var right = Unwrap(nodeIn.Right);

        switch (nodeIn.OperatorKind)
        {
            case BinaryOperatorKind.Or:
            case BinaryOperatorKind.And:
                return ApplyLogicalOperator(nodeIn.OperatorKind, left, right);

            case BinaryOperatorKind.Equal:
            case BinaryOperatorKind.NotEqual:
            case BinaryOperatorKind.GreaterThan:
            case BinaryOperatorKind.GreaterThanOrEqual:
            case BinaryOperatorKind.LessThan:
            case BinaryOperatorKind.LessThanOrEqual:
                return ApplyComparisonOperator(nodeIn.OperatorKind, left, right);

            case BinaryOperatorKind.Add:
            case BinaryOperatorKind.Subtract:
            case BinaryOperatorKind.Multiply:
            case BinaryOperatorKind.Divide:
            case BinaryOperatorKind.Modulo:
                throw new NotSupportedException($"Arithmetic operator '{nodeIn.OperatorKind:g}' is not supported in filter expressions.");

            case BinaryOperatorKind.Has:
                throw new NotSupportedException("The 'has' operator is not supported.");

            default:
                return _query;
        }
    }

    /// <inheritdoc/>
    public override Query Visit(SingleValueFunctionCallNode nodeIn)
    {
        ArgumentNullException.ThrowIfNull(nodeIn);

        var nodes = nodeIn.Parameters.ToArray();
        var caseSensitive = true;
        var columnName = GetColumnName(nodes[0]);
        (caseSensitive, columnName) = GetInnerFunctionCallParameterColumn(nodes, caseSensitive, columnName);

        switch (nodeIn.Name.ToLowerInvariant())
        {
            case "contains":
                return _query.WhereContains(columnName, (string)GetConstantValue(nodes[1])!, caseSensitive);

            case "endswith":
                return _query.WhereEnds(columnName, (string)GetConstantValue(nodes[1])!, caseSensitive);

            case "startswith":
                return _query.WhereStarts(columnName, (string)GetConstantValue(nodes[1])!, caseSensitive);

            case "matchespattern":
                var rawPattern = GetConstantValue(nodes[1]) as string
                    ?? throw new InvalidOperationException("matchespattern requires a string argument.");
                var value = rawPattern.Replace(".*", "%");
                if (value.StartsWith("%5E", StringComparison.InvariantCulture))
                {
                    value = value.Replace("%5E", "");
                }

                if (value[value.Length - 1] == '$')
                {
                    value = value.Substring(0, value.Length - 1);
                }

                return _query.WhereLike(columnName, value, caseSensitive);

            default:
                return _query;
        }
    }

    /// <inheritdoc/>
    public override Query Visit(UnaryOperatorNode nodeIn)
    {
        switch (nodeIn.OperatorKind)
        {
            case UnaryOperatorKind.Not:
                _query = _query.Not();
                if (nodeIn.Operand.Kind is QueryNodeKind.SingleValueFunctionCall
                    or QueryNodeKind.BinaryOperator
                    or QueryNodeKind.In)
                {
                    return nodeIn.Operand.Accept(this);
                }

                return _query;

            default:
                return _query;
        }
    }

    private Query ApplyLogicalOperator(BinaryOperatorKind operatorKind, QueryNode left, QueryNode right)
    {
        _query = _query.Where(q =>
        {
            var lq = left.Accept(new FilterClauseBuilder(q, _tryToParseDates));
            if (operatorKind == BinaryOperatorKind.Or)
            {
                lq = lq.Or();
            }
            return right.Accept(new FilterClauseBuilder(lq, _tryToParseDates));
        });
        return _query;
    }

    private Query ApplyComparisonOperator(BinaryOperatorKind operatorKind, QueryNode left, QueryNode right)
    {
        var op = GetOperatorString(operatorKind);

        if (left.Kind == QueryNodeKind.UnaryOperator)
        {
            _query = _query.Where(q => left.Accept(new FilterClauseBuilder(q, _tryToParseDates)));
            left = ((UnaryOperatorNode)left).Operand;
        }

        if (right.Kind == QueryNodeKind.Constant)
        {
            var value = GetConstantValue(right);
            if (left.Kind == QueryNodeKind.SingleValueFunctionCall)
            {
                _query = ApplyFunction(_query, (SingleValueFunctionCallNode)left, op, value);
            }
            else
            {
                _query = _query.Where(GetColumnName(left), op, value);
            }
        }
        // Fix #52: handle column-to-column comparisons (right side is a property access)
        else if (right.Kind == QueryNodeKind.SingleValuePropertyAccess || right.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
        {
            _query = _query.WhereColumns(GetColumnName(left), op, GetColumnName(right));
        }
        else
        {
            throw new NotSupportedException(
                $"Comparison operator with right-side node kind '{right.Kind:g}' is not supported. " +
                "Only constant values and property access are supported on the right side of a comparison.");
        }

        return _query;
    }

    private Query ApplyFunction(Query q, SingleValueFunctionCallNode leftNode, string operand, object? rightValue)
    {
        var columnName = GetColumnName(leftNode.Parameters.First());
        switch (leftNode.Name.ToUpperInvariant())
        {
            case "YEAR":
            case "MONTH":
            case "DAY":
            case "HOUR":
            case "MINUTE":
                return q.WhereDatePart(leftNode.Name, columnName, operand, rightValue);
            case "DATE":
                return q.WhereDate(columnName, operand, rightValue is DateTime d ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat) : rightValue);
            case "TIME":
                return q.WhereTime(columnName, operand, rightValue is DateTime t ? t.ToString("HH:mm", CultureInfo.InvariantCulture.DateTimeFormat) : rightValue);
            case "TOUPPER":
            case "TOLOWER":
                return q.WhereLike(columnName, rightValue, false);
            case "INDEXOF":
                return ApplyIndexOfFunction(q, leftNode, rightValue, columnName);
            default:
                return q;
        }
    }

    private Query ApplyIndexOfFunction(Query q, SingleValueFunctionCallNode leftNode, object? rightValue, string columnName)
    {
        var nodes = leftNode.Parameters.ToArray();
        var caseSensitive = true;
        (caseSensitive, columnName) = GetInnerFunctionCallParameterColumn(nodes, caseSensitive, columnName);
        return rightValue?.Equals(-1) == true
            ? q.WhereNotContains(columnName, (string)GetConstantValue(nodes[1])!, caseSensitive)
            : q.WhereContains(columnName, (string)GetConstantValue(nodes[1])!, caseSensitive);
    }

    private static (bool CaseSensitive, string ColumnName) GetInnerFunctionCallParameterColumn(QueryNode[] nodes, bool caseSensitive, string columnName)
    {
        var firstNode = nodes[0];
        if (firstNode.Kind == QueryNodeKind.Convert)
        {
            firstNode = ((ConvertNode)firstNode).Source;
        }

        if (firstNode.Kind == QueryNodeKind.SingleValueFunctionCall)
        {
            return GetFunctionCallParameterInfo(caseSensitive, columnName, (SingleValueFunctionCallNode)firstNode);
        }

        return (caseSensitive, columnName);
    }

    private static (bool CaseSensitive, string ColumnName) GetFunctionCallParameterInfo(bool caseSensitive, string columnName, SingleValueFunctionCallNode paramNode)
    {
        var functionName = paramNode.Name.ToUpperInvariant();
        if (string.Equals(functionName, "TOUPPER", StringComparison.Ordinal) ||
            string.Equals(functionName, "TOLOWER", StringComparison.Ordinal))
        {
            caseSensitive = false;
            columnName = GetColumnName(paramNode.Parameters.First());
        }

        return (caseSensitive, columnName);
    }

    private static bool ConvertToDateTimeUTC(string dateTimeString, out DateTime dateTime)
    {
        if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture.DateTimeFormat, DATETIMESTYLES, out var dateTimeValue))
        {
            dateTime = dateTimeValue;
            return true;
        }

        dateTime = default;
        return false;
    }

    private static string GetColumnName(QueryNode node)
    {
        var column = string.Empty;
        if (node.Kind == QueryNodeKind.Convert)
        {
            node = ((ConvertNode)node).Source;
        }

        if (node.Kind == QueryNodeKind.SingleValuePropertyAccess)
        {
            column = ((SingleValuePropertyAccessNode)node).Property.Name.Trim();
        }

        if (node.Kind == QueryNodeKind.SingleValueOpenPropertyAccess)
        {
            column = ((SingleValueOpenPropertyAccessNode)node).Name.Trim();
        }

        // Fix #14: strip single quotes erroneously added by callers around column names
        column = column.Trim('\'');

        return ODataToSqlConverter.DecodeFieldName(column);
    }

    private object? GetConstantValue(QueryNode node)
    {
        if (node.Kind == QueryNodeKind.Convert)
        {
            return GetConstantValue(((ConvertNode)node).Source);
        }

        if (node.Kind == QueryNodeKind.Constant)
        {
            var value = ((ConstantNode)node).Value;
            if (value is string stringValue)
            {
                // Fix #46: preserve leading/trailing whitespace in string values.
                // Trim only for date detection (date literals have no meaningful surrounding spaces).
                if (_tryToParseDates && ConvertToDateTimeUTC(stringValue.Trim(), out var dateTime))
                {
                    return dateTime;
                }

                return stringValue;
            }

            return value;
        }

        if (node.Kind == QueryNodeKind.CollectionConstant)
        {
            return GetCollectionConstantValues((CollectionConstantNode)node);
        }

        return null;
    }

    private IEnumerable<object?> GetCollectionConstantValues(CollectionConstantNode node) =>
        node.Collection.Select(GetConstantValue);

    private static SingleValueNode Unwrap(SingleValueNode node) =>
        node.Kind == QueryNodeKind.Convert ? ((ConvertNode)node).Source : node;

    private static string GetOperatorString(BinaryOperatorKind operatorKind) => operatorKind switch
    {
        BinaryOperatorKind.Equal => "=",
        BinaryOperatorKind.NotEqual => "<>",
        BinaryOperatorKind.GreaterThan => ">",
        BinaryOperatorKind.GreaterThanOrEqual => ">=",
        BinaryOperatorKind.LessThan => "<",
        BinaryOperatorKind.LessThanOrEqual => "<=",
        BinaryOperatorKind.Or => "or",
        BinaryOperatorKind.And => "and",
        _ => string.Empty,
    };
}
