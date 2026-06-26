using System;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.AuditLog.Get;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Features.AuditLog;

[ApiController]
[Route("audit-log")]
public sealed class AuditLogController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogController(IMediator mediator, PaginationLinker linker)
        : base(linker)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? cursor,
        [FromQuery(Name = "page_size")] int? pageSize = null,
        [FromQuery(Name = "team_id")] string? teamId = null,
        [FromQuery(Name = "project_id")] string? projectId = null,
        [FromQuery(Name = "api_key_id")] string? apiKeyId = null,
        [FromQuery(Name = "actor_id")] string? actorId = null,
        [FromQuery] string? action = null,
        [FromQuery(Name = "resource_type")] string? resourceType = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var cursorRequest = new CursorRequest(cursor, pageSize);
        var query = new GetAuditLogQuery(cursorRequest, teamId, projectId, apiKeyId, actorId, action, resourceType, from, to);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, page =>
        {
            var links = Linker.Build(page.SelfCursor, page.NextCursor, pageSize);
            var auditLogPage = new AuditLogPage(links.Self, links.First, links.Next, page.Items);
            return Ok(auditLogPage);
        });
    }
}
