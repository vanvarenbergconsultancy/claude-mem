using System.ComponentModel.DataAnnotations;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;

internal sealed class EncryptedCursorEncoderOptions
{
    // Base64-encoded 32-byte (256-bit) AES-256 key.
    [Required]
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
}
