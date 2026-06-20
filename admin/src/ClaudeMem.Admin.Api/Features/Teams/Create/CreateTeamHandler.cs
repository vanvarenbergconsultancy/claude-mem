using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.Create;

internal sealed class CreateTeamHandler : ICommandHandler<CreateTeamCommand, Result<Team>>
{
    private readonly ITeamAccess _teamAccess;

    public CreateTeamHandler(ITeamAccess teamAccess)
    {
        _teamAccess = teamAccess;
    }

    public async ValueTask<Result<Team>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var createdTeam = await _teamAccess.CreateTeam(command.Name, cancellationToken);

        return Result.Ok(createdTeam);
    }
}
