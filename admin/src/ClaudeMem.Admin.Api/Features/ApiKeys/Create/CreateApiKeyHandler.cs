using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;
using ClaudeMem.Admin.Api.Features.Projects.Shared;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Create;

internal sealed class CreateApiKeyHandler : ICommandHandler<CreateApiKeyCommand, Result<NewApiKey>>
{
    private readonly IApiKeyAccess _apiKeyAccess;
    private readonly IProjectAccess _projectAccess;

    public CreateApiKeyHandler(IApiKeyAccess apiKeyAccess, IProjectAccess projectAccess)
    {
        _apiKeyAccess = apiKeyAccess;
        _projectAccess = projectAccess;
    }

    public async ValueTask<Result<NewApiKey>> Handle(CreateApiKeyCommand command, CancellationToken cancellationToken)
    {
        var teamAndProjectExistence = await _projectAccess.CheckProjectTeamExistence(command.TeamId, command.ProjectId, cancellationToken);
        var teamDoesNotHaveAccessToProjectError = teamAndProjectExistence.ValidateTeamAndProjectIdMisMatch();
        if (teamDoesNotHaveAccessToProjectError.HasValue)
        {
            return Result.Fail<NewApiKey>(teamDoesNotHaveAccessToProjectError.Value.AsError());
        }
        
        var plaintextKey = GenerateSecureRandomKey();
        var hashedKey = ComputeSha256Hash(plaintextKey);

        var insertedApiKey = await _apiKeyAccess.InsertApiKey(command.TeamId, command.ProjectId, command.ActorId, hashedKey, cancellationToken);

        return Result.Ok(new NewApiKey(insertedApiKey.Id, insertedApiKey.ActorId, plaintextKey, insertedApiKey.CreatedAt));
    }

    private static string GenerateSecureRandomKey()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);

        return $"cmem_{Base64Url.EncodeToString(randomBytes)}";
    }

    private static string ComputeSha256Hash(string plaintextKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(plaintextKey);
        var sha256Bytes = SHA256.HashData(keyBytes);

        return Convert.ToHexString(sha256Bytes).ToLowerInvariant();
    }
}
