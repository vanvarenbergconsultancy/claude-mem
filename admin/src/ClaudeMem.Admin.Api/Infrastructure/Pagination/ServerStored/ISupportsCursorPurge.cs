using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>
/// Opt-in capability for <see cref="ICursorStore"/> implementations that manage their own expiry state and require periodic cleanup.
/// </summary>
/// <remarks>
/// Cache-backed stores do not implement this interface because their backing caches evict entries via TTL (Time To Live) automatically.
/// Only stores that perform their own bookkeeping need to implement it.
/// <para>
/// <c>CursorStoreCleanupService</c> resolves all <see cref="ISupportsCursorPurge"/> registrations via <c>IEnumerable&lt;ISupportsCursorPurge&gt;</c>
/// and calls <see cref="PurgeExpired"/> on each store on every scheduled tick.
/// </para>
/// </remarks>
internal interface ISupportsCursorPurge
{
    /// <summary>Removes all entries whose expiry timestamp has passed.</summary>
    Task PurgeExpired(CancellationToken cancellationToken = default);
}
