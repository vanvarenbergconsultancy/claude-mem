using System;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.AuditLog.Get;

internal sealed record GetAuditLogQuery(
    CursorRequest Cursor,
    string? TeamId,
    string? ProjectId,
    string? ApiKeyId,
    string? ActorId,
    string? Action,
    string? ResourceType,
    DateTimeOffset? From,
    DateTimeOffset? To) : IQuery<Result<CursorPageResult<AuditLogEntry>>>;
