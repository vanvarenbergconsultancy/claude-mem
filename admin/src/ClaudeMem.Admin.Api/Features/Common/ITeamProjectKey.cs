namespace ClaudeMem.Admin.Api.Features.Common;

/// <summary>Marker interface for commands and queries that are scoped to a team and project.</summary>
internal interface ITeamProjectKey
{
    /// <summary>The team that owns the resource.</summary>
    public string TeamId { get; init; }

    /// <summary>The project within the team.</summary>
    public string ProjectId { get; init; }
}