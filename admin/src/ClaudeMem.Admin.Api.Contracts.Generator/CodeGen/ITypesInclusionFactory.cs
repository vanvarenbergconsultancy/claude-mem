using System;
using System.Collections.Generic;
using System.Linq;

namespace ClaudeMem.Admin.CodeGen;

/// <summary>Controls which types and namespaces are included or excluded from generated client code.</summary>
public interface ITypesInclusionFactory
{
    /// <summary>Returns type names that should be suppressed from the generated output (e.g. framework types already present in the target project).</summary>
    IReadOnlyList<string> GetExcludedTypeNames();

    /// <summary>Returns the namespaces that generated using-statements should reference.</summary>
    IReadOnlyList<string> GetIncludedTypeNamespaces();

    /// <summary>Returns a deduplicated list of namespaces combining <see cref="GetIncludedTypeNamespaces"/> and any caller-supplied extras.</summary>
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
