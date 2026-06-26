namespace ClaudeMem.Admin.Api.Features.AuditLog;

internal sealed record AuditLogWriteData(
    string Action,
    string ResourceType,
    string ResourceId,
    string? TeamId = null,
    string? ProjectId = null,
    string? ActorId = "admin-api",
    string? ApiKeyId = null,
    string Details = "{}");
