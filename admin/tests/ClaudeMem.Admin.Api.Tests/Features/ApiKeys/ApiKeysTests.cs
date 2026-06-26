using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using Dapper;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.ApiKeys;

[Collection<AdminApiCollection>]
public sealed class ApiKeysTests : IAsyncLifetime
{
    private static string EndpointApiKeysByProject(string teamId, string projectId) => $"/teams/{teamId}/projects/{projectId}/api-keys";
    private static string EndpointApiKeyById(string teamId, string projectId, string keyId) => $"/teams/{teamId}/projects/{projectId}/api-keys/{keyId}";

    private readonly AdminApiFixture _fixture;
    private readonly DbHelper _db;
    private readonly IApiKeysClient _apiKeys;

    public ApiKeysTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
        _db = new DbHelper(fixture);
        _apiKeys = fixture.ApiKeysClient;
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetApiKeys_NonExistentProject_ReturnsProjectTeamMismatchProblemType()
    {
        var teamId = await _db.InsertTeam();

        var act = async () => await _apiKeys.ApiKeysGetAsync(teamId, "no-such-project", cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ProjectTeamMismatch);
    }

    [Fact]
    public async Task GetApiKeys_EmptyProject_ReturnsEmptyPage()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        var page = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetApiKeys_ExcludesRevokedKeys()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var activeKeyId = await _db.InsertApiKey(teamId, projectId, "active-actor");
        var revokedKeyId = await _db.InsertApiKey(teamId, projectId, "revoked-actor");
        await _db.RevokeApiKey(revokedKeyId);

        var page = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);

        var apiKey = page.Items.Should().ContainSingle().Subject;
        apiKey.Id.Should().Be(activeKeyId);
        apiKey.ActorId.Should().Be("active-actor");
    }

    [Fact]
    public async Task GetApiKeys_KeysOnlyReturnedForCorrectProject()
    {
        var teamId = await _db.InsertTeam();
        var project1 = await _db.InsertProject(teamId, "P1");
        var project2 = await _db.InsertProject(teamId, "P2");
        await _db.InsertApiKey(teamId, project1, "actor-1");
        await _db.InsertApiKey(teamId, project2, "actor-2");

        var page = await _apiKeys.ApiKeysGetAsync(teamId, project1, cancellationToken: TestContext.Current.CancellationToken);

        var apiKey = page.Items.Should().ContainSingle().Subject;
        apiKey.ActorId.Should().Be("actor-1");
    }

    [Fact]
    public async Task GetApiKeys_ProjectInDifferentTeam_ReturnsProjectTeamMismatchProblemType()
    {
        var team1 = await _db.InsertTeam("Team A");
        var team2 = await _db.InsertTeam("Team B");
        var projectId = await _db.InsertProject(team1);

        var act = async () => await _apiKeys.ApiKeysGetAsync(team2, projectId, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ProjectTeamMismatch);
    }

    [Fact]
    public async Task GetApiKeys_MultiPagePagination_LinksCorrect()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        for (var i = 0; i < 10; i++)
        {
            await _db.InsertApiKey(teamId, projectId, $"actor-{i:D2}");
        }

        var p1 = await _apiKeys.ApiKeysGetAsync(teamId, projectId, pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);

        using (new AssertionScope("page 1"))
        {
            p1.ShouldHaveItems(p1.Items, 3);
            p1.ShouldBeFirstPage();
        }

        var p2 = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cursor: p1.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);

        using (new AssertionScope("page 2"))
        {
            p2.ShouldHaveItems(p2.Items, 3);
            p2.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p3 = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cursor: p2.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);

        using (new AssertionScope("page 3"))
        {
            p3.ShouldHaveItems(p3.Items, 3);
            p3.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p4 = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cursor: p3.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);

        using (new AssertionScope("page 4 (last)"))
        {
            p4.ShouldHaveItems(p4.Items, 1);
            p4.ShouldBeLastPage(expectedFirst: p1.First);
        }
    }

    [Fact]
    public async Task GetApiKeys_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticated(EndpointApiKeysByProject("some-team", "some-project"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetApiKeys_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKey(EndpointApiKeysByProject("some-team", "some-project"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApiKey_ValidRequest_ReturnsKeyOnce()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        var created = await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "ci-pipeline", null, null), teamId, projectId, TestContext.Current.CancellationToken);

        created.ActorId.Should().Be("ci-pipeline");
        created.Key.Should().NotBeNullOrEmpty();
        created.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateApiKey_KeyHashedInDb_RawKeyNotStored()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        var created = await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "verify-hash", null, null), teamId, projectId, TestContext.Current.CancellationToken);

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var keyHash = await connection.QuerySingleAsync<string>(
            "SELECT key_hash FROM api_keys WHERE id = @Id",
            new { Id = created.Id });

        keyHash.Should().NotBe(created.Key);
        keyHash.Should().HaveLength(64);
    }

    [Fact]
    public async Task CreateApiKey_NonExistentProject_ReturnsProjectTeamMismatchProblemType()
    {
        var teamId = await _db.InsertTeam();

        var act = async () => await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "orphan-key", null, null), teamId, "no-such-project", TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ProjectTeamMismatch);
    }

    [Fact]
    public async Task CreateApiKey_EmptyActorId_Returns422()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        var act = async () => await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "", null, null), teamId, projectId, TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(257)]
    public async Task CreateApiKey_ActorIdTooLong_Returns422(int length)
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        var act = async () => await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, new string('a', length), null, null), teamId, projectId, TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    public async Task CreateApiKey_MinAndMaxLengthBoundaries_ReturnsCreatedApiKey(int length)
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var actorId = new string('a', length);

        var created = await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, actorId, null, null), teamId, projectId, TestContext.Current.CancellationToken);

        created.Should().NotBeNull();
        created.ActorId.Should().Be(actorId);
        created.Key.Should().NotBeNullOrEmpty();
        created.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateApiKey_WithoutApiKey_Returns401()
    {
        var apiKey = new ApiKey(null, null, "test-actor", null, null);
        var response = await _fixture.PostUnauthenticated(EndpointApiKeysByProject("some-team", "some-project"), apiKey, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApiKey_WithWrongApiKey_Returns401()
    {
        var apiKey = new ApiKey(null, null, "test-actor", null, null);
        var response = await _fixture.PostWithWrongKey(EndpointApiKeysByProject("some-team", "some-project"), apiKey, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteApiKey_ExistingKey_RevokesItAndReturns204()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var keyId = await _db.InsertApiKey(teamId, projectId);

        await _apiKeys.ApiKeysDeleteAsync(teamId, projectId, keyId, TestContext.Current.CancellationToken);

        var page = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteApiKey_AlreadyRevoked_ReturnsApiKeyNotFoundProblemType()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var keyId = await _db.InsertApiKey(teamId, projectId);
        await _db.RevokeApiKey(keyId);

        var act = async () => await _apiKeys.ApiKeysDeleteAsync(teamId, projectId, keyId, TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ApiKeyNotFound);
    }

    [Fact]
    public async Task DeleteApiKey_KeyInDifferentTeam_ReturnsApiKeyNotFoundProblemType()
    {
        var team1 = await _db.InsertTeam("T1");
        var team2 = await _db.InsertTeam("T2");
        var project1 = await _db.InsertProject(team1);
        var project2 = await _db.InsertProject(team2);
        var keyId = await _db.InsertApiKey(team1, project1);

        var act = async () => await _apiKeys.ApiKeysDeleteAsync(team2, project2, keyId, TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ApiKeyNotFound);
    }

    [Fact]
    public async Task DeleteApiKey_WithoutApiKey_Returns401()
    {
        var response = await _fixture.DeleteUnauthenticated(EndpointApiKeyById("some-team", "some-project", "some-key"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteApiKey_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.DeleteWithWrongKey(EndpointApiKeyById("some-team", "some-project", "some-key"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }
}
