using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Components.Pages;
using ClaudeMem.Admin.Ui.Tests.Infrastructure;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Pages;

public sealed class TeamsPageTests : UiTestContext
{
    private static readonly Uri BaseUri = new("https://api.example.com/teams?page_size=20");

    private static TeamPage EmptyTeamPage()
    {
        return new TeamPage(BaseUri, BaseUri, null, new List<Team>());
    }

    private static TeamPage TeamPageWithItems(IEnumerable<Team> items, Uri? next = null)
    {
        return new TeamPage(BaseUri, BaseUri, next, items);
    }

    [Fact]
    public void Api_Returns_Error_Shows_Error_Alert()
    {
        var teamsClient = SetupTeamsClient();
        teamsClient.TeamsGetAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                   .Returns<TeamPage>(_ => throw new ApiException("error", 500, null,
                       new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal), null));

        var teamsPage = Render<Teams>();

        teamsPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Api_Returns_Empty_List_Shows_No_Teams_Message()
    {
        var teamsClient = SetupTeamsClient();
        teamsClient.TeamsGetAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(EmptyTeamPage()));

        var teamsPage = Render<Teams>();

        teamsPage.Markup.Should().Contain("No teams yet");
    }

    [Fact]
    public void Api_Returns_Teams_Shows_Team_Names()
    {
        var teamsClient = SetupTeamsClient();
        var team = new Team("t1", "Alpha Team", DateTimeOffset.UtcNow, null);
        teamsClient.TeamsGetAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(TeamPageWithItems(new List<Team> { team })));

        var teamsPage = Render<Teams>();

        teamsPage.Markup.Should().Contain("Alpha Team");
    }

    [Fact]
    public void Api_Returns_Next_Shows_LoadMore_Button()
    {
        var teamsClient = SetupTeamsClient();
        var team = new Team("t1", "Alpha Team", DateTimeOffset.UtcNow, null);
        var nextPageUri = new Uri("https://api.example.com/teams?page_size=20&cursor=abc123");
        teamsClient.TeamsGetAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(TeamPageWithItems(new List<Team> { team }, next: nextPageUri)));

        var teamsPage = Render<Teams>();

        teamsPage.Markup.Should().Contain("Load more");
    }

    [Fact]
    public void Api_Returns_No_Next_Hides_LoadMore_Button()
    {
        var teamsClient = SetupTeamsClient();
        var team = new Team("t1", "Alpha Team", DateTimeOffset.UtcNow, null);
        teamsClient.TeamsGetAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(TeamPageWithItems(new List<Team> { team })));

        var teamsPage = Render<Teams>();

        teamsPage.Markup.Should().NotContain("Load more");
    }
}
