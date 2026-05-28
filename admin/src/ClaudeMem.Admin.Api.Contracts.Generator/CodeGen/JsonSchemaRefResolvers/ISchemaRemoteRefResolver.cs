using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.CodeGen.JsonSchemaRefResolvers;

public interface ISchemaRemoteRefResolver
{
    Task<string> Bundle(string filePath, CancellationToken cancellationToken);
    Task<string> Resolve(string filePath, CancellationToken cancellationToken);
}
