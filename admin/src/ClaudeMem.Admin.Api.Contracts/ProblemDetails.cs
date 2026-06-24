using System.Text.Json.Serialization;

namespace ClaudeMem.Admin.Api.Contracts;

/// <summary>RFC 7807 problem details payload returned by the API on error responses.</summary>
public record ProblemDetails(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("status")] int? Status,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("instance")] string? Instance);
