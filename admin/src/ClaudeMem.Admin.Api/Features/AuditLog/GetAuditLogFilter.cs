using System;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.AuditLog;

internal sealed record GetAuditLogFilter(
    string? TeamId = null,
    string? ProjectId = null,
    string? ApiKeyId = null,
    string? ActorId = null,
    string? Action = null,
    string? ResourceType = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;
