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
        var teamId = await _db.InsertTeamAsync();

        var act = async () => await _apiKeys.ApiKeysGetAsync(teamId, "no-such-project", cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, "/problems/project-team-mismatch");
    }

    [Fact]
    public async Task GetApiKeys_EmptyProject_ReturnsEmptyPage()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        var page = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetApiKeys_ExcludesRevokedKeys()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
        var activeKeyId = await _db.InsertApiKeyAsync(teamId, projectId, "active-actor");
        var revokedKeyId = await _db.InsertApiKeyAsync(teamId, projectId, "revoked-actor");
        await _db.RevokeApiKeyAsync(revokedKeyId);

        var page = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);

        var apiKey = page.Items.Should().ContainSingle().Subject;
        apiKey.Id.Should().Be(activeKeyId);
        apiKey.ActorId.Should().Be("active-actor");
    }

    [Fact]
    public async Task GetApiKeys_KeysOnlyReturnedForCorrectProject()
    {
        var teamId = await _db.InsertTeamAsync();
        var project1 = await _db.InsertProjectAsync(teamId, "P1");
        var project2 = await _db.InsertProjectAsync(teamId, "P2");
        await _db.InsertApiKeyAsync(teamId, project1, "actor-1");
        await _db.InsertApiKeyAsync(teamId, project2, "actor-2");

        var page = await _apiKeys.ApiKeysGetAsync(teamId, project1, cancellationToken: TestContext.Current.CancellationToken);

        var apiKey = page.Items.Should().ContainSingle().Subject;
        apiKey.ActorId.Should().Be("actor-1");
    }

    [Fact]
    public async Task GetApiKeys_ProjectInDifferentTeam_ReturnsProjectTeamMismatchProblemType()
    {
        var team1 = await _db.InsertTeamAsync("Team A");
        var team2 = await _db.InsertTeamAsync("Team B");
        var projectId = await _db.InsertProjectAsync(team1);

        var act = async () => await _apiKeys.ApiKeysGetAsync(team2, projectId, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, "/problems/project-team-mismatch");
    }

    [Fact]
    public async Task GetApiKeys_MultiPagePagination_LinksCorrect()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        for (var i = 0; i < 10; i++)
        {
            await _db.InsertApiKeyAsync(teamId, projectId, $"actor-{i:D2}");
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
        var response = await _fixture.GetUnauthenticatedAsync("/teams/some-team/projects/some-project/api-keys", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetApiKeys_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKeyAsync("/teams/some-team/projects/some-project/api-keys", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApiKey_ValidRequest_ReturnsKeyOnce()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        var created = await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "ci-pipeline", null, null), teamId, projectId, TestContext.Current.CancellationToken);

        created.ActorId.Should().Be("ci-pipeline");
        created.Key.Should().NotBeNullOrEmpty();
        created.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateApiKey_KeyHashedInDb_RawKeyNotStored()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

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
    public async Task CreateApiKey_NonExistentProject_ReturnsProjectNotFoundProblemType()
    {
        var teamId = await _db.InsertTeamAsync();

        var act = async () => await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "orphan-key", null, null), teamId, "no-such-project", TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ApiException<ProblemDetails>>();
        exception.Which.Should().HaveStatusCode(HttpStatusCode.NotFound);
        exception.Which.Result.Type.Should().Be("/problems/project-not-found");
    }

    [Fact]
    public async Task CreateApiKey_EmptyActorId_Returns422()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        var act = async () => await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, "", null, null), teamId, projectId, TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(257)]
    public async Task CreateApiKey_ActorIdTooLong_Returns422(int length)
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        var act = async () => await _apiKeys.ApiKeysPostAsync(new ApiKey(null, null, new string('a', length), null, null), teamId, projectId, TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    public async Task CreateApiKey_MinAndMaxLengthBoundaries_ReturnsCreatedApiKey(int length)
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
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
        var response = await _fixture.PostUnauthenticatedAsync("/teams/some-team/projects/some-project/api-keys", apiKey, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApiKey_WithWrongApiKey_Returns401()
    {
        var apiKey = new ApiKey(null, null, "test-actor", null, null);
        var response = await _fixture.PostWithWrongKeyAsync("/teams/some-team/projects/some-project/api-keys", apiKey, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteApiKey_ExistingKey_RevokesItAndReturns204()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
        var keyId = await _db.InsertApiKeyAsync(teamId, projectId);

        await _apiKeys.ApiKeysDeleteAsync(teamId, projectId, keyId, TestContext.Current.CancellationToken);

        var page = await _apiKeys.ApiKeysGetAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteApiKey_AlreadyRevoked_ReturnsApiKeyNotFoundProblemType()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
        var keyId = await _db.InsertApiKeyAsync(teamId, projectId);
        await _db.RevokeApiKeyAsync(keyId);

        var act = async () => await _apiKeys.ApiKeysDeleteAsync(teamId, projectId, keyId, TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, "/problems/api-key-not-found");
    }

    [Fact]
    public async Task DeleteApiKey_KeyInDifferentTeam_ReturnsApiKeyTeamMismatchProblemType()
    {
        var team1 = await _db.InsertTeamAsync("T1");
        var team2 = await _db.InsertTeamAsync("T2");
        var project1 = await _db.InsertProjectAsync(team1);
        var project2 = await _db.InsertProjectAsync(team2);
        var keyId = await _db.InsertApiKeyAsync(team1, project1);

        var act = async () => await _apiKeys.ApiKeysDeleteAsync(team2, project2, keyId, TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, "/problems/api-key-team-mismatch");
    }

    [Fact]
    public async Task DeleteApiKey_WithoutApiKey_Returns401()
    {
        var response = await _fixture.DeleteUnauthenticatedAsync("/teams/some-team/projects/some-project/api-keys/some-key", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteApiKey_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.DeleteWithWrongKeyAsync("/teams/some-team/projects/some-project/api-keys/some-key", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }
}
