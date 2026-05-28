namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;

internal sealed class PlainCursorEncoder : ICursorEncoder
{
    public string Encode(CursorPayload payload)
    {
        return CursorPayload.Encode(payload);
    }

    public CursorPayload? Decode(string? cursor)
    {
        return CursorPayload.Decode(cursor);
    }
}
