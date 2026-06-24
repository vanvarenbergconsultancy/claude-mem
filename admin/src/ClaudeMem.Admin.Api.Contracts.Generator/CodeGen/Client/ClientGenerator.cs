using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen.Client;

/// <summary>Generates a C# typed HTTP client from an OpenAPI spec using NSwag.</summary>
public class ClientGenerator : GeneratorBase
{
    private readonly ClientGeneratorOptions _clientGeneratorOptions;

    public ClientGenerator(ClientGeneratorOptions generatorOptions) : base(generatorOptions)
    {
        _clientGeneratorOptions = generatorOptions;
    }

    /// <inheritdoc/>
    protected override CSharpGeneratorBase CreateGenerator(OpenApiDocument openApiDocument)
    {
        var factory = new CSharpClientGeneratorSettingsFactory(_clientGeneratorOptions.TypesInclusionFactory);
        var settings = factory.Default(_clientGeneratorOptions, openApiDocument);
        return new CSharpClientGenerator(openApiDocument, settings);
    }
}
