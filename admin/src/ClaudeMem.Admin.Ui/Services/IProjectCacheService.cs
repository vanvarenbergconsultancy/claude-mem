using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Ui.Services;

/// <summary>
/// Singleton cache for project lists, keyed per team. Shared across all Blazor Server circuits.
/// Invalidation is propagated to all circuits via <see cref="ICacheInvalidationBus"/>.
/// </summary>
public interface IProjectCacheService
{
    /// <summary>
    /// Returns all projects for the given team, serving from cache when available.
    /// On a cache miss the list is fetched from the API, cached with a 5-minute absolute TTL, and stampede-protected so concurrent callers for the same team share a single API request.
    /// </summary>
    Task<IReadOnlyList<Project>> GetForTeam(string teamId, CancellationToken cancellationToken = default);

    /// <summary>Removes the cached project list for the specified team and notifies all circuits via the invalidation bus.</summary>
    void InvalidateForTeam(string teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached project lists for every team that has been fetched in this process lifetime
    /// and notifies all circuits via the invalidation bus.
    /// </summary>
    void InvalidateAll(CancellationToken cancellationToken = default);
}
