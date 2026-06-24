using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>
/// <see cref="ICursorStore"/> backed by <see cref="IDistributedCache"/>.
/// Works with any distributed cache backend — Redis, SQL Server, NCache, etc.
/// TTL eviction is handled by the backing store; no manual cleanup is required.
/// </summary>
/// <remarks>
/// The caller is responsible for registering an <see cref="IDistributedCache"/> implementation (e.g. <c>AddStackExchangeRedisCache</c> or <c>AddDistributedMemoryCache</c>)
/// before calling <c>AddDistributedCacheCursorStore</c>.
/// <para>
/// Payloads are serialised to UTF-8 JSON for storage. <see cref="IDistributedCache.GetAsync"/>
/// returns <c>null</c> on a miss, which maps directly to the <see cref="ICursorStore.Retrieve"/> contract without extra workarounds.
/// </para>
/// </remarks>
internal sealed class DistributedCacheCursorStore : ICursorStore
{
    private readonly IDistributedCache _cache;
    private readonly ServerStoredCursorOptions _options;

    public DistributedCacheCursorStore(IDistributedCache cache, ServerStoredCursorOptions options)
    {
        _cache = cache;
        _options = options;
    }

    public async Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var newOpaqueToken = Guid.NewGuid().ToString("N");
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        
        await _cache.SetAsync(newOpaqueToken, bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.TokenExpiry,
        }, cancellationToken);
        
        return newOpaqueToken;
    }

    public async Task<CursorPayload?> Retrieve(string opaqueToken, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(opaqueToken, cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<CursorPayload>(bytes);
    }
}
