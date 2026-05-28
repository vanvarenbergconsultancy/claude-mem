using System.Collections.Generic;

namespace ClaudeMem.Admin.CodeGen;

public class TypesInclusionFactory : ITypesInclusionFactory
{
    public static readonly string ContractsNamespace = "ClaudeMem.Admin.Api.Contracts";

    protected static readonly List<string> ExcludedTypeNames =
    [
        "Microsoft.AspNetCore.Mvc.ProblemDetails",
        "ProblemDetails"
    ];

    protected static readonly List<string> IncludedTypeNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Net",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
        ContractsNamespace
    ];

    public virtual IList<string> GetExcludedTypeNames() => ExcludedTypeNames;

    public virtual IList<string> GetIncludedTypeNamespaces() => IncludedTypeNamespaces;
}
