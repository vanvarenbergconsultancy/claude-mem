using System;
using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Features.AuditLog;

[Collection<AdminApiCollection>]
public sealed class AuditLogTests : IAsyncLifetime
{
    private const string EndpointAuditLog = "/audit-log";

    private readonly AdminApiFixture _fixture;
    private readonly DbHelper _db;
    private readonly IAuditLogClient _auditLogClient;

    public AuditLogTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
        _db = new DbHelper(fixture);
        _auditLogClient = fixture.AuditLogClient;
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetAuditLog_EmptyDatabase_ReturnsEmptyPage()
    {
        var page = await _auditLogClient.AuditLogAsync(cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldBeEmptyPage(page.Items);
    }

    [Fact]
    public async Task GetAuditLog_WithEntries_ReturnsAll()
    {
        await _db.InsertAuditLogEntry();
        await _db.InsertAuditLogEntry();

        var page = await _auditLogClient.AuditLogAsync(cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 2);
    }

    [Fact]
    public async Task GetAuditLog_FilterByTeamId_ReturnsOnlyMatchingEntries()
    {
        var team1 = await _db.InsertTeam("Team Alpha");
        var team2 = await _db.InsertTeam("Team Beta");
        await _db.InsertAuditLogEntry(teamId: team1);
        await _db.InsertAuditLogEntry(teamId: team2);

        var page = await _auditLogClient.AuditLogAsync(teamId: team1, cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        entry.TeamId.Should().Be(team1);
    }

    [Fact]
    public async Task GetAuditLog_FilterByProjectId_ReturnsOnlyMatchingEntries()
    {
        var teamId = await _db.InsertTeam();
        var project1 = await _db.InsertProject(teamId, "Project One");
        var project2 = await _db.InsertProject(teamId, "Project Two");
        await _db.InsertAuditLogEntry(teamId: teamId, projectId: project1);
        await _db.InsertAuditLogEntry(teamId: teamId, projectId: project2);

        var page = await _auditLogClient.AuditLogAsync(projectId: project1, cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        entry.ProjectId.Should().Be(project1);
    }

    [Fact]
    public async Task GetAuditLog_FilterByApiKeyId_ReturnsOnlyMatchingEntries()
    {
        var teamId = await _db.InsertTeam();
        var projectId = await _db.InsertProject(teamId);
        var apiKeyId = await _db.InsertApiKey(teamId, projectId, "test-actor");
        await _db.InsertAuditLogEntry(teamId: teamId, projectId: projectId, apiKeyId: apiKeyId);
        await _db.InsertAuditLogEntry(teamId: teamId, projectId: projectId);

        var page = await _auditLogClient.AuditLogAsync(apiKeyId: apiKeyId, cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        entry.ApiKeyId.Should().Be(apiKeyId);
    }

    [Fact]
    public async Task GetAuditLog_FilterByActorId_ReturnsOnlyMatchingEntries()
    {
        await _db.InsertAuditLogEntry(actorId: "actor-one");
        await _db.InsertAuditLogEntry(actorId: "actor-two");

        var page = await _auditLogClient.AuditLogAsync(actorId: "actor-one", cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        entry.ActorId.Should().Be("actor-one");
    }

    [Fact]
    public async Task GetAuditLog_FilterByAction_ReturnsOnlyMatchingEntries()
    {
        await _db.InsertAuditLogEntry(action: "api_key.create");
        await _db.InsertAuditLogEntry(action: "api_key.revoke");

        var page = await _auditLogClient.AuditLogAsync(action: "api_key.create", cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        entry.Action.Should().Be("api_key.create");
    }

    [Fact]
    public async Task GetAuditLog_FilterByResourceType_ReturnsOnlyMatchingEntries()
    {
        await _db.InsertAuditLogEntry(resourceType: "api_key");
        await _db.InsertAuditLogEntry(resourceType: "team");

        var page = await _auditLogClient.AuditLogAsync(resourceType: "team", cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        entry.ResourceType.Should().Be("team");
    }

    [Fact]
    public async Task GetAuditLog_FilterByDateRange_ReturnsMatchingEntries()
    {
        await _db.InsertAuditLogEntry();

        var page = await _auditLogClient.AuditLogAsync(
            from: DateTimeOffset.UtcNow.AddDays(-1),
            to: DateTimeOffset.UtcNow.AddDays(1),
            cancellationToken: TestContext.Current.CancellationToken);

        page.ShouldHaveItems(page.Items, 1);
    }

    [Fact]
    public async Task GetAuditLog_MultiPagePagination_LinksCorrect()
    {
        for (var i = 0; i < 10; i++)
        {
            await _db.InsertAuditLogEntry(actorId: $"actor-{i:D2}");
        }

        var p1 = await _auditLogClient.AuditLogAsync(pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 1"))
        {
            p1.ShouldHaveItems(p1.Items, 3);
            p1.ShouldBeFirstPage();
        }

        var p2 = await _auditLogClient.AuditLogAsync(cursor: p1.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 2"))
        {
            p2.ShouldHaveItems(p2.Items, 3);
            p2.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p3 = await _auditLogClient.AuditLogAsync(cursor: p2.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 3"))
        {
            p3.ShouldHaveItems(p3.Items, 3);
            p3.ShouldBeMidPage(expectedFirst: p1.First);
        }

        var p4 = await _auditLogClient.AuditLogAsync(cursor: p3.NextCursor(), pageSize: 3, cancellationToken: TestContext.Current.CancellationToken);
        using (new AssertionScope("page 4 (last)"))
        {
            p4.ShouldHaveItems(p4.Items, 1);
            p4.ShouldBeLastPage(expectedFirst: p1.First);
        }
    }

    [Fact]
    public async Task GetAuditLog_WithoutApiKey_Returns401()
    {
        var response = await _fixture.GetUnauthenticated(EndpointAuditLog, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLog_WithWrongApiKey_Returns401()
    {
        var response = await _fixture.GetWithWrongKey(EndpointAuditLog, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLog_ReturnsJoinedTeamAndProjectNames()
    {
        var teamId = await _db.InsertTeam("Acme Corp");
        var projectId = await _db.InsertProject(teamId, "Main Project");
        await _db.InsertAuditLogEntry(teamId: teamId, projectId: projectId);

        var page = await _auditLogClient.AuditLogAsync(cancellationToken: TestContext.Current.CancellationToken);

        var entry = page.Items.Should().ContainSingle().Subject;
        using var scope = new AssertionScope();
        entry.TeamName.Should().Be("Acme Corp");
        entry.ProjectName.Should().Be("Main Project");
    }
}
