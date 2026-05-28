using System.Threading;
using System.Threading.Tasks;
using NSwag;

namespace ClaudeMem.Admin.CodeGen.Document;

public interface IOpenApiDocumentLoader
{
    Task<OpenApiDocument> LoadOpenApiDocument(string openApiFilePath, CancellationToken cancellationToken);
}
