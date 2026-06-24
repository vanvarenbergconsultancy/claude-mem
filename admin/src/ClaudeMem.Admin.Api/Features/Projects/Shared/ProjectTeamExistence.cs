namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

/// <summary>Existence check result used to distinguish "team not found" from "project not in team".</summary>
internal sealed record ProjectTeamExistence(bool TeamExists, bool BelongsToTeam);