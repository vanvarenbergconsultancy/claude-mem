namespace ClaudeMem.Admin.Api.Infrastructure.Results;

/// <summary>Well-known domain error codes returned by command and query handlers.</summary>
public static class ResultErrorCodes
{
    public static readonly ErrorCode TeamNotFound           = new("team_not_found",             "Team not found.",                                           ErrorType.NotFound);
    // NotFound rather than Conflict or Forbidden: returning 409/403 leaks that the resource exists under another tenant, enabling enumeration attacks.
    // Also covers genuinely non-existent projects to prevent callers from enumerating valid project IDs across teams.
    public static readonly ErrorCode ProjectTeamMismatch    = new("project_team_mismatch",      "The project does not belong to the specified team.",        ErrorType.NotFound);
    // Covers both genuinely missing keys and keys that exist under a different team/project — callers cannot enumerate valid key IDs.
    public static readonly ErrorCode ApiKeyNotFound         = new("api_key_not_found",          "API key not found.",                                        ErrorType.NotFound);
    public static readonly ErrorCode JobNotFound            = new("job_not_found",              "Job not found.",                                            ErrorType.NotFound);
    public static readonly ErrorCode JobNotInFailedState    = new("job_not_in_failed_state",    "Job is not in a failed state.",                             ErrorType.Conflict);
}
