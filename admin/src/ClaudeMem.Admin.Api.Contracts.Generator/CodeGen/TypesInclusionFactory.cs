using System.Collections.Generic;

namespace ClaudeMem.Admin.CodeGen;

/// <summary>Default <see cref="ITypesInclusionFactory"/> configured for the ClaudeMem Admin API contracts namespace.</summary>
public class TypesInclusionFactory : ITypesInclusionFactory
{
    /// <summary>The root namespace shared by all generated contract types.</summary>
    public static readonly string ContractsNamespace = "ClaudeMem.Admin.Api.Contracts";

    protected static readonly IReadOnlyList<string> ExcludedTypeNames =
    [
        "Microsoft.AspNetCore.Mvc.ProblemDetails",
        "ProblemDetails"
    ];

    protected static readonly IReadOnlyList<string> IncludedTypeNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Net",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
        ContractsNamespace
    ];

    public virtual IReadOnlyList<string> GetExcludedTypeNames() => ExcludedTypeNames;

    public virtual IReadOnlyList<string> GetIncludedTypeNamespaces() => IncludedTypeNamespaces;
}
