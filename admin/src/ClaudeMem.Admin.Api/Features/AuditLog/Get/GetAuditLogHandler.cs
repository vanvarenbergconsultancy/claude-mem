using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.AuditLog.Get;

internal sealed class GetAuditLogHandler : IQueryHandler<GetAuditLogQuery, Result<CursorPageResult<AuditLogEntry>>>
{
    private readonly IAuditLogAccess _auditLogAccess;

    public GetAuditLogHandler(IAuditLogAccess auditLogAccess)
    {
        _auditLogAccess = auditLogAccess;
    }

    public async ValueTask<Result<CursorPageResult<AuditLogEntry>>> Handle(GetAuditLogQuery query, CancellationToken cancellationToken)
    {
        var filter = new GetAuditLogFilter(
            TeamId: query.TeamId,
            ProjectId: query.ProjectId,
            ApiKeyId: query.ApiKeyId,
            ActorId: query.ActorId,
            Action: query.Action,
            ResourceType: query.ResourceType,
            From: query.From,
            To: query.To,
            Cursor: query.Cursor.Cursor,
            PageSize: query.Cursor.PageSize);

        var page = await _auditLogAccess.GetAuditLog(filter, cancellationToken);

        return Result.Ok(page);
    }
}
