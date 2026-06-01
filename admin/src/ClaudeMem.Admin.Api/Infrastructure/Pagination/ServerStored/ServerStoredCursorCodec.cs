using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

internal sealed class ServerStoredCursorCodec : ICursorCodec
{
    private readonly ICursorStore _store;

    public ServerStoredCursorCodec(ICursorStore store)
    {
        _store = store;
    }

    public Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        return _store.StoreAsync(payload, cancellationToken);
    }

    public async Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            return null;
        }

        var payload = await _store.RetrieveAsync(token, cancellationToken);
        if(payload is null)
        {
            throw new InvalidCursorException();
        }

        return payload;
    }
}
