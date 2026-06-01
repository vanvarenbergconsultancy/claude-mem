using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;

internal sealed class PlainCursorCodec : ICursorCodec
{
    public Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CursorPayload.Encode(payload));
    }

    public Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CursorPayload.Decode(token));
    }
}
