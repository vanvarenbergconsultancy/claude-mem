namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal sealed record AuditLogDbRow(
    string Id,
    string? TeamId,
    string? ProjectId,
    string? ActorId,
    string? ApiKeyId,
    string Action,
    string ResourceType,
    string? ResourceId);
