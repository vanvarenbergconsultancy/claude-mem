using System.Linq;
using System.Reflection;
using ClaudeMem.Admin.CodeGen.NameGenerators;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen.Client;

/// <summary>Builds <see cref="CSharpClientGeneratorSettings"/> for generating typed HTTP client code from an OpenAPI spec.</summary>
public class CSharpClientGeneratorSettingsFactory : CSharpGeneratorSettingsFactory
{
    public CSharpClientGeneratorSettingsFactory(ITypesInclusionFactory typesInclusionFactory) : base(typesInclusionFactory)
    {
    }

    /// <summary>Returns the default settings combining client-specific and base C# settings.</summary>
    public CSharpClientGeneratorSettings Default(ClientGeneratorOptions options, OpenApiDocument apiDocument)
    {
        var settings = ClientSpecificGeneratorSettings(options);
        ApplyBaseCSharpGeneratorSettings(settings.CSharpGeneratorSettings, options);
        EnableCustomEmbeddedTemplates(settings);
        return settings;
    }

    private static CSharpClientGeneratorSettings ClientSpecificGeneratorSettings(ClientGeneratorOptions options)
    {
        var namespaceUsages = options.TypesInclusionFactory
            .GetUniqueListForUsingStatements(options.AdditionalNamespaceUsages)
            .ToArray();
        var operationNameGenerator = OperationNameGeneratorFactory.Get(options.OperationNameGenerator);

        return new CSharpClientGeneratorSettings
        {
            AdditionalNamespaceUsages = namespaceUsages,
            ClientClassAccessModifier = "public",
            DisposeHttpClient = false,
            GenerateBaseUrlProperty = false,
            GenerateClientClasses = true,
            GenerateClientInterfaces = true,
            GenerateDtoTypes = true,
            GenerateExceptionClasses = true,
            GenerateOptionalParameters = true,
            GenerateSyncMethods = false,
            InjectHttpClient = true,
            OperationNameGenerator = operationNameGenerator,
            ParameterNameGenerator = new ParameterNameGenerator(),
            UseBaseUrl = false,
            UseHttpClientCreationMethod = false,
            UseHttpRequestMessageCreationMethod = false,
            WrapDtoExceptions = false,
            WrapResponses = false,
        };
    }

    private static void EnableCustomEmbeddedTemplates(CSharpClientGeneratorSettings settings)
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        settings.CSharpGeneratorSettings.TemplateFactory =
            new CustomEmbeddedTemplateFactory(settings.CSharpGeneratorSettings, currentAssembly);
    }
}
