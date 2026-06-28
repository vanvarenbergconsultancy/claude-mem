using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClaudeMem.Admin.Ui.Services;

/// <summary> Singleton implementation of <see cref="ICacheInvalidationBus"/> that fans out invalidation events to all active Blazor Server circuits. </summary>
internal sealed partial class CacheInvalidationBus : ICacheInvalidationBus
{
    private readonly List<Func<string, Task>> _handlers;
    private readonly Lock _lock;
    private readonly ILogger<CacheInvalidationBus> _logger;

    public CacheInvalidationBus(ILogger<CacheInvalidationBus> logger)
    {
        _handlers = [];
        _lock = new Lock();
        _logger = logger;
    }

    public void Subscribe(Func<string, Task> handler)
    {
        lock (_lock)
        {
            _handlers.Add(handler);
        }
    }

    public void Unsubscribe(Func<string, Task> handler)
    {
        lock (_lock)
        {
            _handlers.Remove(handler);
        }
    }

    public async Task Notify(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Snapshot the handler list under the lock and release immediately.
        // Holding the lock across async invocations would block Subscribe/Unsubscribe on all other threads for the full duration of every handler's await chain.
        List<Func<string, Task>> snapshot;
        lock (_lock)
        {
            snapshot = new List<Func<string, Task>>(_handlers);
        }

        LogNotifying(_logger, snapshot.Count, key);

        // Invoke all handlers and collect the tasks.
        // Synchronous throws are caught per-handler so one badly-written handler cannot prevent others from being scheduled.
        var tasks = new List<Task>(snapshot.Count);
        foreach (var handler in snapshot)
        {
            try
            {
                tasks.Add(handler(key));
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogHandlerThrewSynchronously(_logger, ex, key);
            }
        }

        // Wait for all async completions.
        // A fault in one handler is logged but does not propagate — one broken circuit must not cancel notifications to others.
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogHandlersFaulted(_logger, ex, key);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache invalidation bus notifying {HandlerCount} handlers for key '{Key}'")]
    private static partial void LogNotifying(ILogger logger, int handlerCount, string key);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cache invalidation handler threw synchronously for key '{Key}'")]
    private static partial void LogHandlerThrewSynchronously(ILogger logger, Exception ex, string key);

    [LoggerMessage(Level = LogLevel.Error, Message = "One or more cache invalidation handlers faulted for key '{Key}'")]
    private static partial void LogHandlersFaulted(ILogger logger, Exception ex, string key);
}
