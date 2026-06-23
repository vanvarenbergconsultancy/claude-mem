using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Features.Observations;

[ApiController]
[Route("teams/{teamId}/projects/{projectId}/observations")]
public sealed class ObservationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ObservationsController(IMediator mediator, PaginationLinker linker)
        : base(linker)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        string teamId,
        string projectId,
        [FromQuery] string? cursor,
        [FromQuery(Name = "page_size")] int? pageSize = null,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        var cursorRequest = new CursorRequest(cursor, pageSize);
        var query = new Get.GetObservationsQuery(teamId, projectId, cursorRequest, q);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, page =>
        {
            var links = Linker.Build(page.SelfCursor, page.NextCursor, pageSize);
            var observationPage = new ObservationPage(links.Self, links.First, links.Next, page.Items);
            return Ok(observationPage);
        });
    }
}
