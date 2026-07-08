
using System.Collections.Generic;

namespace ODataFilter.Core;
/// <summary>The SQL string and named parameter bindings produced by <see cref="IODataSqlTranslator"/>.</summary>
/// <param name="Sql">Parameterised SQL query string.</param>
/// <param name="Parameters">Named bindings — pass directly to <c>NpgsqlCommand.Parameters</c> or Dapper.</param>
public sealed record ODataSqlResult(
    string Sql,
    IReadOnlyDictionary<string, object> Parameters);
