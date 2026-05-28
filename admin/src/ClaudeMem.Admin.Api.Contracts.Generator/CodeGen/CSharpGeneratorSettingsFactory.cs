using System.Linq;
using ClaudeMem.Admin.CodeGen.NameGenerators;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;

namespace ClaudeMem.Admin.CodeGen;

public class CSharpGeneratorSettingsFactory
{
    protected ITypesInclusionFactory TypesInclusionFactory { get; }

    public CSharpGeneratorSettingsFactory(ITypesInclusionFactory typesInclusionFactory)
    {
        TypesInclusionFactory = typesInclusionFactory;
    }

    protected void ApplyBaseCSharpGeneratorSettings(CSharpGeneratorSettings settings, BaseGeneratorOptions options, OpenApiDocument apiDocument, bool useSystemTextJson = true)
    {
        SerializerIndependent(settings, options.Namespace);
        if (useSystemTextJson)
        {
            SystemTextJsonDependent(settings);
        }
        else
        {
            NewtonsoftJsonDependent(settings);
        }
    }

    private void SerializerIndependent(CSharpGeneratorSettings settings, string @namespace)
    {
        settings.ArrayType = "IEnumerable";
        settings.ClassStyle = CSharpClassStyle.Record;
        settings.ExcludedTypeNames = TypesInclusionFactory.GetExcludedTypeNames().ToArray();
        settings.GenerateDataAnnotations = true;
        settings.GenerateImmutableArrayProperties = true;
        settings.GenerateNativeRecords = true;
        settings.GenerateNullableReferenceTypes = true;
        settings.GenerateOptionalPropertiesAsNullable = true;
        settings.Namespace = @namespace;
        settings.RequiredPropertiesMustBeDefined = true;
        settings.SchemaType = SchemaType.OpenApi3;
        settings.TypeNameGenerator = new DefaultTypeNameGenerator();
        settings.PropertyNameGenerator = new PropertyNameGenerator();
    }

    private static void SystemTextJsonDependent(CSharpGeneratorSettings settings)
    {
        settings.JsonLibrary = CSharpJsonLibrary.SystemTextJson;
    }

    private static void NewtonsoftJsonDependent(CSharpGeneratorSettings settings)
    {
        settings.JsonLibrary = CSharpJsonLibrary.NewtonsoftJson;
        const string converter = "Microsoft.AspNetCore.Mvc.NewtonsoftJson.ProblemDetailsConverter";
        settings.JsonConverters = settings.JsonConverters?.Length > 0
            ? [.. settings.JsonConverters, converter]
            : [converter];
    }
}
