using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ODataFilter.Core.DynamicODataToSql;
using SqlKata;
using SqlKata.Compilers;

namespace ODataFilter.Core;

/// <summary>Translates <see cref="ODataQueryOptions"/> into parameterised SQL using SqlKata + an absorbed DynamicODataToSQL core.</summary>
/// <remarks>Thread-safe. Register as singleton in DI via <see cref="ODataFilterServiceCollectionExtensions"/>.</remarks>
public sealed class ODataSqlTranslator : IODataSqlTranslator
{
    private readonly ODataToSqlConverter _converter;

    /// <summary> Initializes a new instance of the <see cref="ODataSqlTranslator"/> class. </summary>
    /// <param name="sqlCompiler"> SqlKata compiler for the target database dialect (e.g. <c>new PostgresCompiler()</c> or <c>new SqlServerCompiler()</c>). </param>
    public ODataSqlTranslator(Compiler sqlCompiler)
    {
        ArgumentNullException.ThrowIfNull(sqlCompiler);
        _converter = new ODataToSqlConverter(new EdmModelBuilder(), sqlCompiler);
    }

    /// <inheritdoc/>
    public ODataSqlResult Translate(string tableName, ODataQueryOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(options);

        var odataQuery = BuildODataQueryDictionary(options);
        var (sql, namedBindings) = _converter.ConvertToSql(tableName, odataQuery);

        var nameBindingsDict = new Dictionary<string, object>(namedBindings, StringComparer.Ordinal);
        var readOnlyDict = new ReadOnlyDictionary<string, object>(nameBindingsDict);
        
        return new ODataSqlResult(sql, readOnlyDict);
    }

    /// <inheritdoc/>
    public Query TranslateToQuery(string tableName, ODataQueryOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(options);

        var odataQuery = BuildODataQueryDictionary(options);
        
        return _converter.ConvertToSqlKataQuery(tableName, odataQuery);
    }

    private static Dictionary<string, string> BuildODataQueryDictionary(ODataQueryOptions options)
    {
        var dict = new Dictionary<string, string>(4, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(options.Filter))
        {
            dict[Constants.ODataOperations.Filter] = options.Filter;
        }

        if (!string.IsNullOrEmpty(options.OrderBy))
        {
            dict[Constants.ODataOperations.OrderBy] = options.OrderBy;
        }

        if (options.Top.HasValue)
        {
            dict[Constants.ODataOperations.Top] = options.Top.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return dict;
    }
}
