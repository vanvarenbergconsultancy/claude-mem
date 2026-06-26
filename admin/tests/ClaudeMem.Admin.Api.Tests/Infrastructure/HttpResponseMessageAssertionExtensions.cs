using System.Net.Http;
using AwesomeAssertions.Execution;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal static class HttpResponseMessageAssertionExtensions
{
    internal static HttpResponseMessageAssertions Should(this HttpResponseMessage instance)
        => new(instance, AssertionChain.GetOrCreate());
}
