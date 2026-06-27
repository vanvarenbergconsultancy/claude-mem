using System;
using System.Collections.Generic;
using System.Linq;
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

public sealed class CreateApiKeyDialogTests : UiTestContext
{
    private const string TestTeamId = "team-1";
    private const string TestProjectId = "project-1";

    private Task<IRenderedComponent<CreateApiKeyDialog>> OpenCreateApiKeyDialogAsync()
    {
        var parameters = new DialogParameters<CreateApiKeyDialog>
        {
            { x => x.TeamId, TestTeamId },
            { x => x.ProjectId, TestProjectId }
        };

        return OpenDialog<CreateApiKeyDialog>("New API Key", parameters);
    }

    [Fact]
    public async Task Submit_With_Empty_Label_Does_Not_Call_Api()
    {
        var apiKeysClient = SetupApiKeysClient();

        var dialog = await OpenCreateApiKeyDialogAsync();

        await SubmitDialogAsync(dialog);

        await Task.Delay(50);

        await apiKeysClient.DidNotReceive().ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_With_Valid_Label_Calls_ApiKeysPostAsync()
    {
        var apiKeysClient = SetupApiKeysClient();
        apiKeysClient.ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NewApiKey("key-id", "pc-windows-dev", "test-key-value", DateTimeOffset.UtcNow)));

        var dialog = await OpenCreateApiKeyDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "pc-windows-dev");

        await Task.Delay(100);

        await apiKeysClient.Received(1).ApiKeysPostAsync(
            Arg.Is<ApiKey>(k => k.ActorId == "pc-windows-dev"),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_With_ValidationError_Shows_FieldError()
    {
        var apiKeysClient = SetupApiKeysClient();
        var validationException = CreateValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "ActorId", ["Label must not be empty."] }
        });
        apiKeysClient.ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<NewApiKey>(validationException));

        var dialog = await OpenCreateApiKeyDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "some-label");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Label must not be empty.");
    }

    [Fact]
    public async Task Submit_With_UnauthorizedError_Shows_AuthMessage()
    {
        var apiKeysClient = SetupApiKeysClient();
        apiKeysClient.ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<NewApiKey>(CreateUnauthorizedException()));

        var dialog = await OpenCreateApiKeyDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "some-label");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task Submit_With_NetworkError_Shows_ConnectionMessage()
    {
        var apiKeysClient = SetupApiKeysClient();
        apiKeysClient.ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<NewApiKey>(CreateNetworkException()));

        var dialog = await OpenCreateApiKeyDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "some-label");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Could not reach the server");
    }

    [Fact]
    public async Task Submit_Success_Auto_Copies_Key_To_Clipboard()
    {
        var apiKeysClient = SetupApiKeysClient();
        apiKeysClient.ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NewApiKey("key-id", "pc-windows-dev", "test-key-value", DateTimeOffset.UtcNow)));

        var dialog = await OpenCreateApiKeyDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "pc-windows-dev");

        await Task.Delay(100);

        JSInterop.Invocations
            .Where(i => string.Equals(i.Identifier, "navigator.clipboard.writeText", StringComparison.Ordinal))
            .Should().ContainSingle()
            .Which.Arguments.Should().HaveElementAt(0, "test-key-value");
    }

    [Fact]
    public async Task Submit_Success_Shows_Updated_Alert_Text()
    {
        var apiKeysClient = SetupApiKeysClient();
        apiKeysClient.ApiKeysPostAsync(
            Arg.Any<ApiKey>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NewApiKey("key-id", "pc-windows-dev", "test-key-value", DateTimeOffset.UtcNow)));

        var dialog = await OpenCreateApiKeyDialogAsync();

        await FillAndSubmitDialogAsync(dialog, "pc-windows-dev");

        await Task.Delay(100);

        dialog.Markup.Should().Contain("Key copied to your clipboard");
        dialog.Markup.Should().Contain("it will not be shown again");
    }
}
