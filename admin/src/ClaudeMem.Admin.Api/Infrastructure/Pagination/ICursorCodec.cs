using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal interface ICursorCodec
{
    Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default);

    /// <summary>Converts a token string back into a <see cref="CursorPayload"/>.</summary>
    /// <returns>Null when <paramref name="token"/> is null (first-page semantics).</returns>
    /// <exception cref="InvalidCursorException">Thrown when the token is invalid or tampered with.</exception>
    Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default);
}
