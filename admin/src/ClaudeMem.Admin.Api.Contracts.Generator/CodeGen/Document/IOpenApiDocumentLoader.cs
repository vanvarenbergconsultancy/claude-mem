using System.Threading;
using System.Threading.Tasks;
using NSwag;

namespace ClaudeMem.Admin.CodeGen.Document;

/// <summary>Loads and parses an OpenAPI document from the file system.</summary>
public interface IOpenApiDocumentLoader
{
    /// <summary>Reads and deserialises the OpenAPI spec at the given path, bundling remote <c>$ref</c>s as needed.</summary>
    Task<OpenApiDocument> LoadOpenApiDocument(string openApiFilePath, CancellationToken cancellationToken);
}
