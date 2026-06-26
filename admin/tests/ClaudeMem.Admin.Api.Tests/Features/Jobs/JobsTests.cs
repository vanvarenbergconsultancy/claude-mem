using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.Jobs;

[Collection<AdminApiCollection>]
public sealed class JobsTests : IAsyncLifetime
{
    private const string EndpointJobs = "/jobs";
    private static string EndpointJobRetry(string id) => $"/jobs/{id}/retry";

    private readonly AdminApiFixture _fixture;
    private readonly DbHelper _db;
    private readonly IJobsClient _jobs;

    public JobsTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
        _db = new DbHelper(fixture);
        _jobs = fixture.JobsClient;
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetJobs_EmptyDatabase_ReturnsEmptyPage()
    {
        var page = await _jobs.JobsAsync(cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetJobs_WithJobs_ReturnsAll()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        await _db.InsertJob(teamId, projectId, "queued");
        await _db.InsertJob(teamId, projectId, "failed");

        var page = await _jobs.JobsAsync(cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 2);
    }

    [Fact]
    public async Task GetJobs_FilterByStatus_ReturnsMatchingJobs()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var failedId = await _db.InsertJob(teamId, projectId, "failed");
        await _db.InsertJob(teamId, projectId, "completed");

        var page = await _jobs.JobsAsync(status: "failed", cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 1);
        var job = page.Items.Should().ContainSingle().Subject;
        job.Id.Should().Be(failedId);
        job.Status.Should().Be("failed");
    }

    [Fact]
    public async Task GetJobs_FilterByProjectId_ReturnsOnlyThatProjectsJobs()
    {
        var teamId = await _db.InsertTeam();
        var project1 = await _db.InsertProject(teamId, "P1");
        var project2 = await _db.InsertProject(teamId, "P2");
        var job1Id = await _db.InsertJob(teamId, project1, "queued");
        await _db.InsertJob(teamId, project2, "queued");

        var page = await _jobs.JobsAsync(projectId: project1, cancellationToken: TestContext.Current.CancellationToken);

        var job = page.Items.Should().ContainSingle().Subject;
        job.Id.Should().Be(job1Id);
    }

    [Fact]
    public async Task GetJobs_MultiPagePagination_LinksCorrect()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);

        for (var i = 0; i < 10; i++)
        {
            await _db.InsertJob(teamId, projectId, "completed");
        }

        var p1 = await _jobs.JobsAsync(pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 1"))
        {
            p1.ShouldHaveItems(p1.Items, 3);
            p1.ShouldBeFirstPage();
        }

        var p2 = await _jobs.JobsAsync(cursor: p1.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 2"))
        {
            p2.ShouldHaveItems(p2.Items, 3);
            p2.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p3 = await _jobs.JobsAsync(cursor: p2.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 3"))
        {
            p3.ShouldHaveItems(p3.Items, 3);
            p3.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p4 = await _jobs.JobsAsync(cursor: p3.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 4 (last)"))
        {
            p4.ShouldHaveItems(p4.Items, 1);
            p4.ShouldBeLastPage(expectedFirst: p1.First);
        }
    }

    [Fact]
    public async Task GetJobs_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticated(EndpointJobs, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetJobs_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKey(EndpointJobs, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RetryJob_FailedJob_ResetsStatusToQueued()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var jobId = await _db.InsertJob(teamId, projectId, "failed");

        await _jobs.RetryAsync(jobId, TestContext.Current.CancellationToken);

        var page = await _jobs.JobsAsync(projectId: projectId, status: "queued", cancellationToken: TestContext.Current.CancellationToken);
        var job = page.Items.Should().ContainSingle().Subject;
        job.Id.Should().Be(jobId);
    }

    [Fact]
    public async Task RetryJob_NonExistentJob_ReturnsJobNotFoundProblemType()
    {
        var act = async () => await _jobs.RetryAsync("does-not-exist", TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ApiException<ProblemDetails>>();
        exception.Which.Should().HaveStatusCode(HttpStatusCode.NotFound);
        exception.Which.Result.Type.Should().Be(Constants.ProblemTypes.JobNotFound);
    }

    [Fact]
    public async Task RetryJob_NotFailedJob_ReturnsJobNotInFailedStateProblemType()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var jobId = await _db.InsertJob(teamId, projectId, "completed");

        var act = async () => await _jobs.RetryAsync(jobId, TestContext.Current.CancellationToken);

        await act.Should().ThrowProblemDetailsAsync(HttpStatusCode.Conflict, Constants.ProblemTypes.JobNotInFailedState);
    }

    [Fact]
    public async Task RetryJob_WithoutApiKey_Returns401()
    {
        var response = await _fixture.PostUnauthenticated(EndpointJobRetry("some-id"), new { }, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RetryJob_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.PostWithWrongKey(EndpointJobRetry("some-id"), new { }, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }
}
