using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>Converts <see cref="InvalidCursorException"/> to a 400 Bad Request problem-details response.</summary>
internal sealed class InvalidCursorExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public InvalidCursorExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InvalidCursorException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid cursor.",
            Detail = exception.Message,
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        };

        await _problemDetailsService.WriteAsync(problemDetailsContext);

        return true;
    }
}
