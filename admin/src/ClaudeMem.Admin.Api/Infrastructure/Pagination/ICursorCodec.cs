using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>
/// Converts <see cref="CursorPayload"/> values to and from opaque cursor tokens that are safe to expose in API responses.
/// </summary>
/// <remarks>
/// Three built-in strategies are provided:
/// <list type="bullet">
///   <item><description><c>PlainCursorCodec</c> — base64-encoded JSON, no integrity guarantee.</description></item>
///   <item><description><c>SignedCursorCodec</c> — HMAC-SHA256 signed, tamper-evident.</description></item>
///   <item><description><c>EncryptedCursorCodec</c> — ASP.NET Core Data Protection, encrypted + authenticated.</description></item>
///   <item><description><c>ServerStoredCursorCodec</c> — opaque server-side token, payload never leaves the server.</description></item>
/// </list>
/// All methods use <see cref="Task{T}"/> (not <see cref="System.Threading.Tasks.ValueTask{T}"/>) so the interface is safe to consume from library code where callers cannot be trusted to follow <c>ValueTask</c>'s single-await contract.
/// </remarks>
internal interface ICursorCodec
{
    /// <summary>Encodes a <see cref="CursorPayload"/> into an opaque cursor token.</summary>
    Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default);

    /// <summary>Decodes an opaque cursor token back into a <see cref="CursorPayload"/>.</summary>
    /// <returns><c>null</c> when <paramref name="token"/> is <c>null</c> (first-page semantics).</returns>
    /// <exception cref="InvalidCursorException">The token is syntactically invalid or has been tampered with.</exception>
    Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default);
}
