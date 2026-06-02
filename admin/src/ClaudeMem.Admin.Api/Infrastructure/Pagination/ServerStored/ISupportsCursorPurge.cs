using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

internal interface ISupportsCursorPurge
{
    Task PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
