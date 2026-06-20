using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Features.Teams;

[ApiController]
[Route("teams")]
public sealed class TeamsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator, PaginationLinker linker)
        : base(linker)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? cursor,
        [FromQuery(Name = "page_size")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var cursorRequest = new CursorRequest(cursor, pageSize);
        var query = new Get.GetTeamsQuery(cursorRequest);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, page =>
        {
            var links = Linker.Build(page.SelfCursor, page.NextCursor, pageSize);
            var teamPage = new TeamPage(links.Self, links.First, links.Next, page.Items);
            
            return Ok(teamPage);
        });
    }

    [HttpGet("{teamId}")]
    public async Task<IActionResult> GetById(string teamId, CancellationToken cancellationToken)
    {
        var query = new GetById.GetTeamByIdQuery(teamId);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Team body, CancellationToken cancellationToken)
    {
        var command = new Create.CreateTeamCommand(body.Name);
        var result = await _mediator.Send(command, cancellationToken);

        return FromResult(result, createdTeam => CreatedAtAction(nameof(GetById), new { teamId = createdTeam.Id }, createdTeam));
    }
}
