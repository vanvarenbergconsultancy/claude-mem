using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Unit;

public sealed class ProjectCacheServiceTests
{
    private static readonly Uri FakeUri = new("http://test");
    private const string TeamId = "team-1";

    private static ProjectPage MakePage(params Project[] projects)
    {
        return new ProjectPage(FakeUri, FakeUri, null, projects);
    }

    private static Project MakeProject(string id, string name)
    {
        return new Project(id, TeamId, name, null, null, null);
    }

    private static IServiceScopeFactory CreateScopeFactory(IProjectsClient client)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        serviceProvider.GetService(typeof(IProjectsClient)).Returns(client);
        scope.ServiceProvider.Returns(serviceProvider);
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }

    private static (ProjectCacheService service, IProjectsClient client, ICacheInvalidationBus bus) CreateService()
    {
        var client = Substitute.For<IProjectsClient>();
        var bus = Substitute.For<ICacheInvalidationBus>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var scopeFactory = CreateScopeFactory(client);
        var service = new ProjectCacheService(cache, scopeFactory, bus, NullLogger<ProjectCacheService>.Instance);

        return (service, client, bus);
    }

    private static void ArrangeClient(IProjectsClient client, ProjectPage page, string teamId = TeamId)
    {
        client.ProjectsGetAsync(teamId, cursor: Arg.Any<string?>(), pageSize: Arg.Any<int?>(), cancellationToken: Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(page));
    }

    private static void AssertClientProjectsGetAsyncCalled(IProjectsClient client, int amount, string teamId = TeamId)
    {
        _ = client.Received(amount).ProjectsGetAsync(teamId, cursor: Arg.Any<string?>(), pageSize: Arg.Any<int?>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetForTeam_CacheMiss_CallsClient()
    {
        var (service, client, _) = CreateService();
        var pageReturnedByClient = MakePage();
        ArrangeClient(client, pageReturnedByClient);

        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);

        AssertClientProjectsGetAsyncCalled(client, 1);
    }

    [Fact]
    public async Task GetForTeam_CacheMiss_ReturnsClientResult()
    {
        var (service, client, _) = CreateService();
        var project = MakeProject("p1", "ProjectAlpha");
        ArrangeClient(client, MakePage(project));

        var result = await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);

        result.Should().NotBeNullOrEmpty();
        result.Should().ContainSingle(p => p.Id == project.Id);
    }

    [Fact]
    public async Task GetForTeam_SecondCall_UsesCacheAndDoesNotCallClientAgain()
    {
        var (service, client, _) = CreateService();
        var project = MakeProject("p1", "Alpha");
        ArrangeClient(client, MakePage(project));

        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);
        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);

        AssertClientProjectsGetAsyncCalled(client, 1);
    }

    [Fact]
    public async Task InvalidateForTeam_CausesNextGetForTeamToCallClientAgain()
    {
        var (service, client, _) = CreateService();
        var project = MakeProject("p1", "Alpha");
        ArrangeClient(client, MakePage(project));

        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);
        service.InvalidateForTeam(TeamId, TestContext.Current.CancellationToken);
        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);

        AssertClientProjectsGetAsyncCalled(client, 2);
    }

    [Fact]
    public void InvalidateForTeam_NotifiesBusWithProjectKey()
    {
        var (service, _, bus) = CreateService();

        service.InvalidateForTeam(TeamId, TestContext.Current.CancellationToken);

        _ = bus.Received(1).Notify(CacheKeys.ProjectsForTeam(TeamId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAll_CausesNextGetForTeamToCallClientAgainForEachTeam()
    {
        var (service, client, _) = CreateService();
        const string teamId2 = "team-2";
        ArrangeClient(client, MakePage());
        ArrangeClient(client, MakePage(), teamId2);

        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);
        await service.GetForTeam(teamId2, TestContext.Current.CancellationToken);
        service.InvalidateAll(TestContext.Current.CancellationToken);
        await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);
        await service.GetForTeam(teamId2, TestContext.Current.CancellationToken);

        _ = client.Received(4).ProjectsGetAsync(Arg.Any<string>(), cursor: Arg.Any<string?>(), pageSize: Arg.Any<int?>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public void InvalidateAll_NotifiesBusWithWildcardKey()
    {
        var (service, _, bus) = CreateService();

        service.InvalidateAll(TestContext.Current.CancellationToken);

        _ = bus.Received(1).Notify("projects:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetForTeam_ResultsReturnedAlphabeticallySortedByName()
    {
        var (service, client, _) = CreateService();
        var project1 = MakeProject("p1", "Alpha");
        var project2 = MakeProject("p2", "Zebra");
        ArrangeClient(client, MakePage(project1, project2));

        var result = await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);

        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be(project1.Name);
        result[1].Name.Should().Be(project2.Name);
    }

    [Fact]
    public async Task GetForTeam_DifferentTeams_CachedIndependently()
    {
        var (service, client, _) = CreateService();
        const string teamId2 = "team-2";
        var project1 = MakeProject("p1", "TeamOneProject");
        ArrangeClient(client, MakePage(project1));

        var project2 = MakeProject("p2", "TeamTwoProject");
        ArrangeClient(client, MakePage(project2), teamId2);

        var result1 = await service.GetForTeam(TeamId, TestContext.Current.CancellationToken);
        var result2 = await service.GetForTeam(teamId2, TestContext.Current.CancellationToken);

        result1.Should().NotBeNullOrEmpty();
        result1.Should().ContainSingle(p => p.Name == project1.Name);
        result2.Should().NotBeNullOrEmpty();
        result2.Should().ContainSingle(p => p.Name == project2.Name);
    }
}
