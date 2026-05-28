namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

public enum CursorEncodingStrategy
{
    Plain = 0,
    Hmac = 1,
    Encrypted = 2,
}
