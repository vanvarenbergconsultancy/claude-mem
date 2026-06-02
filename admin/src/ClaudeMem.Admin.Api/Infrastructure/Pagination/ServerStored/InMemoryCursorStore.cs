using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

internal sealed class InMemoryCursorStore : ICursorStore, ISupportsCursorPurge
{
    private readonly ConcurrentDictionary<string, (CursorPayload Payload, DateTimeOffset Expiry)> _store = new();
    private readonly ServerStoredCursorOptions _options;

    public InMemoryCursorStore(ServerStoredCursorOptions options)
    {
        _options = options;
    }

    public Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        var expiry = DateTimeOffset.UtcNow.Add(_options.TokenExpiry);
        _store[token] = (payload, expiry);

        return Task.FromResult(token);
    }

    public Task<CursorPayload?> Retrieve(string token, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(token, out var entry))
        {
            if (entry.Expiry > DateTimeOffset.UtcNow)
            {
                return Task.FromResult<CursorPayload?>(entry.Payload);
            }

            _store.TryRemove(token, out _);
        }

        return Task.FromResult<CursorPayload?>(null);
    }

    public Task PurgeExpiredAsync(CancellationToken cancellationToken = default)
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
