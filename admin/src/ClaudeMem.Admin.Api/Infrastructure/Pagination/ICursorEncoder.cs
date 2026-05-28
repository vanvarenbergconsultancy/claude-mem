namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal interface ICursorEncoder
{
    string Encode(CursorPayload payload);

    /// <summary> Decodes a cursor string into a <see cref="CursorPayload"/> object. </summary>
    /// <param name="cursor">The cursor string to decode.</param>
    /// <returns>The decoded <see cref="CursorPayload"/> object, or null if the cursor is null (first-page semantics).</returns>
    /// <exception cref="InvalidCursorException">Thrown when the cursor is invalid or tampered with.</exception>
    CursorPayload? Decode(string? cursor);
}
