using System.Collections.Generic;
using System.Linq;

namespace ClaudeMem.Admin.CodeGen;

public interface ITypesInclusionFactory
{
    IList<string> GetExcludedTypeNames();

    IList<string> GetIncludedTypeNamespaces();

    public IList<string> GetUniqueListForUsingStatements(params string[] additionalNamespaces)
    {
        var uniqueNamespacesOfIncludedTypesAndExtraNamespaces = GetIncludedTypeNamespaces()
            .Concat(additionalNamespaces)
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Distinct()
            .ToList();

        return uniqueNamespacesOfIncludedTypesAndExtraNamespaces;
    }
}
