using System.Collections.Generic;
using NSwag;
using NSwag.CodeGeneration;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

public class ParameterNameGenerator : IParameterNameGenerator
{
    private readonly DefaultParameterNameGenerator _generator = new();

    public string Generate(OpenApiParameter parameter, IEnumerable<OpenApiParameter> allParameters)
    {
        var name = _generator.Generate(parameter, allParameters);
        return NameSanitizer.MakeSafeParameterName(name);
    }
}
