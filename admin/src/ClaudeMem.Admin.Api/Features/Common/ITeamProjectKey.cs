namespace ClaudeMem.Admin.Api.Features.Common;

internal interface ITeamProjectKey
{
    public string TeamId { get; init; }
    public string ProjectId { get; init; }
}