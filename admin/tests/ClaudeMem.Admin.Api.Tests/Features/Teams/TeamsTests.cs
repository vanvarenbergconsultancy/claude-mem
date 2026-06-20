using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.Teams;

[Collection<AdminApiCollection>]
public sealed class TeamsTests : IAsyncLifetime
{
    private readonly AdminApiFixture _fixture;
    private readonly DbHelper _db;
    private readonly ITeamsClient _teams;

    public TeamsTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
        _db = new DbHelper(fixture);
        _teams = fixture.TeamsClient;
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetTeams_EmptyDatabase_ReturnsEmptyPage()
    {
        var page = await _teams.TeamsGetAsync(cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetTeams_WithTeams_ReturnsTeams()
    {
        var id1 = await _db.InsertTeamAsync("Alpha");
        var id2 = await _db.InsertTeamAsync("Beta");

        var page = await _teams.TeamsGetAsync(cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 2);
        page.Items.Should().Contain(t => t.Id == id1 && t.Name == "Alpha");
        page.Items.Should().Contain(t => t.Id == id2 && t.Name == "Beta");
    }

    [Fact]
    public async Task GetTeams_MultiPagePagination_LinksCorrect()
    {
        for (var i = 0; i < 10; i++)
        {
            await _db.InsertTeamAsync($"Team {i:D2}");
        }

        var p1 = await _teams.TeamsGetAsync(pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 1"))
        {
            p1.ShouldHaveItems(p1.Items, 3);
            p1.ShouldBeFirstPage();
        }

        var p2 = await _teams.TeamsGetAsync(cursor: p1.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 2"))
        {
            p2.ShouldHaveItems(p2.Items, 3);
            p2.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p3 = await _teams.TeamsGetAsync(cursor: p2.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 3"))
        {
            p3.ShouldHaveItems(p3.Items, 3);
            p3.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p4 = await _teams.TeamsGetAsync(cursor: p3.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 4 (last)"))
        {
            p4.ShouldHaveItems(p4.Items, 1);
            p4.ShouldBeLastPage(expectedFirst: p1.First);
        }
    }

    [Fact]
    public async Task GetTeams_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticatedAsync("/teams", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTeams_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKeyAsync("/teams", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTeam_ExistingTeam_ReturnsTeamDetail()
    {
        var teamId = await _db.InsertTeamAsync("Detail Team");
        await _db.InsertProjectAsync(teamId, "Project A");

        var detail = await _teams.TeamsGetAsync(teamId, TestContext.Current.CancellationToken);

        detail.Should().NotBeNull();
        detail.Id.Should().Be(teamId);
        detail.Name.Should().Be("Detail Team");
        detail.ProjectCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTeam_NonExistentTeam_ReturnsTeamNotFoundErrorCode()
    {
        var act = async () => await _teams.TeamsGetAsync("does-not-exist", TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.Should()
            .HaveStatusCode(HttpStatusCode.NotFound)
            .And.HaveErrorCode("team_not_found");
    }

    [Fact]
    public async Task GetTeamById_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticatedAsync("/teams/some-id", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTeamById_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKeyAsync("/teams/some-id", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_ValidRequest_ReturnsCreatedTeam()
    {
        var detail = await _teams.TeamsPostAsync(new Team(null, "My New Team", null, null), TestContext.Current.CancellationToken);

        detail.Should().NotBeNull();
        detail.Name.Should().Be("My New Team");
        detail.Id.Should().NotBeNullOrEmpty();
        detail.ProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateTeam_EmptyName_Returns422()
    {
        var act = async () => await _teams.TeamsPostAsync(new Team(null, "", null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(257)]
    public async Task CreateTeam_NameLengthTooSmallOrTooLong_Returns422(int length)
    {
        var act = async () => await _teams.TeamsPostAsync(new Team(null, new string('A', length), null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(256)]
    public async Task CreateTeam_MinAndMaxLengthBoundaries_ReturnsCreatedTeam(int length)
    {
        var detail = await _teams.TeamsPostAsync(new Team(null, new string('A', length), null, null), TestContext.Current.CancellationToken);

        detail.Should().NotBeNull();
        detail.Name.Should().Be(new string('A', length));
        detail.Id.Should().NotBeNullOrEmpty();
        detail.ProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateTeam_WithoutApiKey_Returns401()
    {
        var team = new Team(null, "Test Team", null, null);
        var response = await _fixture.PostUnauthenticatedAsync("/teams", team, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_WithWrongApiKey_Returns401()
    {
        var team = new Team(null, "Test Team", null, null);
        var response = await _fixture.PostWithWrongKeyAsync("/teams", team, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }
}
