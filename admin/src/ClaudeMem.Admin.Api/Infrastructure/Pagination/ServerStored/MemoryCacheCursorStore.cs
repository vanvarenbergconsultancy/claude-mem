using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>
/// <see cref="ICursorStore"/> backed by <see cref="IMemoryCache"/>.
/// TTL eviction is handled automatically by the cache; no manual cleanup is required.
/// </summary>
/// <remarks>
/// Suitable for single-instance deployments only — the in-process cache is not shared across multiple hosts.
/// For multi-instance deployments use <see cref="DistributedCacheCursorStore"/> or a custom <see cref="ICursorStore"/> backed by shared storage.
/// </remarks>
internal sealed class MemoryCacheCursorStore : ICursorStore
{
    private readonly IMemoryCache _cache;
    private readonly ServerStoredCursorOptions _options;

    public MemoryCacheCursorStore(IMemoryCache cache, ServerStoredCursorOptions options)
    {
        _cache = cache;
        _options = options;
    }

    public Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var newOpaqueToken = Guid.NewGuid().ToString("N");
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.TokenExpiry,
        };

        _cache.Set(newOpaqueToken, payload, cacheOptions);

        return Task.FromResult(newOpaqueToken);
    }

    public Task<CursorPayload?> Retrieve(string opaqueToken, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(opaqueToken, out CursorPayload? payload);

        return Task.FromResult(payload);
    }
}
