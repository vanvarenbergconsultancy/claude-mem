using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.DeveloperEndpoints;

[Collection<AdminApiCollection>]
public sealed class DeveloperEndpointTests
{
    private readonly AdminApiFixture _fixture;

    public DeveloperEndpointTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOpenApiSpec_WithoutApiKey_Returns200WithYamlContent()
    {
        var response = await _fixture.GetUnauthenticated("/openapi/v1.yaml", TestContext.Current.CancellationToken);

        using var scope = new AssertionScope();
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/yaml");
    }

    [Fact]
    public async Task GetScalarUi_WithoutApiKey_Returns200WithHtmlContent()
    {
        var response = await _fixture.GetUnauthenticated("/scalar", TestContext.Current.CancellationToken);

        using var scope = new AssertionScope();
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }
}
