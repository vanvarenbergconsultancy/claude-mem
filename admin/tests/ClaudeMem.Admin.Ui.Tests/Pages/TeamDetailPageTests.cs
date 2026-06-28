using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Components.Dialogs;
using ClaudeMem.Admin.Ui.Components.Pages;
using ClaudeMem.Admin.Ui.Services;
using ClaudeMem.Admin.Ui.Tests.Infrastructure;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Pages;

public sealed class TeamDetailPageTests : UiTestContext
{
    private const string TestTeamId = "team-1";
    private static readonly Uri BaseUri = new("https://api.example.com/teams/team-1/projects?page_size=20");

    private static Team ATeam()
    {
        return new Team(TestTeamId, "Alpha Team", DateTimeOffset.UtcNow, null);
    }

    private static ProjectPage EmptyProjectPage()
    {
        return new ProjectPage(BaseUri, BaseUri, null, new List<Project>());
    }

    private static ApiException ServerError()
    {
        return new ApiException("error", 500, null, new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal), null);
    }

    private (ITeamsClient teamsClient, IProjectsClient projectsClient, IProjectCacheService projectCacheService) Setup()
    {
        var projectCacheService = SetupProjectCacheService();
        var teamsClient = SetupTeamsClient();
        var projectsClient = SetupProjectsClient();
        return (teamsClient, projectsClient, projectCacheService);
    }

    private static void SetupSuccessfulProjectsList(IProjectsClient projectsClient)
    {
        projectsClient.ProjectsGetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(EmptyProjectPage()));
    }

    private IRenderedComponent<TeamDetail> RenderTeamDetailPage()
    {
        return Render<TeamDetail>(p => p.Add(c => c.TeamId, TestTeamId));
    }

    [Fact]
    public void Team_Load_Fails_Shows_Error_Alert()
    {
        var (teamsClient, projectsClient, _) = Setup();

        teamsClient.TeamsGetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns<Team>(_ => throw ServerError());
        SetupSuccessfulProjectsList(projectsClient);

        var teamDetailPage = RenderTeamDetailPage();

        teamDetailPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Team_Loads_Shows_Team_Name()
    {
        var (teamsClient, projectsClient, _) = Setup();

        teamsClient.TeamsGetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(ATeam()));
        SetupSuccessfulProjectsList(projectsClient);

        var teamDetailPage = RenderTeamDetailPage();

        teamDetailPage.Markup.Should().Contain("Alpha Team");
    }

    [Fact]
    public void Projects_Load_Fails_Shows_Error_Alert()
    {
        var (teamsClient, projectsClient, _) = Setup();

        teamsClient.TeamsGetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(ATeam()));
        projectsClient.ProjectsGetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                      .Returns<ProjectPage>(_ => throw ServerError());

        var teamDetailPage = RenderTeamDetailPage();

        teamDetailPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Projects_Empty_Shows_No_Projects_Message()
    {
        var (teamsClient, projectsClient, _) = Setup();

        teamsClient.TeamsGetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(ATeam()));
        SetupSuccessfulProjectsList(projectsClient);

        var teamDetailPage = RenderTeamDetailPage();

        teamDetailPage.Markup.Should().Contain("No projects yet");
    }

    [Fact]
    public async Task CreateProject_Dialog_Success_InvalidatesProjectCache()
    {
        var (teamsClient, projectsClient, projectCacheService) = Setup();
        teamsClient.TeamsGetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(ATeam()));
        SetupSuccessfulProjectsList(projectsClient);

        var createdProject = new Project("p1", TestTeamId, "New Project", null, null, null);
        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(createdProject)));

        var dialogService = SetupMockedDialogService();
        dialogService.ShowAsync<CreateProjectDialog>(Arg.Any<string>(), Arg.Any<DialogParameters<CreateProjectDialog>>())
                     .Returns(Task.FromResult(dialogRef));

        var teamDetailPage = RenderTeamDetailPage();
        await teamDetailPage.Find(".mud-button-filled-primary").ClickAsync();
        await Task.Delay(100);

        projectCacheService.Received(1).InvalidateForTeam(TestTeamId, Arg.Any<CancellationToken>());
    }
}
