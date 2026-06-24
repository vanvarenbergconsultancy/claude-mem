using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>
/// Backing storage for the server-stored cursor strategy.
/// Implementations persist cursor payloads server-side and hand callers an opaque token — the payload never travels to the client.
/// </summary>
/// <remarks>
/// Three built-in implementations are provided:
/// <list type="bullet">
///   <item><description><c>InMemoryCursorStore</c> — <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, no external dependencies, lost on restart.</description></item>
///   <item><description><c>MemoryCacheCursorStore</c> — <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>, automatic TTL eviction.</description></item>
///   <item><description><c>DistributedCacheCursorStore</c> — <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>, works with Redis, SQL, etc.</description></item>
/// </list>
/// </remarks>
internal interface ICursorStore
{
    /// <summary>Persists <paramref name="payload"/> and returns an opaque token that can retrieve it later.</summary>
    Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the payload associated with <paramref name="opaqueToken"/>.</summary>
    /// <returns><c>null</c> if the token is not found or has expired.</returns>
    Task<CursorPayload?> Retrieve(string opaqueToken, CancellationToken cancellationToken = default);
}
