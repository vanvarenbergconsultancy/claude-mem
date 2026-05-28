namespace ClaudeMem.Admin.CodeGen.Client;

/// <summary>
/// Types inclusion factory for the HTTP client generator.
/// Inherits from TypesInclusionFactory which already excludes ProblemDetails
/// and includes the ClaudeMem.Admin.Api.Contracts namespace.
/// </summary>
public class ClientTypesInclusionFactory : TypesInclusionFactory;
