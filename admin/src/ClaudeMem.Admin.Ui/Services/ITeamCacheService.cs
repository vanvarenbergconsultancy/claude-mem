using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Ui.Services;

/// <summary>
/// Singleton cache for the full list of teams.
/// Shared across all Blazor Server circuits to avoid redundant API calls when multiple browser tabs or users open the admin UI simultaneously.
/// Invalidation is propagated to all circuits via <see cref="ICacheInvalidationBus"/>.
/// </summary>
public interface ITeamCacheService
{
    /// <summary>
    /// Returns all teams, serving from the in-memory cache when available.
    /// On a cache miss the list is fetched from the API, cached with a 5-minute absolute TTL, and stampede-protected so concurrent callers share a single API request.
    /// </summary>
    Task<IReadOnlyList<Team>> GetAll(CancellationToken cancellationToken = default);

    /// <summary> Removes the cached team list and notifies all subscribed circuits via the invalidation bus so they can refresh their local state. </summary>
    void Invalidate(CancellationToken cancellationToken = default);
}
