// Absorbed from: https://github.com/DynamicODataToSQL/DynamicODataToSQL
// Original license: MIT
// Modifications: see git history from this commit onward.
// Changes include: target framework upgrade, SqlKata 4.x compatibility, namespace moved to ODataFilter.Core.Internal.
// Fix #41: documented as thread-safe singleton.
#nullable disable
namespace ODataFilter.Core.Internal.DynamicODataToSql;

using System;

using Microsoft.OData.Edm;

/// <summary>
/// Builds an open EDM entity model for a given table name so the OData URI parser
/// can operate without a schema registration step.
/// </summary>
/// <remarks>Thread-safe. Register as singleton in DI.</remarks>
internal class EdmModelBuilder : IEdmModelBuilder
{
    private const string DEFAULTNAMESPACE = "ODataToSqlConverter";

    /// <inheritdoc/>
    public (IEdmModel, IEdmEntityType, IEdmEntitySet) BuildTableModel(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentNullException(nameof(tableName));
        }

        var model = new EdmModel();
        var entityType = new EdmEntityType(DEFAULTNAMESPACE, tableName, null, false, true);
        AddProperties(entityType);
        model.AddElement(entityType);

        var defaultContainer = new EdmEntityContainer(DEFAULTNAMESPACE, "DefaultContainer");
        model.AddElement(defaultContainer);
        var entitySet = defaultContainer.AddEntitySet(tableName, entityType);

        return (model, entityType, entitySet);
    }

    protected virtual void AddProperties(EdmEntityType entityType)
    {
    }
}
