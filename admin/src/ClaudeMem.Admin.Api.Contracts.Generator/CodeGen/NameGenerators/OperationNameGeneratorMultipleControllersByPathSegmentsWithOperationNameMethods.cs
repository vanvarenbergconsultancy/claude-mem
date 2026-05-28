using System.Linq;
using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

public class OperationNameGeneratorMultipleControllersByPathSegmentsWithOperationNameMethods : MultipleClientsFromPathSegmentsOperationNameGenerator
{
    public override string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        var pathSegments = path.Split('/');
        var nonParameterSegments = pathSegments.Where(p => !p.Contains('{') && !string.IsNullOrWhiteSpace(p));
        return nonParameterSegments.FirstOrDefault() ?? NameSanitizer.SanitizeApiTitle(document);
    }
}
