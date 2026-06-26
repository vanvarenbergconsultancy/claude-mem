using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.AuditLog;

internal interface IAuditLogAccess
{
    Task<CursorPageResult<AuditLogEntry>> GetAuditLog(GetAuditLogFilter filter, CancellationToken cancellationToken);
    Task WriteEntry(AuditLogWriteData data, CancellationToken cancellationToken);
}
