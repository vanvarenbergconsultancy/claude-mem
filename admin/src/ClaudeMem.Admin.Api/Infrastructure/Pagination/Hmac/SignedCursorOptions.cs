using System.ComponentModel.DataAnnotations;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;

/// <summary>Configuration for the HMAC-signed cursor codec.</summary>
public sealed record SignedCursorOptions
{
    /// <summary>The HMAC-SHA-256 signing key. Required; must not be empty.</summary>
    [Required]
    public string SigningKey { get; init; } = string.Empty;
}
