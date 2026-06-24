namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>Selects the NSwag operation-name generator strategy used when generating client code.</summary>
public enum OperationNameGenerator
{
    /// <summary>Single client class; method names derived from URL path segments.</summary>
    SingleControllerWithPathSegmentsMethods = 1,

    /// <summary>One client class per OpenAPI tag; method names derived from path segments.</summary>
    MultipleControllersByTagWithPathSegmentsMethods = 5,

    /// <summary>One client class per URL path segment; method names taken from the operation's <c>operationId</c>.</summary>
    MultipleControllersByPathSegmentsWithOperationNameMethods = 7
}
