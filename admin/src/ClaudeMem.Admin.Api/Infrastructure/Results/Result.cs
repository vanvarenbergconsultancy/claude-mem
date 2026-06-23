using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Infrastructure.Results;

/// <summary>Represents the outcome of an operation that either succeeds with a value or fails with one or more errors.</summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    /// <summary>True when the operation succeeded; false when it failed.</summary>
    public bool IsSuccess { get; }

    /// <summary>The success value. Only meaningful when <see cref="IsSuccess"/> is true.</summary>
    public T? Value { get; }

    /// <summary>The errors describing why the operation failed. Empty when <see cref="IsSuccess"/> is true.</summary>
    public IReadOnlyList<ResultError> Errors { get; }

    internal Result(bool isSuccess, T? value, IReadOnlyList<ResultError> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }
}

/// <summary>Factory methods for creating <see cref="Result{T}"/> instances.</summary>
public static class Result
{
    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Ok<T>(T value) => new(true, value, []);

    /// <summary>Creates a failed result carrying one or more <paramref name="errors"/>.</summary>
    public static Result<T> Fail<T>(params ResultError[] errors) => new(false, default, errors);
}
