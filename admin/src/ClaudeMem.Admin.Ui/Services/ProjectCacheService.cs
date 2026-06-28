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

internal sealed partial class ProjectCacheService : IProjectCacheService
{
    private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(2);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICacheInvalidationBus _bus;
    private readonly ILogger<ProjectCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    // Tracks which teamIds have been fetched so InvalidateAll can clear every project key without needing to scan the cache (IMemoryCache has no enumeration API).
    private readonly ConcurrentDictionary<string, byte> _knownTeamIds;

    public ProjectCacheService(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        ICacheInvalidationBus bus,
        ILogger<ProjectCacheService> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);
        _knownTeamIds = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<Project>> GetForTeam(string teamId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.ProjectsForTeam(teamId);

        if (_cache.TryGetValue(key, out IReadOnlyList<Project>? cached) && cached is not null)
        {
            return cached;
        }

        var semaphore = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

        return await FetchUnderLockAsync(teamId, key, semaphore, cancellationToken).ConfigureAwait(false);
    }

    public void InvalidateForTeam(string teamId, CancellationToken cancellationToken = default)
    {
        var projectKeyForTeam = CacheKeys.ProjectsForTeam(teamId);
        _cache.Remove(projectKeyForTeam);
        LogProjectsCacheInvalidated(_logger, teamId);
        _ = _bus.Notify(projectKeyForTeam, cancellationToken);
    }

    public void InvalidateAll(CancellationToken cancellationToken = default)
    {
        foreach (var teamId in _knownTeamIds.Keys)
        {
            var projectKeyForTeam = CacheKeys.ProjectsForTeam(teamId);
            _cache.Remove(projectKeyForTeam);
        }

        _knownTeamIds.Clear();
        LogAllProjectsCacheInvalidated(_logger);
        _ = _bus.Notify(CacheKeys.ProjectsAll, cancellationToken);
    }

    private async Task<IReadOnlyList<Project>> FetchUnderLockAsync(string teamId, string key, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check: another waiter may have populated the cache while we were queued.
            if (_cache.TryGetValue(key, out IReadOnlyList<Project>? doubleChecked) && doubleChecked is not null)
            {
                return doubleChecked;
            }

            return await FetchFromApiAndCacheAsync(teamId, key, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<IReadOnlyList<Project>> FetchFromApiAndCacheAsync(string teamId, string key, CancellationToken cancellationToken)
    {
        LogProjectsCacheMiss(_logger, teamId);

        // IProjectsClient is registered as a transient typed HTTP client. Resolving it via a scope avoids the captive-dependency problem (Singleton holding a transient directly).
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IProjectsClient>();
        var page = await client.ProjectsGetAsync(teamId, pageSize: 100, cancellationToken: cancellationToken).ConfigureAwait(false);
        var result = page.Items.OrderBy(p => p.Name, StringComparer.Ordinal).ToList() ?? [];

        _cache.Set<IReadOnlyList<Project>>(key, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteTtl,
            SlidingExpiration = SlidingTtl
        });

        _knownTeamIds.TryAdd(teamId, 0);
        LogProjectsCachePopulated(_logger, result.Count, teamId);

        return result;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Projects cache miss for team '{TeamId}' — fetching from API")]
    private static partial void LogProjectsCacheMiss(ILogger logger, string teamId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Projects cache populated with {Count} entries for team '{TeamId}'")]
    private static partial void LogProjectsCachePopulated(ILogger logger, int count, string teamId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Projects cache invalidated for team '{TeamId}'")]
    private static partial void LogProjectsCacheInvalidated(ILogger logger, string teamId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "All project cache entries invalidated")]
    private static partial void LogAllProjectsCacheInvalidated(ILogger logger);
}
