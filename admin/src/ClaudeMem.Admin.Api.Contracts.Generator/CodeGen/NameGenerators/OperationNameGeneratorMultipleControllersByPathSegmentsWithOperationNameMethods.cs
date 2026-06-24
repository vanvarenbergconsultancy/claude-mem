using System.Linq;
using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>Groups operations into clients by the first non-parameter path segment (e.g. <c>/teams/{id}</c> → <c>TeamsClient</c>).</summary>
public class OperationNameGeneratorMultipleControllersByPathSegmentsWithOperationNameMethods : MultipleClientsFromPathSegmentsOperationNameGenerator
{
    /// <inheritdoc/>
    public override string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        var pathSegments = path.Split('/');
        var nonParameterSegments = pathSegments.Where(p => !p.Contains('{') && !string.IsNullOrWhiteSpace(p));
        return nonParameterSegments.FirstOrDefault() ?? NameSanitizer.SanitizeApiTitle(document);
    }
}
