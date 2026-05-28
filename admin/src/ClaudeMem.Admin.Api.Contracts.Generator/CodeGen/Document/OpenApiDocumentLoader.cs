using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.CodeGen.JsonSchemaRefResolvers;
using NSwag;

namespace ClaudeMem.Admin.CodeGen.Document;

public class OpenApiDocumentLoader : IOpenApiDocumentLoader
{
    private readonly ISchemaRemoteRefResolver _schemaRemoteRefResolver;

    public OpenApiDocumentLoader(ISchemaRemoteRefResolver schemaRemoteRefResolver)
    {
        _schemaRemoteRefResolver = schemaRemoteRefResolver;
    }

    public async Task<OpenApiDocument> LoadOpenApiDocument(string openApiFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(openApiFilePath))
        {
            throw new ArgumentNullException(nameof(openApiFilePath));
        }

        var format = OpenApiFormatExtensions.GetFormat(openApiFilePath);
        var bundledPath = await _schemaRemoteRefResolver.Bundle(openApiFilePath, cancellationToken).ConfigureAwait(false);

        return await LoadFromFile(bundledPath, format, cancellationToken).ConfigureAwait(false);
    }

    private static Task<OpenApiDocument> LoadFromFile(string path, OpenApiFormat format, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The file '{path}' does not exist.");
        }

        return format switch
        {
            OpenApiFormat.Json => OpenApiDocument.FromFileAsync(path, cancellationToken),
            OpenApiFormat.Yaml => OpenApiYamlDocument.FromFileAsync(path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "The OpenAPI format is not supported.")
        };
    }
}
