using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>Single-client strategy that derives the client name from the OpenAPI document's info title.</summary>
public class OperationNameGeneratorSingleControllerWithPathSegmentsMethods : SingleClientFromPathSegmentsOperationNameGenerator
{
    /// <inheritdoc/>
    public override string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
        => NameSanitizer.SanitizeApiTitle(document);
}
