using System;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Infrastructure;

/// <summary>Base class for all API controllers, providing access to the pagination linker and a uniform result-to-action-result mapper.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Builds hypermedia pagination links for paged responses.</summary>
    protected PaginationLinker Linker { get; }

    protected ApiControllerBase(PaginationLinker linker)
    {
        Linker = linker;
    }

    /// <summary>Maps a domain <see cref="Result{T}"/> to an <see cref="IActionResult"/>, translating error types to the appropriate HTTP status codes.</summary>
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var error = result.Errors[0];
        var problemType = "/problems/" + error.Code.Replace('_', '-');

        return error.Type switch
        {
            ErrorType.NotFound   => Problem(type: problemType, detail: error.Message, statusCode: 404),
            ErrorType.Conflict   => Problem(type: problemType, detail: error.Message, statusCode: 409),
            ErrorType.Validation => Problem(type: problemType, detail: error.Message, statusCode: 422),
            ErrorType.Forbidden  => Forbid(),
            _                    => Problem(type: problemType, detail: error.Message, statusCode: 500)
        };
    }
}
