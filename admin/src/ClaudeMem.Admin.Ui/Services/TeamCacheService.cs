using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClaudeMem.Admin.Ui.Services;

internal sealed partial class TeamCacheService : ITeamCacheService
{
    private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(2);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICacheInvalidationBus _bus;
    private readonly ILogger<TeamCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    public TeamCacheService(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        ICacheInvalidationBus bus,
        ILogger<TeamCacheService> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<Team>> GetAll(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKeys.TeamsAll, out IReadOnlyList<Team>? cached) && cached is not null)
        {
            return cached;
        }

        var semaphore = _locks.GetOrAdd(CacheKeys.TeamsAll, static _ => new SemaphoreSlim(1, 1));
        return await FetchUnderLockAsync(semaphore, cancellationToken).ConfigureAwait(false);
    }

    public void Invalidate(CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKeys.TeamsAll);
        LogTeamsCacheInvalidated(_logger);
        _ = _bus.Notify(CacheKeys.TeamsAll, cancellationToken);
    }

    private async Task<IReadOnlyList<Team>> FetchUnderLockAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check: another waiter may have populated the cache while we were queued.
            if (_cache.TryGetValue(CacheKeys.TeamsAll, out IReadOnlyList<Team>? doubleChecked) && doubleChecked is not null)
            {
                return doubleChecked;
            }

            return await FetchFromApiAndCacheAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<IReadOnlyList<Team>> FetchFromApiAndCacheAsync(CancellationToken cancellationToken)
    {
        LogTeamsCacheMiss(_logger);

        // ITeamsClient is registered as a transient typed HTTP client. Resolving it via a scope avoids the captive-dependency problem (Singleton holding a transient directly).
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<ITeamsClient>();
        var page = await client.TeamsGetAsync(pageSize: 100, cancellationToken: cancellationToken).ConfigureAwait(false);
        var result = page.Items.OrderBy(t => t.Name, StringComparer.Ordinal).ToList() ?? [];

        _cache.Set<IReadOnlyList<Team>>(CacheKeys.TeamsAll, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteTtl,
            SlidingExpiration = SlidingTtl
        });

        LogTeamsCachePopulated(_logger, result.Count);

        return result;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Teams cache miss — fetching from API")]
    private static partial void LogTeamsCacheMiss(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Teams cache populated with {Count} entries")]
    private static partial void LogTeamsCachePopulated(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Teams cache invalidated")]
    private static partial void LogTeamsCacheInvalidated(ILogger logger);
}
