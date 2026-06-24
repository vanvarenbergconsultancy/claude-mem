using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;

/// <summary>Cursor codec that base-64 encodes the payload without any signing or encryption. Do not use in production-facing APIs.</summary>
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
