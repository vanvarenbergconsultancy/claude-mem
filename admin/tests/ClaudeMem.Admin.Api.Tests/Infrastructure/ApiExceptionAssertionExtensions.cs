using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Specialized;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal static class ApiExceptionAssertionExtensions
{
    public static ApiExceptionAssertions Should(this ApiException instance)
        => new(instance, AssertionChain.GetOrCreate());

    [CustomAssertion]
    public static async Task ThrowApiExceptionAsync<TTask, TAssertions>(
        this AsyncFunctionAssertions<TTask, TAssertions> assertions,
        HttpStatusCode statusCode,
        string because = "",
        params object[] becauseArgs)
        where TTask : Task
        where TAssertions : AsyncFunctionAssertions<TTask, TAssertions>
    {
        var constraint = await assertions.ThrowAsync<ApiException>();
        constraint.Which.Should().HaveStatusCode(statusCode, because, becauseArgs);
    }

    [CustomAssertion]
    public static async Task ThrowProblemDetailsAsync<TTask, TAssertions>(
        this AsyncFunctionAssertions<TTask, TAssertions> assertions,
        HttpStatusCode statusCode,
        string problemType,
        string because = "",
        params object[] becauseArgs)
        where TTask : Task
        where TAssertions : AsyncFunctionAssertions<TTask, TAssertions>
    {
        var constraint = await assertions.ThrowAsync<ApiException<ProblemDetails>>(because, becauseArgs);
        constraint.Which.Should().HaveStatusCode(statusCode);
        constraint.Which.Result.Type.Should().Be(problemType);
    }
}
