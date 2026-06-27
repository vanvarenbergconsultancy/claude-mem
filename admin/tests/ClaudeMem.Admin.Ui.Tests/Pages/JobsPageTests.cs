using System;
using System.Collections.Generic;
using System.Linq;
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

public sealed class JobsPageTests : UiTestContext
{
    private static readonly Uri BaseUri = new("https://api.example.com/jobs?page_size=20");

    private static JobPage EmptyJobPage()
    {
        return new JobPage(BaseUri, BaseUri, null, new List<Job>());
    }

    private static JobPage JobPageWithItems(IEnumerable<Job> items, Uri? next = null)
    {
        return new JobPage(BaseUri, BaseUri, next, items);
    }

    [Fact]
    public void Api_Returns_Error_Shows_Error_Alert()
    {
        var jobsClient = SetupJobsClient();
        jobsClient.JobsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns<JobPage>(_ => throw new ApiException("error", 500, null,
                      new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal), null));

        var jobsPage = Render<Jobs>();

        jobsPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Api_Returns_Jobs_Shows_Status_Chip()
    {
        var jobsClient = SetupJobsClient();
        var completedJob = new Job("job1", "proj1", "completed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        jobsClient.JobsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(JobPageWithItems(new List<Job> { completedJob })));

        var jobsPage = Render<Jobs>();

        jobsPage.Markup.Should().Contain("completed");
    }

    [Fact]
    public void Api_Returns_Empty_Shows_NoRecords_Message()
    {
        var jobsClient = SetupJobsClient();
        jobsClient.JobsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(EmptyJobPage()));

        var jobsPage = Render<Jobs>();

        jobsPage.Markup.Should().Contain("No jobs match");
    }

    [Fact]
    public void Completed_Job_Has_No_Retry_Button()
    {
        var jobsClient = SetupJobsClient();
        var completedJob = new Job("job1", "proj1", "completed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        jobsClient.JobsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(JobPageWithItems(new List<Job> { completedJob })));

        var jobsPage = Render<Jobs>();

        var retryButtons = jobsPage.FindComponents<MudIconButton>()
            .Where(b => string.Equals(b.Instance.Icon, Icons.Material.Filled.Replay, StringComparison.Ordinal))
            .ToList();
        retryButtons.Should().BeEmpty();
    }

    [Fact]
    public void Failed_Job_Has_Retry_Button()
    {
        var jobsClient = SetupJobsClient();
        var failedJob = new Job("job1", "proj1", "failed", DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow);
        jobsClient.JobsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(JobPageWithItems(new List<Job> { failedJob })));

        var jobsPage = Render<Jobs>();

        var retryButtons = jobsPage.FindComponents<MudIconButton>()
            .Where(b => string.Equals(b.Instance.Icon, Icons.Material.Filled.Replay, StringComparison.Ordinal))
            .ToList();
        retryButtons.Should().ContainSingle();
    }
}
