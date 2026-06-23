using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Features.ApiKeys;

[ApiController]
[Route("teams/{teamId}/projects/{projectId}/api-keys")]
public sealed class ApiKeysController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ApiKeysController(IMediator mediator, PaginationLinker linker)
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
        CancellationToken cancellationToken = default)
    {
        var cursorRequest = new CursorRequest(cursor, pageSize);
        var query = new Get.GetApiKeysQuery(teamId, projectId, cursorRequest);
        var result = await _mediator.Send(query, cancellationToken);

        return FromResult(result, page =>
        {
            var links = Linker.Build(page.SelfCursor, page.NextCursor, pageSize);
            var apiKeyPage = new ApiKeyPage(links.Self, links.First, links.Next, page.Items);
            return Ok(apiKeyPage);
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(string teamId, string projectId, [FromBody] ApiKey body, CancellationToken cancellationToken)
    {
        var command = new Create.CreateApiKeyCommand(teamId, projectId, body.ActorId);
        var result = await _mediator.Send(command, cancellationToken);

        return FromResult(result, createdKey => StatusCode(StatusCodes.Status201Created, createdKey));
    }

    [HttpDelete("{keyId}")]
    public async Task<IActionResult> Delete(string teamId, string projectId, string keyId, CancellationToken cancellationToken)
    {
        var command = new Delete.DeleteApiKeyCommand(teamId, projectId, keyId);
        var result = await _mediator.Send(command, cancellationToken);

        return FromResult(result, _ => NoContent());
    }
}
