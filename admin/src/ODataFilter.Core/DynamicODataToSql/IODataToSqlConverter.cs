// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.

using System.Collections.Generic;
using SqlKata;

namespace ODataFilter.Core.DynamicODataToSql;

internal interface IODataToSqlConverter
{
    (string, IDictionary<string, object>) ConvertToSql(string tableName, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true);

    Query ConvertToSqlKataQuery(string tableName, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true);

    (string, IDictionary<string, object>) ConvertToSqlFromRawSql(string rawSql, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true);

    Query ConvertToSqlKataQueryFromRawSql(string rawSql, IDictionary<string, string> odataQuery, bool count = false, bool tryToParseDates = true);
}
