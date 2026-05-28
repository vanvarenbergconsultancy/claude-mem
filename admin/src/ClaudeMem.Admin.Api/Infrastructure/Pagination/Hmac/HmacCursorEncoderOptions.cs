using System.ComponentModel.DataAnnotations;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;

internal sealed class HmacCursorEncoderOptions
{
    [Required]
    public string SigningKey { get; set; } = string.Empty;
}
