using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen.Client;

public class ClientGenerator : GeneratorBase
{
    private readonly ClientGeneratorOptions _clientGeneratorOptions;

    public ClientGenerator(ClientGeneratorOptions generatorOptions) : base(generatorOptions)
    {
        _clientGeneratorOptions = generatorOptions;
    }

    protected override CSharpGeneratorBase CreateGenerator(OpenApiDocument openApiDocument)
    {
        var factory = new CSharpClientGeneratorSettingsFactory(_clientGeneratorOptions.TypesInclusionFactory);
        var settings = factory.Default(_clientGeneratorOptions, openApiDocument);
        return new CSharpClientGenerator(openApiDocument, settings);
    }
}
