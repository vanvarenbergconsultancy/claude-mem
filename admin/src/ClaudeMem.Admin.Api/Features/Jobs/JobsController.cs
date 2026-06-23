using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Features.Jobs;

[ApiController]
[Route("jobs")]
public sealed class JobsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator, PaginationLinker linker)
        : base(linker)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? cursor,
        [FromQuery(Name = "page_size")] int? pageSize = null,
        [FromQuery] string? status = null,
        [FromQuery(Name = "project_id")] string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var cursorRequest = new CursorRequest(cursor, pageSize);
        var query = new Get.GetJobsQuery(cursorRequest, status, projectId);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, page =>
        {
            var links = Linker.Build(page.SelfCursor, page.NextCursor, pageSize);
            var jobPage = new JobPage(links.Self, links.First, links.Next, page.Items);
            return Ok(jobPage);
        });
    }

    [HttpPost("{jobId}/retry")]
    public async Task<IActionResult> RetryJob(string jobId, CancellationToken cancellationToken)
    {
        var command = new Retry.RetryJobCommand(jobId);
        var result = await _mediator.Send(command, cancellationToken);

        return FromResult(result, _ => NoContent());
    }
}
