using System;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Features.AuditLog;
using ClaudeMem.Admin.Api.Features.Projects.Shared;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Delete;

internal sealed class DeleteApiKeyHandler : ICommandHandler<DeleteApiKeyCommand, Result<Unit>>
{
    private readonly IApiKeyAccess _apiKeyAccess;
    private readonly IProjectAccess _projectAccess;
    private readonly IAuditLogAccess _auditLog;

    public DeleteApiKeyHandler(IApiKeyAccess apiKeyAccess, IProjectAccess projectAccess, IAuditLogAccess auditLog)
    {
        _apiKeyAccess = apiKeyAccess;
        _projectAccess = projectAccess;
        _auditLog = auditLog;
    }

    public async ValueTask<Result<Unit>> Handle(DeleteApiKeyCommand command, CancellationToken cancellationToken)
    {
        var existingApiKey = await _apiKeyAccess.GetApiKey(command.KeyId, cancellationToken);
        if (existingApiKey is null)
        {
            return await GetNotFoundResult(command, cancellationToken);
        }

        var errorCode = GetMismatchError(command, existingApiKey);
        if (errorCode is not null)
        {
            return Result.Fail<Unit>(errorCode.Value.AsError());
        }

        await _apiKeyAccess.RevokeApiKeyById(command.KeyId, cancellationToken);

        await _auditLog.WriteEntry(new AuditLogWriteData(
            Action: "api_key.revoke",
            ResourceType: "api_key",
            ResourceId: command.KeyId,
            TeamId: command.TeamId,
            ProjectId: command.ProjectId,
            ApiKeyId: command.KeyId), cancellationToken);

        return Result.Ok(Unit.Value);
    }

    private async Task<Result<Unit>> GetNotFoundResult(DeleteApiKeyCommand command, CancellationToken cancellationToken)
    {
        var existence = await _projectAccess.CheckProjectTeamExistence(command.TeamId, command.ProjectId, cancellationToken);
        var teamDoesNotHaveAccessToProjectError = existence.ValidateTeamAndProjectIdMisMatch();
        if (teamDoesNotHaveAccessToProjectError.HasValue)
        {
            return Result.Fail<Unit>(teamDoesNotHaveAccessToProjectError.Value.AsError());
        }

        return Result.Fail<Unit>(ResultErrorCodes.ApiKeyNotFound.AsError());
    }

    private static ErrorCode? GetMismatchError(DeleteApiKeyCommand command, ApiKeyLookup existingApiKey)
    {
        if (!string.Equals(existingApiKey.TeamId, command.TeamId, StringComparison.Ordinal) || !string.Equals(existingApiKey.ProjectId, command.ProjectId, StringComparison.Ordinal))
        {
            return ResultErrorCodes.ApiKeyNotFound;
        }

        return null;
    }
}