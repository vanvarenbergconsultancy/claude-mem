using System;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Infrastructure;

public abstract class ApiControllerBase : ControllerBase
{
    protected PaginationLinker Linker { get; }

    protected ApiControllerBase(PaginationLinker linker)
    {
        Linker = linker;
    }

    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var error = result.Errors[0];
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(error),
            ErrorType.Conflict => Conflict(error),
            ErrorType.Validation => UnprocessableEntity(error),
            ErrorType.Forbidden => Forbid(),
            _ => StatusCode(500, error)
        };
    }
}
