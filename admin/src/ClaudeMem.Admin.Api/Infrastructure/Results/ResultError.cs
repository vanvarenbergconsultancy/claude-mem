namespace ClaudeMem.Admin.Api.Infrastructure.Results;

/// <summary>Describes a single failure within a <see cref="Result{T}"/>, carrying a machine-readable code, a human-readable message, and an error classification.</summary>
/// <param name="Code">Machine-readable identifier used to derive the <c>/problems/{code}</c> problem type URI.</param>
/// <param name="Message">Human-readable description of the failure, surfaced as the ProblemDetails <c>detail</c> field.</param>
/// <param name="Type">Classification that controls which HTTP status code the error maps to.</param>
public sealed record ResultError(string Code, string Message, ErrorType Type);
