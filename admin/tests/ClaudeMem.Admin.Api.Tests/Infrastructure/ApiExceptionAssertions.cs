using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;
using AwesomeAssertions.Specialized;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal sealed class ApiExceptionAssertions
    : ReferenceTypeAssertions<ApiException, ApiExceptionAssertions>
{
    private readonly AssertionChain _chain;

    internal ApiExceptionAssertions(ApiException instance, AssertionChain chain)
        : base(instance, chain)
    {
        _chain = chain;
    }

    protected override string Identifier => "ApiException";

    public AndConstraint<ApiExceptionAssertions> HaveStatusCode(
        HttpStatusCode statusCode,
        string because = "",
        params object[] becauseArgs)
    {
        _chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.StatusCode == (int)statusCode)
            .FailWith(
                "Expected {context:ApiException} to have status code {0}{reason}, but found {1}.",
                statusCode,
                (HttpStatusCode)Subject.StatusCode);

        return new AndConstraint<ApiExceptionAssertions>(this);
    }

    public AndConstraint<ApiExceptionAssertions> HaveErrorCode(
        string errorCode,
        string because = "",
        params object[] becauseArgs)
    {
        string? actualCode = null;
        if (!string.IsNullOrEmpty(Subject.Response))
        {
            using var doc = JsonDocument.Parse(Subject.Response);
            if (doc.RootElement.TryGetProperty("code", out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                actualCode = prop.GetString();
            }
        }

        _chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualCode == errorCode)
            .FailWith(
                "Expected {context:ApiException} to have error code {0}{reason}, but found {1}.",
                errorCode,
                actualCode ?? "(none)");

        return new AndConstraint<ApiExceptionAssertions>(this);
    }
}

internal static class ApiExceptionAssertionExtensions
{
    internal static ApiExceptionAssertions Should(this ApiException instance)
        => new(instance, AssertionChain.GetOrCreate());

    [CustomAssertion]
    internal static async Task ThrowApiExceptionAsync<TTask, TAssertions>(
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
}
