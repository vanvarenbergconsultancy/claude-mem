using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>
/// <see cref="ICursorStore"/> backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// No external dependencies; state is lost on process restart.
/// </summary>
/// <remarks>
/// Expiry is checked lazily on <see cref="Retrieve"/> and proactively on each <see cref="PurgeExpired"/> tick fired by <see cref="CursorStoreCleanupService"/>.
/// Token is deleted after retrieval to enforce single-use, but expired tokens are purged only on cleanup ticks to avoid unnecessary store churn.
/// <para>
/// Suitable for single-instance deployments only.
/// For multi-instance or durable scenarios use <see cref="MemoryCacheCursorStore"/> or <see cref="DistributedCacheCursorStore"/>.
/// </para>
/// </remarks>
internal sealed class InMemoryCursorStore : ICursorStore, ISupportsCursorPurge
{
    private readonly ConcurrentDictionary<string, (CursorPayload Payload, DateTimeOffset Expiry)> _store = new(StringComparer.Ordinal);
    private readonly ServerStoredCursorOptions _options;

    public InMemoryCursorStore(ServerStoredCursorOptions options)
    {
        _options = options;
    }

    public Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var newOpaqueToken = Guid.NewGuid().ToString("N");
        var expiryDateForToken = DateTimeOffset.UtcNow.Add(_options.TokenExpiry);
        _store[newOpaqueToken] = (payload, expiryDateForToken);

        return Task.FromResult(newOpaqueToken);
    }

    public Task<CursorPayload?> Retrieve(string opaqueToken, CancellationToken cancellationToken = default)
    {
        var exists = _store.TryGetValue(opaqueToken, out var cursorWithExpiryFromStore);
        if (!exists)
        {
            return Task.FromResult<CursorPayload?>(null);
        }

        var originalPayloadForOpaqueToken = cursorWithExpiryFromStore.Payload;

        _store.TryRemove(opaqueToken, out _);

        var isTokenExpired = cursorWithExpiryFromStore.Expiry <= DateTimeOffset.UtcNow;
        if (isTokenExpired)
        {
            return Task.FromResult<CursorPayload?>(null);
        }

        return Task.FromResult<CursorPayload?>(originalPayloadForOpaqueToken);
    }

    public Task PurgeExpired(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _store)
        {
            if (kvp.Value.Expiry <= now)
            {
                _store.TryRemove(kvp.Key, out _);
            }
        }

        return Task.CompletedTask;
    }
}
