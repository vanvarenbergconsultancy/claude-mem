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

public sealed class TeamCacheServiceTests
{
    private static readonly Uri FakeUri = new("http://test");

    private static TeamPage MakePage(params Team[] teams)
    {
        return new TeamPage(FakeUri, FakeUri, null, teams);
    }

    private static Team MakeTeam(string id, string name)
    {
        return new Team(id, name, null, null);
    }

    private static IServiceScopeFactory CreateScopeFactory(ITeamsClient client)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        serviceProvider.GetService(typeof(ITeamsClient)).Returns(client);
        scope.ServiceProvider.Returns(serviceProvider);
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }

    private static (TeamCacheService service, ITeamsClient client, ICacheInvalidationBus bus) CreateService()
    {
        var client = Substitute.For<ITeamsClient>();
        var bus = Substitute.For<ICacheInvalidationBus>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var scopeFactory = CreateScopeFactory(client);
        var service = new TeamCacheService(cache, scopeFactory, bus, NullLogger<TeamCacheService>.Instance);

        return (service, client, bus);
    }

    private static void ArrangeClient(ITeamsClient client, TeamPage page)
    {
        client.TeamsGetAsync(cursor: Arg.Any<string?>(), pageSize: Arg.Any<int?>(), cancellationToken: Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(page));
    }

    private static void AssertClientTeamsGetAsyncCalled(ITeamsClient client, int amount)
    {
        _ = client.Received(amount).TeamsGetAsync(cursor: Arg.Any<string?>(), pageSize: Arg.Any<int?>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_CacheMiss_CallsClient()
    {
        var (service, client, _) = CreateService();
        var pageReturnedByClient = MakePage();
        ArrangeClient(client, pageReturnedByClient);

        await service.GetAll(TestContext.Current.CancellationToken);

        AssertClientTeamsGetAsyncCalled(client, 1);
    }

    [Fact]
    public async Task GetAll_CacheMiss_ReturnsClientResult()
    {
        var (service, client, _) = CreateService();
        var team = MakeTeam("t1", "Alpha");
        ArrangeClient(client, MakePage(team));

        var result = await service.GetAll(TestContext.Current.CancellationToken);

        result.Should().NotBeNullOrEmpty();
        result.Should().ContainSingle(t => t.Id == team.Id);
    }

    [Fact]
    public async Task GetAll_SecondCall_UsesCacheAndDoesNotCallClientAgain()
    {
        var (service, client, _) = CreateService();
        var team = MakeTeam("t1", "Alpha");
        ArrangeClient(client, MakePage(team));

        await service.GetAll(TestContext.Current.CancellationToken);
        await service.GetAll(TestContext.Current.CancellationToken);

        AssertClientTeamsGetAsyncCalled(client, 1);
    }

    [Fact]
    public async Task Invalidate_AfterPopulation_CausesNextGetAllToCallClientAgain()
    {
        var (service, client, _) = CreateService();
        var team = MakeTeam("t1", "Alpha");
        ArrangeClient(client, MakePage(team));

        await service.GetAll(TestContext.Current.CancellationToken);
        service.Invalidate(TestContext.Current.CancellationToken);
        await service.GetAll(TestContext.Current.CancellationToken);

        AssertClientTeamsGetAsyncCalled(client, 2);
    }

    [Fact]
    public void Invalidate_NotifiesBusWithTeamsAllKey()
    {
        var (service, _, bus) = CreateService();

        service.Invalidate(TestContext.Current.CancellationToken);

        _ = bus.Received(1).Notify(CacheKeys.TeamsAll, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_ResultsReturnedAlphabeticallySortedByName()
    {
        var (service, client, _) = CreateService();
        var team1 = MakeTeam("t1", "Alpha");
        var team2 = MakeTeam("t2", "Zebra");
        ArrangeClient(client, MakePage(team1, team2));

        var result = await service.GetAll(TestContext.Current.CancellationToken);

        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be(team1.Name);
        result[1].Name.Should().Be(team2.Name);
    }
}
