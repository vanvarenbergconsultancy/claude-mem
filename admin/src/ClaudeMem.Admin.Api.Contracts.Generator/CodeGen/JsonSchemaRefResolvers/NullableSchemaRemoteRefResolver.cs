using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.CodeGen.JsonSchemaRefResolvers;

/// <summary>
/// No-op resolver that passes the file path through unchanged.
/// Used when the spec has no external $ref links that need bundling.
/// </summary>
public sealed class NullableSchemaRemoteRefResolver : ISchemaRemoteRefResolver
{
    public Task<string> Bundle(string filePath, CancellationToken cancellationToken)
        => Task.FromResult(filePath);

    public Task<string> Resolve(string filePath, CancellationToken cancellationToken)
        => Task.FromResult(filePath);
}
