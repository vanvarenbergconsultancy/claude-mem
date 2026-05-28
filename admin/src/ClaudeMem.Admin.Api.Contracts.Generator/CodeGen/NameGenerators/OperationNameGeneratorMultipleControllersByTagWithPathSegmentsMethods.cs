using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>
/// Generates one class per tag, using path segments for method names.
/// Falls back to the API title when an operation has no tag.
/// </summary>
public class OperationNameGeneratorMultipleControllersByTagWithPathSegmentsMethods : MultipleClientsFromFirstTagAndPathSegmentsOperationNameGenerator
{
    public override string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        var baseClientName = base.GetClientName(document, path, httpMethod, operation);
        return string.IsNullOrEmpty(baseClientName)
            ? NameSanitizer.SanitizeApiTitle(document)
            : baseClientName;
    }
}
