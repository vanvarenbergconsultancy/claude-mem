using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Infrastructure.Results;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<ResultError> Errors { get; }

    internal Result(bool isSuccess, T? value, IReadOnlyList<ResultError> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => new(true, value, []);
    public static Result<T> Fail<T>(params ResultError[] errors) => new(false, default, errors);
}
