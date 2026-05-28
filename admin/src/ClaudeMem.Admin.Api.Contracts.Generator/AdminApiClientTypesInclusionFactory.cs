using ClaudeMem.Admin.CodeGen;

namespace ClaudeMem.Admin.Api.Contracts.Generator;

public class AdminApiClientTypesInclusionFactory : TypesInclusionFactory
{
    // Base TypesInclusionFactory already excludes ProblemDetails and includes
    // the ClaudeMem.Admin.Api.Contracts namespace — nothing extra required here.
}
