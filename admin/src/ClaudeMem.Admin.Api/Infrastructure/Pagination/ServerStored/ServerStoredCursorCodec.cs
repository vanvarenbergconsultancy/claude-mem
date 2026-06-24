using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>Cursor codec that persists payloads server-side via <see cref="ICursorStore"/> and issues opaque token references to clients.</summary>
internal sealed class ServerStoredCursorCodec : ICursorCodec
{
    private readonly ICursorStore _store;

    public ServerStoredCursorCodec(ICursorStore store)
    {
        _store = store;
    }

    public Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        return _store.Store(payload, cancellationToken);
    }

    public async Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            return null;
        }

        var payload = await _store.Retrieve(token, cancellationToken);
        if(payload is null)
        {
            throw new InvalidCursorException();
        }

        return payload;
    }
}
