using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Projects.Shared;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Get;

internal sealed class GetApiKeysHandler : IQueryHandler<GetApiKeysQuery, Result<CursorPageResult<ApiKey>>>
{
    private readonly IApiKeyAccess _apiKeyAccess;
    private readonly IProjectAccess _projectAccess;

    public GetApiKeysHandler(IApiKeyAccess apiKeyAccess, IProjectAccess projectAccess)
    {
        _apiKeyAccess = apiKeyAccess;
        _projectAccess = projectAccess;
    }

    public async ValueTask<Result<CursorPageResult<ApiKey>>> Handle(GetApiKeysQuery query, CancellationToken cancellationToken)
    {
        var teamAndProjectExistence = await _projectAccess.CheckProjectTeamExistence(query.TeamId, query.ProjectId, cancellationToken);
        var teamDoesNotHaveAccessToProjectError = teamAndProjectExistence.ValidateTeamAndProjectIdMisMatch();
        if (teamDoesNotHaveAccessToProjectError.HasValue)
        {
            return Result.Fail<CursorPageResult<ApiKey>>(teamDoesNotHaveAccessToProjectError.Value.AsError());
        }

        var filter = new GetApiKeysFilter(
            ProjectId: query.ProjectId,
            Cursor: query.Cursor.Cursor,
            PageSize: query.Cursor.PageSize);

        var apiKeysPage = await _apiKeyAccess.GetApiKeys(filter, cancellationToken);

        return Result.Ok(apiKeysPage);
    }
}
