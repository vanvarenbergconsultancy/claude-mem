using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal static class FixtureHttpExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task<HttpResponseMessage> GetUnauthenticatedAsync(
        this AdminApiFixture fixture, string path, CancellationToken cancellationToken)
    {
        using var client = fixture.CreateClient();
        return await client.GetAsync(path, cancellationToken);
    }

    public static async Task<HttpResponseMessage> GetWithWrongKeyAsync(
        this AdminApiFixture fixture, string path, CancellationToken cancellationToken)
    {
        using var client = CreateWrongKeyClient(fixture);
        return await client.GetAsync(path, cancellationToken);
    }

    public static async Task<HttpResponseMessage> PostUnauthenticatedAsync<TBody>(
        this AdminApiFixture fixture, string path, TBody body, CancellationToken cancellationToken)
    {
        using var client = fixture.CreateClient();
        return await client.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
    }

    public static async Task<HttpResponseMessage> PostWithWrongKeyAsync<TBody>(
        this AdminApiFixture fixture, string path, TBody body, CancellationToken cancellationToken)
    {
        using var client = CreateWrongKeyClient(fixture);
        return await client.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
    }

    public static async Task<HttpResponseMessage> PutUnauthenticatedAsync<TBody>(
        this AdminApiFixture fixture, string path, TBody body, CancellationToken cancellationToken)
    {
        using var client = fixture.CreateClient();
        return await client.PutAsJsonAsync(path, body, JsonOptions, cancellationToken);
    }

    public static async Task<HttpResponseMessage> PutWithWrongKeyAsync<TBody>(
        this AdminApiFixture fixture, string path, TBody body, CancellationToken cancellationToken)
    {
        using var client = CreateWrongKeyClient(fixture);
        return await client.PutAsJsonAsync(path, body, JsonOptions, cancellationToken);
    }

    private static HttpClient CreateWrongKeyClient(AdminApiFixture fixture)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        return client;
    }
}
