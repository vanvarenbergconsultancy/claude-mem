using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

internal sealed class CursorStoreCleanupService : BackgroundService
{
    private readonly IEnumerable<ISupportsCursorPurge> _stores;
    private readonly TimeSpan _interval;

    public CursorStoreCleanupService(IEnumerable<ISupportsCursorPurge> stores, ServerStoredCursorOptions options)
    {
        _stores = stores;
        _interval = options.CleanupInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var store in _stores)
            {
                await store.PurgeExpiredAsync(stoppingToken);
            }
        }
    }
}
