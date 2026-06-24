using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.Projects;

[Collection<AdminApiCollection>]
public sealed class ProjectsTests : IAsyncLifetime
{
    private static string EndpointProjectsByTeam(string teamId) => $"/teams/{teamId}/projects";
    private static string EndpointProjectById(string teamId, string projectId) => $"/teams/{teamId}/projects/{projectId}";

    private readonly AdminApiFixture _fixture;
    private readonly DbHelper _db;
    private readonly IProjectsClient _projects;

    public ProjectsTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
        _db = new DbHelper(fixture);
        _projects = fixture.ProjectsClient;
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetProjects_NonExistentTeam_ReturnsTeamNotFoundProblemType()
    {
        var act = async () => await _projects.ProjectsGetAsync("no-such-team", cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.TeamNotFound);
    }

    [Fact]
    public async Task GetProjects_TeamHasNoProjects_ReturnsEmptyPage()
    {
        var teamId = await _db.InsertTeamAsync();

        var page = await _projects.ProjectsGetAsync(teamId, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetProjects_WithProjects_ReturnsOnlyProjectsForThatTeam()
    {
        var team1 = await _db.InsertTeamAsync("Team 1");
        var team2 = await _db.InsertTeamAsync("Team 2");
        var proj1 = await _db.InsertProjectAsync(team1, "Project A");
        await _db.InsertProjectAsync(team2, "Project B");

        var page = await _projects.ProjectsGetAsync(team1, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 1);
        var project = page.Items.Should().ContainSingle().Subject;
        project.Id.Should().Be(proj1);
        project.TeamId.Should().Be(team1);
    }

    [Fact]
    public async Task GetProjects_MultiPagePagination_LinksCorrect()
    {
        var teamId = await _db.InsertTeamAsync();

        for (var i = 0; i < 10; i++)
        {
            await _db.InsertProjectAsync(teamId, $"Project {i:D2}");
        }

        var p1 = await _projects.ProjectsGetAsync(teamId, pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 1"))
        {
            p1.ShouldHaveItems(p1.Items, 3);
            p1.ShouldBeFirstPage();
        }

        var p2 = await _projects.ProjectsGetAsync(teamId, cursor: p1.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 2"))
        {
            p2.ShouldHaveItems(p2.Items, 3);
            p2.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p3 = await _projects.ProjectsGetAsync(teamId, cursor: p2.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 3"))
        {
            p3.ShouldHaveItems(p3.Items, 3);
            p3.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p4 = await _projects.ProjectsGetAsync(teamId, cursor: p3.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 4 (last)"))
        {
            p4.ShouldHaveItems(p4.Items, 1);
            p4.ShouldBeLastPage(expectedFirst: p1.First);
        }
    }

    [Fact]
    public async Task GetProjects_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticatedAsync(EndpointProjectsByTeam("some-team"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKeyAsync(EndpointProjectsByTeam("some-team"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProject_ExistingProject_ReturnsDetail()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId, "Detailed Project");
        await _db.InsertApiKeyAsync(teamId, projectId);
        await _db.InsertObservationAsync(teamId, projectId);

        var project = await _projects.ProjectsGetAsync(teamId, projectId, TestContext.Current.CancellationToken);

        project.Id.Should().Be(projectId);
        project.Name.Should().Be("Detailed Project");
        project.ApiKeyCount.Should().Be(1);
        project.ObservationCount.Should().Be(1);
    }

    [Fact]
    public async Task GetProject_ProjectInDifferentTeam_ReturnsProjectTeamMismatchProblemType()
    {
        var team1 = await _db.InsertTeamAsync("Team 1");
        var team2 = await _db.InsertTeamAsync("Team 2");
        var projectId = await _db.InsertProjectAsync(team1);

        var act = async () => await _projects.ProjectsGetAsync(team2, projectId, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ApiException<ProblemDetails>>();
        exception.Which.Should().HaveStatusCode(HttpStatusCode.NotFound);
        exception.Which.Result.Type.Should().Be(Constants.ProblemTypes.ProjectTeamMismatch);
    }

    [Fact]
    public async Task GetProjectById_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticatedAsync(EndpointProjectById("some-team", "some-id"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectById_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKeyAsync(EndpointProjectById("some-team", "some-id"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_ValidRequest_ReturnsCreatedProject()
    {
        var teamId = await _db.InsertTeamAsync();

        var project = await _projects.ProjectsPostAsync(new Project(null, null, "My Project", null, null, null), teamId, TestContext.Current.CancellationToken);

        project.Should().NotBeNull();
        project.Name.Should().Be("My Project");
        project.TeamId.Should().Be(teamId);
        project.ApiKeyCount.Should().Be(0);
        project.ObservationCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProject_NonExistentTeam_ReturnsTeamNotFoundProblemType()
    {
        var act = async () => await _projects.ProjectsPostAsync(new Project(null, null, "Orphan Project", null, null, null), "no-such-team", TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ApiException<ProblemDetails>>();
        exception.Which.Should().HaveStatusCode(HttpStatusCode.NotFound);
        exception.Which.Result.Type.Should().Be(Constants.ProblemTypes.TeamNotFound);
    }

    [Fact]
    public async Task CreateProject_EmptyName_Returns422()
    {
        var teamId = await _db.InsertTeamAsync();

        var act = async () => await _projects.ProjectsPostAsync(new Project(null, null, "", null, null, null), teamId, TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(257)]
    public async Task CreateProject_NameLengthTooSmallOrTooLong_Returns422(int length)
    {
        var teamId = await _db.InsertTeamAsync();

        var act = async () => await _projects.ProjectsPostAsync(new Project(null, null, new string('A', length), null, null, null), teamId, TestContext.Current.CancellationToken);

        await act.Should().ThrowApiExceptionAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(256)]
    public async Task CreateProject_MinAndMaxLengthBoundaries_ReturnsCreatedProject(int length)
    {
        var teamId = await _db.InsertTeamAsync();

        var project = await _projects.ProjectsPostAsync(new Project(null, null, new string('A', length), null, null, null), teamId, TestContext.Current.CancellationToken);

        project.Should().NotBeNull();
        project.Name.Should().Be(new string('A', length));
        project.TeamId.Should().Be(teamId);
        project.ApiKeyCount.Should().Be(0);
        project.ObservationCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProject_WithoutApiKey_Returns401()
    {
        var project = new Project(null, null, "Test Project", null, null, null);
        var response = await _fixture.PostUnauthenticatedAsync(EndpointProjectsByTeam("some-team"), project, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_WithWrongApiKey_Returns401()
    {
        var project = new Project(null, null, "Test Project", null, null, null);
        var response = await _fixture.PostWithWrongKeyAsync(EndpointProjectsByTeam("some-team"), project, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProject_RevokedApiKeysNotCounted()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
        var keyId = await _db.InsertApiKeyAsync(teamId, projectId);
        await _db.RevokeApiKeyAsync(keyId);

        var project = await _projects.ProjectsGetAsync(teamId, projectId, TestContext.Current.CancellationToken);

        project.Should().NotBeNull();
        project.ApiKeyCount.Should().Be(0);
    }
}
