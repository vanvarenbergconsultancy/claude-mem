namespace ClaudeMem.Admin.Api.Infrastructure.Results;

public sealed record ResultError(string Code, string Message, ErrorType Type);
