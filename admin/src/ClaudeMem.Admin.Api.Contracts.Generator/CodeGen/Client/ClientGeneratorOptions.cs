using ClaudeMem.Admin.CodeGen.Document;
using ClaudeMem.Admin.CodeGen.JsonSchemaRefResolvers;
using ClaudeMem.Admin.CodeGen.NameGenerators;

namespace ClaudeMem.Admin.CodeGen.Client;

public class ClientGeneratorOptions : BaseGeneratorOptions
{
    public ClientGeneratorOptions(string @namespace, string inputSpecificationFileName, string outputFilePath = "",
        string[]? additionalNamespaceUsages = null, bool useSystemTextJson = true,
        ISchemaRemoteRefResolver? schemaRemoteRefResolver = null,
        IOpenApiDocumentLoader? openApiDocumentLoader = null,
        ITypesInclusionFactory? typesInclusionFactory = null,
        OperationNameGenerator operationNameGenerator = OperationNameGenerator.SingleControllerWithPathSegmentsMethods)
        : base(@namespace, inputSpecificationFileName, outputFilePath, additionalNamespaceUsages, useSystemTextJson,
            schemaRemoteRefResolver, openApiDocumentLoader,
            typesInclusionFactory ?? new TypesInclusionFactory(),
            operationNameGenerator)
    {
    }
}
