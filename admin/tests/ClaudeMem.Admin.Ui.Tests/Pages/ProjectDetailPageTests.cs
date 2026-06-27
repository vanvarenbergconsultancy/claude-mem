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

public sealed class ProjectDetailPageTests : UiTestContext
{
    private const string TestTeamId = "team-1";
    private const string TestProjectId = "proj-1";
    private static readonly Uri BaseUri = new("https://api.example.com/projects?page_size=20");

    private static Project AProject()
    {
        return new Project(TestProjectId, TestTeamId, "My Project", null, null, null);
    }

    private static ApiKeyPage EmptyApiKeyPage()
    {
        return new ApiKeyPage(BaseUri, BaseUri, null, new List<ApiKey>());
    }

    private static ObservationPage EmptyObservationPage()
    {
        return new ObservationPage(BaseUri, BaseUri, null, new List<Observation>());
    }

    private static ApiException ServerError()
    {
        return new ApiException("error", 500, null, new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal), null);
    }

    private IRenderedComponent<ProjectDetail> RenderProjectDetailPage()
    {
        return Render<ProjectDetail>(p => p
            .Add(c => c.TeamId, TestTeamId)
            .Add(c => c.ProjectId, TestProjectId));
    }

    private static void SetupSuccessfulDependencies(
        IProjectsClient projectsClient,
        IApiKeysClient apiKeysClient,
        IObservationsClient observationsClient)
    {
        projectsClient.ProjectsGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AProject()));
        apiKeysClient.ApiKeysGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult(EmptyApiKeyPage()));
        observationsClient.ObservationsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                          .Returns(Task.FromResult(EmptyObservationPage()));
    }

    [Fact]
    public void Project_Load_Fails_Shows_Error_Alert()
    {
        var projectsClient = SetupProjectsClient();
        var apiKeysClient = SetupApiKeysClient();
        var observationsClient = SetupObservationsClient();

        projectsClient.ProjectsGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns<Project>(_ => throw ServerError());
        apiKeysClient.ApiKeysGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult(EmptyApiKeyPage()));
        observationsClient.ObservationsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                          .Returns(Task.FromResult(EmptyObservationPage()));

        var projectDetailPage = RenderProjectDetailPage();

        projectDetailPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Project_Loads_Shows_Project_Name()
    {
        var projectsClient = SetupProjectsClient();
        var apiKeysClient = SetupApiKeysClient();
        var observationsClient = SetupObservationsClient();
        SetupSuccessfulDependencies(projectsClient, apiKeysClient, observationsClient);

        var projectDetailPage = RenderProjectDetailPage();

        projectDetailPage.Markup.Should().Contain("My Project");
    }

    [Fact]
    public void ApiKeys_Load_Fails_Shows_Error_Alert()
    {
        var projectsClient = SetupProjectsClient();
        var apiKeysClient = SetupApiKeysClient();
        var observationsClient = SetupObservationsClient();

        projectsClient.ProjectsGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AProject()));
        apiKeysClient.ApiKeysGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                     .Returns<ApiKeyPage>(_ => throw ServerError());
        observationsClient.ObservationsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                          .Returns(Task.FromResult(EmptyObservationPage()));

        var projectDetailPage = RenderProjectDetailPage();

        projectDetailPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Observations_Load_Fails_Shows_Error_Alert()
    {
        var projectsClient = SetupProjectsClient();
        var apiKeysClient = SetupApiKeysClient();
        var observationsClient = SetupObservationsClient();

        projectsClient.ProjectsGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AProject()));
        apiKeysClient.ApiKeysGetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult(EmptyApiKeyPage()));
        observationsClient.ObservationsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                          .Returns<ObservationPage>(_ => throw ServerError());

        var projectDetailPage = RenderProjectDetailPage();

        projectDetailPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void ApiKeys_Empty_Shows_No_Keys_Message()
    {
        var projectsClient = SetupProjectsClient();
        var apiKeysClient = SetupApiKeysClient();
        var observationsClient = SetupObservationsClient();
        SetupSuccessfulDependencies(projectsClient, apiKeysClient, observationsClient);

        var projectDetailPage = RenderProjectDetailPage();

        projectDetailPage.Markup.Should().Contain("No API keys");
    }
}
