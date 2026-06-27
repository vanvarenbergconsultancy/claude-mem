using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Components.Dialogs;
using ClaudeMem.Admin.Ui.Tests.Infrastructure;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Dialogs;

public sealed class CreateProjectDialogTests : UiTestContext
{
    private const string TestTeamId = "team-1";

    private Task<IRenderedComponent<CreateProjectDialog>> OpenCreateProjectDialogAsync()
    {
        var parameters = new DialogParameters<CreateProjectDialog>
        {
            { x => x.TeamId, TestTeamId }
        };

        return OpenDialog<CreateProjectDialog>("Create Project", parameters);
    }

    [Fact]
    public async Task Submit_With_Empty_Name_Does_Not_Call_Api()
    {
        var projectsClient = SetupProjectsClient();

        var dialog = await OpenCreateProjectDialogAsync();

        await SubmitDialogAsync(dialog);

        await Task.Delay(50);

        await projectsClient.DidNotReceive().ProjectsPostAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_With_Valid_Name_Calls_ProjectsPostAsync()
    {
        var projectsClient = SetupProjectsClient();
        projectsClient.ProjectsPostAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(new Project("p1", TestTeamId, "New Project", null, null, null)));

        var dialog = await OpenCreateProjectDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "New Project");

        await Task.Delay(100);

        await projectsClient.Received(1).ProjectsPostAsync(
            Arg.Is<Project>(p => p.Name == "New Project"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_With_ValidationError_Shows_FieldError()
    {
        var projectsClient = SetupProjectsClient();
        var validationException = CreateValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "Name", ["Name must be at least 4 characters."] }
        });
        projectsClient.ProjectsPostAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromException<Project>(validationException));

        var dialog = await OpenCreateProjectDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "ab");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Name must be at least 4 characters.");
    }

    [Fact]
    public async Task Submit_With_UnauthorizedError_Shows_AuthMessage()
    {
        var projectsClient = SetupProjectsClient();
        projectsClient.ProjectsPostAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromException<Project>(CreateUnauthorizedException()));

        var dialog = await OpenCreateProjectDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "ValidName");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task Submit_With_NetworkError_Shows_ConnectionMessage()
    {
        var projectsClient = SetupProjectsClient();
        projectsClient.ProjectsPostAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromException<Project>(CreateNetworkException()));

        var dialog = await OpenCreateProjectDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "ValidName");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Could not reach the server");
    }
}
