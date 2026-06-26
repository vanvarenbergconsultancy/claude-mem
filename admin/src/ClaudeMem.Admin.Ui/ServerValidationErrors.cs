using System;
using System.Collections.Generic;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Ui;

/// <summary>Wraps the per-field error dictionary from a 422 ValidationProblemDetails response.</summary>
internal sealed class ServerValidationErrors
{
    public static readonly ServerValidationErrors Empty = new(null);

    private readonly IDictionary<string, IEnumerable<string>>? _errors;

    public ServerValidationErrors(IDictionary<string, IEnumerable<string>>? errors)
    {
        _errors = errors?.Count > 0 ? errors : null;
    }

    public static ServerValidationErrors FromException(Exception ex)
    {
        if (ex is ApiException<ValidationProblemDetails> { Result.Errors: { Count: > 0 } errors })
        {
            return new ServerValidationErrors(errors);
        }

        return Empty;
    }

    public bool HasAny => _errors is not null;

    public bool HasError(string propertyName)
    {
        return _errors?.ContainsKey(propertyName) ?? false;
    }

    public string GetErrorText(string propertyName)
    {
        return _errors is not null && _errors.TryGetValue(propertyName, out var messages)
            ? string.Join(", ", messages)
            : string.Empty;
    }
}
