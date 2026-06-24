using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMem.Admin.Api.Infrastructure.Validation;

/// <summary>Converts FluentValidation <see cref="ValidationException"/> to a 422 Unprocessable Entity problem-details response with field-level errors.</summary>
internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var groupedErrorsByPropertyName = validationException.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problemDetails = new ValidationProblemDetails(groupedErrorsByPropertyName)
        {
            Status = Constants.StatusCodeConventions.ValidationFailedStatusCode
        };

        httpContext.Response.StatusCode = Constants.StatusCodeConventions.ValidationFailedStatusCode;

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        };

        await _problemDetailsService.WriteAsync(problemDetailsContext);

        return true;
    }
}
