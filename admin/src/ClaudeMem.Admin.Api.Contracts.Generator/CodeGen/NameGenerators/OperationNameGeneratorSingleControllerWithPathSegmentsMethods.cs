using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

public class OperationNameGeneratorSingleControllerWithPathSegmentsMethods : SingleClientFromPathSegmentsOperationNameGenerator
{
    public override string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
        => NameSanitizer.SanitizeApiTitle(document);
}
