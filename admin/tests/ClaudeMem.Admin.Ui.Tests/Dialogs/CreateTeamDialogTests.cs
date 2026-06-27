using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Components.Dialogs;
using ClaudeMem.Admin.Ui.Tests.Infrastructure;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Dialogs;

public sealed class CreateTeamDialogTests : UiTestContext
{
    private Task<IRenderedComponent<CreateTeamDialog>> OpenCreateTeamDialogAsync()
    {
        return OpenDialog<CreateTeamDialog>("Create Team");
    }

    [Fact]
    public async Task Submit_With_Empty_Name_Does_Not_Call_Api()
    {
        var teamsClient = SetupTeamsClient();

        var dialog = await OpenCreateTeamDialogAsync();

        await SubmitDialogAsync(dialog);

        await Task.Delay(50);

        await teamsClient.DidNotReceive().TeamsPostAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_With_Valid_Name_Calls_TeamsPostAsync()
    {
        var teamsClient = SetupTeamsClient();
        teamsClient.TeamsPostAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(new Team("t1", "New Team", DateTimeOffset.UtcNow, 0)));

        var dialog = await OpenCreateTeamDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "New Team");

        await Task.Delay(100);

        await teamsClient.Received(1).TeamsPostAsync(
            Arg.Is<Team>(t => t.Name == "New Team"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_With_ValidationError_Shows_FieldError()
    {
        var teamsClient = SetupTeamsClient();
        var validationException = CreateValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "Name", ["Name must be at least 4 characters."] }
        });
        teamsClient.TeamsPostAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromException<Team>(validationException));

        var dialog = await OpenCreateTeamDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "ab");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Name must be at least 4 characters.");
    }

    [Fact]
    public async Task Submit_With_UnauthorizedError_Shows_AuthMessage()
    {
        var teamsClient = SetupTeamsClient();
        teamsClient.TeamsPostAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromException<Team>(CreateUnauthorizedException()));

        var dialog = await OpenCreateTeamDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "ValidName");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task Submit_With_NetworkError_Shows_ConnectionMessage()
    {
        var teamsClient = SetupTeamsClient();
        teamsClient.TeamsPostAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromException<Team>(CreateNetworkException()));

        var dialog = await OpenCreateTeamDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "ValidName");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Could not reach the server");
    }
}
