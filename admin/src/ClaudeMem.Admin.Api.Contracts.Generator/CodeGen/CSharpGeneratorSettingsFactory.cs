using System.Linq;
using ClaudeMem.Admin.CodeGen.NameGenerators;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen;

/// <summary>Base factory for configuring NSwag <see cref="CSharpGeneratorSettings"/>. Subclasses apply generator-specific settings.</summary>
public class CSharpGeneratorSettingsFactory
{
    /// <summary>The types-inclusion factory used to populate namespace usages and excluded types.</summary>
    protected ITypesInclusionFactory TypesInclusionFactory { get; }

    public CSharpGeneratorSettingsFactory(ITypesInclusionFactory typesInclusionFactory)
    {
        TypesInclusionFactory = typesInclusionFactory;
    }

    /// <summary>Applies common C# generator settings (namespace, class style, serialiser) from the given options.</summary>
    protected void ApplyBaseCSharpGeneratorSettings(CSharpGeneratorSettings settings, BaseGeneratorOptions options, bool useSystemTextJson = true)
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
        var excludedTypeNames = TypesInclusionFactory.GetExcludedTypeNames().ToArray();
        settings.ExcludedTypeNames = excludedTypeNames;
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
