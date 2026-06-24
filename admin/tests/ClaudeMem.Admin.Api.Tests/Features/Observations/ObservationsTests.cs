using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.Observations;

[Collection<AdminApiCollection>]
public sealed class ObservationsTests : IAsyncLifetime
{
    private static string EndpointObservationsByProject(string teamId, string projectId) => $"/teams/{teamId}/projects/{projectId}/observations";

    private readonly AdminApiFixture _fixture;
    private readonly DbHelper _db;
    private readonly IObservationsClient _observations;

    public ObservationsTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
        _db = new DbHelper(fixture);
        _observations = fixture.ObservationsClient;
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetObservations_NonExistentProject_ReturnsProjectTeamMismatchProblemType()
    {
        var teamId = await _db.InsertTeamAsync();

        var act = async () => await _observations.ObservationsAsync(teamId, "no-project", cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ProjectTeamMismatch);
    }

    [Fact]
    public async Task GetObservations_EmptyProject_ReturnsEmptyPage()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        var page = await _observations.ObservationsAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetObservations_WithObservations_ReturnsThemInDescendingOrder()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
        var id1 = await _db.InsertObservationAsync(teamId, projectId, "First observation");
        var id2 = await _db.InsertObservationAsync(teamId, projectId, "Second observation");

        var page = await _observations.ObservationsAsync(teamId, projectId, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 2);
        page.Items.ElementAt(0).Id.Should().Be(id2);
        page.Items.ElementAt(1).Id.Should().Be(id1);
    }

    [Fact]
    public async Task GetObservations_OnlyReturnsObservationsForCorrectProject()
    {
        var teamId = await _db.InsertTeamAsync();
        var project1 = await _db.InsertProjectAsync(teamId, "P1");
        var project2 = await _db.InsertProjectAsync(teamId, "P2");
        var obsId = await _db.InsertObservationAsync(teamId, project1, "Project 1 obs");
        await _db.InsertObservationAsync(teamId, project2, "Project 2 obs");

        var page = await _observations.ObservationsAsync(teamId, project1, cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 1);
        var observation = page.Items.Should().ContainSingle().Subject;
        observation.Id.Should().Be(obsId);
    }

    [Fact]
    public async Task GetObservations_ProjectInDifferentTeam_ReturnsProjectTeamMismatchProblemType()
    {
        var team1 = await _db.InsertTeamAsync("T1");
        var team2 = await _db.InsertTeamAsync("T2");
        var projectId = await _db.InsertProjectAsync(team1);

        var act = async () => await _observations.ObservationsAsync(team2, projectId, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.NotFound, Constants.ProblemTypes.ProjectTeamMismatch);
    }

    [Fact]
    public async Task GetObservations_WithSearchQuery_FiltersResults()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);
        await _db.InsertObservationAsync(teamId, projectId, "Learned about PostgreSQL indexing strategies.");
        await _db.InsertObservationAsync(teamId, projectId, "Refactored the authentication middleware.");

        var page = await _observations.ObservationsAsync(teamId, projectId, q: "PostgreSQL", cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 1);
        var observation = page.Items.Should().ContainSingle().Subject;
        observation.Content.Should().Contain("PostgreSQL");
    }

    [Fact]
    public async Task GetObservations_MultiPagePagination_LinksCorrect()
    {
        var teamId = await _db.InsertTeamAsync();
        var projectId = await _db.InsertProjectAsync(teamId);

        for (var i = 0; i < 10; i++)
        {
            await _db.InsertObservationAsync(teamId, projectId, $"Observation number {i}.");
        }

        var p1 = await _observations.ObservationsAsync(teamId, projectId, pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 1"))
        {
            p1.ShouldHaveItems(p1.Items, 3);
            p1.ShouldBeFirstPage();
        }

        var p2 = await _observations.ObservationsAsync(teamId, projectId, cursor: p1.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 2"))
        {
            p2.ShouldHaveItems(p2.Items, 3);
            p2.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p3 = await _observations.ObservationsAsync(teamId, projectId, cursor: p2.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 3"))
        {
            p3.ShouldHaveItems(p3.Items, 3);
            p3.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p4 = await _observations.ObservationsAsync(teamId, projectId, cursor: p3.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 4 (last)"))
        {
            p4.ShouldHaveItems(p4.Items, 1);
            p4.ShouldBeLastPage(expectedFirst: p1.First);
        }
    }

    [Fact]
    public async Task GetObservations_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticatedAsync(EndpointObservationsByProject("some-team", "some-project"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetObservations_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKeyAsync(EndpointObservationsByProject("some-team", "some-project"), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }
}
