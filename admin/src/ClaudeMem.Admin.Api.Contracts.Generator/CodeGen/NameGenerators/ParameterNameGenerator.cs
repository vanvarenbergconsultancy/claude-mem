using System.Collections.Generic;
using NSwag;
using NSwag.CodeGeneration;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>NSwag parameter-name generator that sanitises names to valid C# identifiers.</summary>
public class ParameterNameGenerator : IParameterNameGenerator
{
    private readonly DefaultParameterNameGenerator _generator = new();

    /// <inheritdoc/>
    public string Generate(OpenApiParameter parameter, IEnumerable<OpenApiParameter> allParameters)
    {
        var name = _generator.Generate(parameter, allParameters);
        return NameSanitizer.MakeSafeParameterName(name);
    }
}
