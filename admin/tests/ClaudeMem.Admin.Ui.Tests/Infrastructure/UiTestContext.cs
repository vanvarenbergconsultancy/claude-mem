using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Xunit;
using TestContext = Xunit.TestContext;

namespace ClaudeMem.Admin.Ui.Tests.Infrastructure;

public abstract class UiTestContext : BunitContext, IAsyncLifetime
{
    protected UiTestContext()
    {
        Services.AddMudServices();
        Services.AddSingleton<IPopoverService, NullPopoverService>();
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected ITeamsClient SetupTeamsClient()
    {
        var client = Substitute.For<ITeamsClient>();
        Services.AddSingleton(client);

        return client;
    }

    protected IProjectsClient SetupProjectsClient()
    {
        var client = Substitute.For<IProjectsClient>();
        Services.AddSingleton(client);

        return client;
    }

    protected IApiKeysClient SetupApiKeysClient()
    {
        var client = Substitute.For<IApiKeysClient>();
        Services.AddSingleton(client);

        return client;
    }

    protected IObservationsClient SetupObservationsClient()
    {
        var client = Substitute.For<IObservationsClient>();
        Services.AddSingleton(client);

        return client;
    }

    protected IJobsClient SetupJobsClient()
    {
        var client = Substitute.For<IJobsClient>();
        Services.AddSingleton(client);

        return client;
    }

    protected ITeamCacheService SetupTeamCacheService()
    {
        var service = Substitute.For<ITeamCacheService>();
        Services.AddSingleton(service);

        return service;
    }

    protected IProjectCacheService SetupProjectCacheService()
    {
        var service = Substitute.For<IProjectCacheService>();
        Services.AddSingleton(service);

        return service;
    }

    protected ICacheInvalidationBus SetupCacheInvalidationBus()
    {
        var bus = Substitute.For<ICacheInvalidationBus>();
        Services.AddSingleton(bus);

        return bus;
    }

    protected IAuditLogClient SetupAuditLogClient()
    {
        var client = Substitute.For<IAuditLogClient>();
        Services.AddSingleton(client);

        return client;
    }

    protected static ApiException<ValidationProblemDetails> CreateValidationException(IDictionary<string, string[]> fieldErrors)
    {
        var errors = fieldErrors.ToDictionary(
            k => k.Key,
            v => (IEnumerable<string>)v.Value,
            StringComparer.Ordinal);

        var vpd = new ValidationProblemDetails(type: null, title: "Validation failed", status: 422, detail: null, instance: null, errors: errors);
        
        return new ApiException<ValidationProblemDetails>(
            "Error", 422, null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
            vpd, null);
    }

    protected static ApiException CreateUnauthorizedException()
    {
        return new ApiException("Missing or invalid API key", 401, null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal), null);
    }

    protected static HttpRequestException CreateNetworkException()
    {
        return new HttpRequestException("Connection refused");
    }

    protected async Task<IRenderedComponent<TDialog>> OpenDialog<TDialog>(string title, DialogParameters<TDialog>? parameters = null)
        where TDialog : ComponentBase
    {
        var dialogProvider = Render<MudDialogProvider>();
        var dialogService = Services.GetService<IDialogService>()
            ?? throw new InvalidOperationException("IDialogService not registered.");

        await dialogService.ShowAsync<TDialog>(title, parameters ?? new DialogParameters<TDialog>());
        await Task.Delay(50, TestContext.Current.CancellationToken);

        await dialogProvider.WaitForStateAsync(
            () => dialogProvider.FindComponents<TDialog>().Count > 0,
            timeout: TimeSpan.FromSeconds(5));

        var dialog = dialogProvider.FindComponent<TDialog>();

        dialog.Should().NotBeNull();

        return dialog;
    }

    protected static async Task SubmitDialogAsync<TComponent>(IRenderedComponent<TComponent> dialog)
        where TComponent : ComponentBase
    {
        var buttons = dialog.FindAll("button");

        buttons.Should().NotBeEmpty();

        await buttons[^1].ClickAsync();
    }

    protected static async Task FillAndSubmitDialogAsync<TComponent>(IRenderedComponent<TComponent> dialog, string value)
        where TComponent : ComponentBase
    {
        var input = dialog.Find("input");

        input.Should().NotBeNull();

        await input.InputAsync(value);

        await SubmitDialogAsync(dialog);
    }

    ValueTask IAsyncLifetime.InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
