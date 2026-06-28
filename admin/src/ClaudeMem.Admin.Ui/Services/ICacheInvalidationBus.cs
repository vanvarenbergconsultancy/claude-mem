using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Ui.Services;

/// <summary>
/// Broadcasts cache invalidation events across all active Blazor Server circuits.
/// Components subscribe to receive notifications when cached data changes, allowing them to refresh their local state without polling.
/// </summary>
public interface ICacheInvalidationBus
{
    /// <summary>
    /// Registers a callback to be invoked when a cache key is invalidated.
    /// The handler receives the invalidated cache key as its argument.
    /// Call <see cref="Unsubscribe"/> in the component's <c>Dispose</c> to prevent memory leaks.
    /// </summary>
    void Subscribe(Func<string, Task> handler);

    /// <summary>
    /// Removes a previously registered handler.
    /// Must be called from <c>IDisposable.Dispose</c> on any component that called <see cref="Subscribe"/> to prevent memory leaks and ghost callbacks on disposed circuit instances.
    /// </summary>
    void Unsubscribe(Func<string, Task> handler);

    /// <summary>
    /// Notifies all subscribed handlers that the entry identified by <paramref name="key"/> has been invalidated.
    /// Handler exceptions are logged and isolated so one failing circuit cannot block others.
    /// </summary>
    Task Notify(string key, CancellationToken cancellationToken = default);
}
