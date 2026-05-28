using ClaudeMem.Admin.CodeGen.Document;
using ClaudeMem.Admin.CodeGen.JsonSchemaRefResolvers;
using ClaudeMem.Admin.CodeGen.NameGenerators;

namespace ClaudeMem.Admin.CodeGen;

public class BaseGeneratorOptions
{
    private string _outputFilePath;

    public BaseGeneratorOptions(string @namespace, string inputSpecificationFileName, string outputFilePath = "",
        string[]? additionalNamespaceUsages = null,
        bool useSystemTextJson = true, ISchemaRemoteRefResolver? schemaRemoteRefResolver = null,
        IOpenApiDocumentLoader? openApiDocumentLoader = null, ITypesInclusionFactory? typesInclusionFactory = null,
        OperationNameGenerator operationNameGenerator = OperationNameGenerator.MultipleControllersByPathSegmentsWithOperationNameMethods)
    {
        Namespace = @namespace;
        InputSpecificationFileName = inputSpecificationFileName;
        _outputFilePath = outputFilePath;
        AdditionalNamespaceUsages = additionalNamespaceUsages ?? [];
        UseSystemTextJson = useSystemTextJson;
        SchemaRemoteRefResolver = schemaRemoteRefResolver ?? new NullableSchemaRemoteRefResolver();
        OpenApiDocumentLoader = openApiDocumentLoader ?? new OpenApiDocumentLoader(SchemaRemoteRefResolver);
        TypesInclusionFactory = typesInclusionFactory ?? new TypesInclusionFactory();
        OperationNameGenerator = operationNameGenerator;
    }

    public string Namespace { get; init; }
    public string InputSpecificationFileName { get; init; }

    public string OutputFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_outputFilePath))
            {
                _outputFilePath = DefaultFilePathBasedOnOtherOptions();
            }

            return _outputFilePath;
        }
    }

    public string[] AdditionalNamespaceUsages { get; init; }
    public bool UseSystemTextJson { get; init; }
    public ISchemaRemoteRefResolver SchemaRemoteRefResolver { get; init; }
    public IOpenApiDocumentLoader OpenApiDocumentLoader { get; init; }
    public ITypesInclusionFactory TypesInclusionFactory { get; init; }
    public OperationNameGenerator OperationNameGenerator { get; init; }

    protected virtual string DefaultFilePathBasedOnOtherOptions() => _outputFilePath;
}
