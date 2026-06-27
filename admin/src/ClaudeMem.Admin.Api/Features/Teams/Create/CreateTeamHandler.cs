using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.AuditLog;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.Create;

internal sealed class CreateTeamHandler : ICommandHandler<CreateTeamCommand, Result<Team>>
{
    private readonly ITeamAccess _teamAccess;
    private readonly IAuditLogAccess _auditLog;

    public CreateTeamHandler(ITeamAccess teamAccess, IAuditLogAccess auditLog)
    {
        _teamAccess = teamAccess;
        _auditLog = auditLog;
    }

    public async ValueTask<Result<Team>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var createdTeam = await _teamAccess.CreateTeam(command.Name, cancellationToken);

        await _auditLog.WriteEntry(new AuditLogWriteData(
            Action: "team.create",
            ResourceType: "team",
            ResourceId: createdTeam.Id!,
            TeamId: createdTeam.Id), cancellationToken);

        return Result.Ok(createdTeam);
    }
}
