// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
#nullable disable
namespace ODataFilter.Core.Internal.DynamicODataToSql;

using Microsoft.OData.Edm;

internal interface IEdmModelBuilder
{
    (IEdmModel, IEdmEntityType, IEdmEntitySet) BuildTableModel(string tableName);
}
