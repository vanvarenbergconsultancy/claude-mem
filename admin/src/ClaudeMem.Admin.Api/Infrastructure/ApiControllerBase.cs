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
        var problemType = "/problems/" + error.Code.Replace('_', '-');

        return error.Type switch
        {
            ErrorType.NotFound   => Problem(type: problemType, detail: error.Message, statusCode: 404),
            ErrorType.Conflict   => Problem(type: problemType, detail: error.Message, statusCode: 409),
            ErrorType.Validation => Problem(type: problemType, detail: error.Message, statusCode: 422),
            ErrorType.Forbidden  => Forbid(),
            _                    => Problem(type: problemType, detail: error.Message, statusCode: 500),
        };
    }
}
