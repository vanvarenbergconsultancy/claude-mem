using SqlKata;

namespace ODataFilter.Core;

/// <summary>Translates <see cref="ODataQueryOptions"/> into parameterised SQL for raw-SQL data access (Npgsql / Dapper).</summary>
/// <remarks>Thread-safe. Implementations should be registered as singletons in DI.</remarks>
public interface IODataSqlTranslator
{
    /// <summary>Translates the options into a parameterised SQL string and named bindings.</summary>
    /// <param name="tableName">Target table or view name (used as the SQL FROM clause).</param>
    /// <param name="options">Query options to translate.</param>
    ODataSqlResult Translate(string tableName, ODataQueryOptions options);

    /// <summary>Returns a SqlKata <see cref="Query"/> for further composition before compilation (e.g. adding JOINs).</summary>
    /// <param name="tableName">Target table or view name.</param>
    /// <param name="options">Query options to translate.</param>
    Query TranslateToQuery(string tableName, ODataQueryOptions options);
}
