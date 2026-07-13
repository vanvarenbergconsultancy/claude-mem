using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class InvalidCursorExceptionHandlerTests
{
    private static InvalidCursorExceptionHandler CreateHandler()
    {
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
#pragma warning disable CA2012 // NSubstitute setup intercepts the call; ValueTask is never actually stored
        problemDetailsService.WriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(ValueTask.CompletedTask);
#pragma warning restore CA2012
        return new InvalidCursorExceptionHandler(problemDetailsService);
    }

    [Fact]
    public async Task TryHandleAsync_NonInvalidCursorException_ReturnsFalse()
    {
        var handler = CreateHandler();
        var httpContext = new DefaultHttpContext();

        var result = await handler.TryHandleAsync(httpContext, new InvalidOperationException(), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_InvalidCursorException_ReturnsTrueAndSets400()
    {
        var handler = CreateHandler();
        var httpContext = new DefaultHttpContext();

        var result = await handler.TryHandleAsync(httpContext, new InvalidCursorException(), TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
