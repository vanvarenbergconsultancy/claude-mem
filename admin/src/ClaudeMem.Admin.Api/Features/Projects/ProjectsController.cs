using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Features.Projects;

[ApiController]
[Route("teams/{teamId}/projects")]
public sealed class ProjectsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator, PaginationLinker linker)
        : base(linker)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        string teamId,
        [FromQuery] string? cursor,
        [FromQuery(Name = "page_size")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var cursorRequest = new CursorRequest(cursor, pageSize);
        var query = new Get.GetProjectsQuery(teamId, cursorRequest);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, page =>
        {
            var links = Linker.Build(page.SelfCursor, page.NextCursor, pageSize);
            var projectPage = new ProjectPage(links.Self, links.First, links.Next, page.Items);
            return Ok(projectPage);
        });
    }

    [HttpGet("{projectId}")]
    public async Task<IActionResult> GetById(string teamId, string projectId, CancellationToken cancellationToken)
    {
        var query = new GetById.GetProjectByIdQuery(teamId, projectId);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string teamId, [FromBody] Project body, CancellationToken cancellationToken)
    {
        var command = new Create.CreateProjectCommand(teamId, body.Name);
        var result = await _mediator.Send(command, cancellationToken);

        return FromResult(result, createdProject => CreatedAtAction(nameof(GetById), new { teamId, projectId = createdProject.Id }, createdProject));
    }
}
