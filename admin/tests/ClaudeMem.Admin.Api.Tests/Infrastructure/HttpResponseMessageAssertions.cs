using System.Net;
using System.Net.Http;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal sealed class HttpResponseMessageAssertions
    : ReferenceTypeAssertions<HttpResponseMessage, HttpResponseMessageAssertions>
{
    private readonly AssertionChain _chain;

    internal HttpResponseMessageAssertions(HttpResponseMessage instance, AssertionChain chain)
        : base(instance, chain)
    {
        _chain = chain;
    }

    protected override string Identifier => "response";

    public AndConstraint<HttpResponseMessageAssertions> HaveStatusCode(
        HttpStatusCode statusCode,
        string because = "",
        params object[] becauseArgs)
    {
        _chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.StatusCode == statusCode)
            .FailWith(
                "Expected {context:response} to have status code {0}{reason}, but found {1}.",
                statusCode,
                Subject.StatusCode);

        return new AndConstraint<HttpResponseMessageAssertions>(this);
    }
}

internal static class HttpResponseMessageAssertionExtensions
{
    internal static HttpResponseMessageAssertions Should(this HttpResponseMessage instance)
        => new(instance, AssertionChain.GetOrCreate());
}
