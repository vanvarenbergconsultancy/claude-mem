using System.ComponentModel.DataAnnotations;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;

public sealed record SignedCursorOptions
{
    [Required]
    public string SigningKey { get; init; } = string.Empty;
}
