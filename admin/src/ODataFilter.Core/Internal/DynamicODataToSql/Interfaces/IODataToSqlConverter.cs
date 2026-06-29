// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
#nullable disable
namespace ODataFilter.Core.Internal.DynamicODataToSql;

using System.Collections.Generic;

using SqlKata;

internal interface IODataToSqlConverter
{
    (string, IDictionary<string, object>) ConvertToSQL(
        string tableName,
        IDictionary<string, string> odataQuery,
        bool count = false,
        bool tryToParseDates = true);

    Query ConvertToSQLKataQuery(
        string tableName,
        IDictionary<string, string> odataQuery,
        bool count = false,
        bool tryToParseDates = true);

    (string, IDictionary<string, object>) ConvertToSqlFromRawSql(
        string rawSql,
        IDictionary<string, string> odataQuery,
        bool count = false,
        bool tryToParseDates = true);

    Query ConvertToSQLKataQueryFromRawSql(
        string rawSql,
        IDictionary<string, string> odataQuery,
        bool count = false,
        bool tryToParseDates = true);
}
