using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

internal interface ICursorStore
{
    Task<string> StoreAsync(CursorPayload payload, CancellationToken cancellationToken = default);

    /// <returns>Null if the token is not found or has expired.</returns>
    Task<CursorPayload?> RetrieveAsync(string token, CancellationToken cancellationToken = default);
}
