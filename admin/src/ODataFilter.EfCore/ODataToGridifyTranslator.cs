using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ODataFilter.Core;
using ODataFilter.Core.DynamicODataToSql;

namespace ODataFilter.EfCore;

/// <summary>Translates an OData <c>$filter</c> string into a Gridify filter string.</summary>
/// <remarks>
/// Gridify filter syntax reference: https://alirezanet.github.io/Gridify/guide/filtering.html
/// This translator walks the OData AST and emits the Gridify equivalent operator-by-operator.
/// </remarks>
internal static class ODataToGridifyTranslator
{
    private static readonly (EdmModel Model, IEdmEntityType EntityType, IEdmEntitySet EntitySet) SharedState = CreateSharedModel();

    private static (EdmModel, IEdmEntityType, IEdmEntitySet) CreateSharedModel()
    {
        const string ns = "ODataFilterEfCore";
        const string name = "Entity";
        
        var model = new EdmModel();
        
        var entityType = new EdmEntityType(ns, name, null, false, true);
        model.AddElement(entityType);
        
        var container = new EdmEntityContainer(ns, "Container");
        model.AddElement(container);
        
        var entitySet = container.AddEntitySet(name, entityType);

        return (model, entityType, entitySet);
    }

    /// <summary>
    /// Converts an OData <c>$filter</c> expression string to a Gridify filter string.
    /// Returns <see langword="null"/> when <paramref name="odataFilter"/> is null or whitespace.
    /// </summary>
    /// <exception cref="ODataFilterParseException">Thrown when the OData expression cannot be parsed.</exception>
    public static string? ToGridifyFilter(string? odataFilter)
    {
        if (string.IsNullOrWhiteSpace(odataFilter))
        {
            return null;
        }

        var queryOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Constants.ODataOperations.Filter] = odataFilter 
        };

        var parser = new ODataQueryOptionParser(SharedState.Model, SharedState.EntityType, SharedState.EntitySet, queryOptions)
            {
                Resolver =
                {
                    EnableCaseInsensitive = true,
                    EnableNoDollarQueryOptions = true
                }
            };

        FilterClause clause;
        try
        {
            clause = parser.ParseFilter();
        }
        catch (ODataException ex)
        {
            throw new ODataFilterParseException($"Failed to parse OData filter '{odataFilter}': {ex.Message}", ex);
        }

        if (clause is null)
        {
            return null;
        }

        var sb = new StringBuilder();
        WriteNode(clause.Expression, sb);
        return sb.ToString();
    }

    /// <summary>
    /// Converts an OData <c>$orderby</c> expression string to a Gridify ordering string.
    /// Returns <see langword="null"/> when <paramref name="odataOrderBy"/> is null or whitespace.
    /// </summary>
    /// <exception cref="ODataFilterParseException">Thrown when the OData expression cannot be parsed.</exception>
    public static string? ToGridifyOrdering(string? odataOrderBy)
    {
        if (string.IsNullOrWhiteSpace(odataOrderBy))
        {
            return null;
        }

        var queryOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Constants.ODataOperations.OrderBy] = odataOrderBy
        };

        var parser = new ODataQueryOptionParser(SharedState.Model, SharedState.EntityType, SharedState.EntitySet, queryOptions)
            {
                Resolver =
                {
                    EnableCaseInsensitive = true,
                    EnableNoDollarQueryOptions = true
                }
            };

        OrderByClause? clause;
        try
        {
            clause = parser.ParseOrderBy();
        }
        catch (ODataException ex)
        {
            throw new ODataFilterParseException($"Failed to parse OData orderby '{odataOrderBy}': {ex.Message}", ex);
        }

        if (clause is null)
        {
            return null;
        }

        var parts = new List<string>();
        while (clause is not null)
        {
            var name = GetPropertyName(clause.Expression);
            if (name is not null)
            {
                var dir = clause.Direction == OrderByDirection.Ascending ? "asc" : "desc";
                parts.Add($"{name} {dir}");
            }

            clause = clause.ThenBy;
        }

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static void WriteNode(SingleValueNode node, StringBuilder sb)
    {
        switch (node)
        {
            case BinaryOperatorNode bin:
                WriteBinary(bin, sb);
                break;
            case UnaryOperatorNode unary when unary.OperatorKind == UnaryOperatorKind.Not:
                sb.Append('!');
                sb.Append('(');
                WriteNode(unary.Operand, sb);
                sb.Append(')');
                break;
            case SingleValueFunctionCallNode fn:
                WriteFunction(fn, sb);
                break;
            default:
                throw new NotSupportedException($"OData filter node type {node.GetType().Name} is not supported by the EF Core adapter.");
        }
    }

    private static void WriteBinary(BinaryOperatorNode node, StringBuilder sb)
    {
        var left = Unwrap(node.Left);
        var right = Unwrap(node.Right);

        switch (node.OperatorKind)
        {
            case BinaryOperatorKind.And:
                WriteNode(left, sb);
                sb.Append(", ");
                WriteNode(right, sb);
                break;

            case BinaryOperatorKind.Or:
                WriteNode(left, sb);
                sb.Append(" | ");
                WriteNode(right, sb);
                break;

            default:
                var column = GetPropertyName(left) ?? throw new NotSupportedException("Left side of comparison must be a property access.");
                var op = GetGridifyOperator(node.OperatorKind);
                var value = GetConstantValue(right);
                sb.Append(column);
                sb.Append(op);
                sb.Append(EscapeGridifyValue(value));
                break;
        }
    }

    private static void WriteFunction(SingleValueFunctionCallNode fn, StringBuilder sb)
    {
        var parameters = System.Linq.Enumerable.ToArray(fn.Parameters);
        var column = GetPropertyName(parameters[0]) ?? throw new NotSupportedException("First function argument must be a property access.");
        var value = GetConstantValue(parameters[1]);

        switch (fn.Name.ToLowerInvariant())
        {
            case Constants.ODataOperations.ComparisonOperator.Contains:
                sb.Append(column).Append("=*").Append(EscapeGridifyValue(value));
                break;
            case Constants.ODataOperations.ComparisonOperator.StartsWith:
                sb.Append(column).Append("^=").Append(EscapeGridifyValue(value));
                break;
            case Constants.ODataOperations.ComparisonOperator.EndsWith:
                sb.Append(column).Append("$=").Append(EscapeGridifyValue(value));
                break;
            default:
                throw new NotSupportedException($"OData function '{fn.Name}' is not supported by the EF Core adapter.");
        }
    }

    private static string GetGridifyOperator(BinaryOperatorKind kind)
    {
        return kind switch
        {
            BinaryOperatorKind.Equal => Constants.ODataOperations.BinaryOperator.Equal,
            BinaryOperatorKind.NotEqual => Constants.ODataOperations.BinaryOperator.NotEqual,
            BinaryOperatorKind.GreaterThan => Constants.ODataOperations.BinaryOperator.GreaterThan,
            BinaryOperatorKind.GreaterThanOrEqual => Constants.ODataOperations.BinaryOperator.GreaterThanOrEqual,
            BinaryOperatorKind.LessThan => Constants.ODataOperations.BinaryOperator.LessThan,
            BinaryOperatorKind.LessThanOrEqual => Constants.ODataOperations.BinaryOperator.LessThanOrEqual,
            _ => throw new NotSupportedException($"Binary operator {kind:g} is not supported in Gridify filter expressions.")
        };
    }

    private static string? GetPropertyName(QueryNode node)
    {
        var unwrapped = node.Kind == QueryNodeKind.Convert ? (node as ConvertNode)!.Source : node;

        return unwrapped switch
        {
            SingleValuePropertyAccessNode prop => prop.Property.Name,
            SingleValueOpenPropertyAccessNode open => open.Name,
            _ => null
        };
    }

    private static string GetConstantValue(QueryNode node)
    {
        if (node.Kind == QueryNodeKind.Convert)
        {
            return GetConstantValue((node as ConvertNode)!.Source);
        }

        if (node is ConstantNode constant)
        {
            return Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        }

        throw new NotSupportedException($"Right-hand side node type {node.GetType().Name} is not supported.");
    }

    private static SingleValueNode Unwrap(SingleValueNode node)
    {
        return node.Kind == QueryNodeKind.Convert ? (node as ConvertNode)!.Source : node;
    }

    private static string EscapeGridifyValue(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
