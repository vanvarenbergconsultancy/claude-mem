using System;
using System.Collections.Generic;
using System.Linq;

namespace ClaudeMem.Admin.CodeGen;

public interface ITypesInclusionFactory
{
    IReadOnlyList<string> GetExcludedTypeNames();

    IReadOnlyList<string> GetIncludedTypeNamespaces();

    public IReadOnlyList<string> GetUniqueListForUsingStatements(params string[] additionalNamespaces)
    {
        var uniqueNamespacesOfIncludedTypesAndExtraNamespaces = GetIncludedTypeNamespaces()
            .Concat(additionalNamespaces)
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return uniqueNamespacesOfIncludedTypesAndExtraNamespaces;
    }
}
