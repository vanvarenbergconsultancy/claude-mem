// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
using Microsoft.OData.Edm;

namespace ODataFilter.Core.DynamicODataToSql;

internal interface IEdmModelBuilder
{
    (IEdmModel, IEdmEntityType, IEdmEntitySet) BuildTableModel(string tableName);
}
