using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Components.Pages;
using ClaudeMem.Admin.Ui.Services;
using ClaudeMem.Admin.Ui.Tests.Infrastructure;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Pages;

public sealed class AuditLogPageTests : UiTestContext
{
    private static readonly Uri BaseUri = new("https://api.example.com/audit-log?page_size=20");

    private static AuditLogPage EmptyAuditLogPage()
    {
        return new AuditLogPage(BaseUri, BaseUri, null, new List<AuditLogEntry>());
    }

    private static AuditLogPage AuditLogPageWithItems(IEnumerable<AuditLogEntry> items, Uri? next = null)
    {
        return new AuditLogPage(BaseUri, BaseUri, next, items);
    }

    private static AuditLogEntry MakeEntry(string id = "e1", string action = "test.action", string resourceType = "test")
    {
        return new AuditLogEntry(id, null, null, null, null, null, null, action, resourceType, null, null, DateTimeOffset.UtcNow);
    }

    private IAuditLogClient SetupWithCacheMocks()
    {
        var teamCacheService = SetupTeamCacheService();
        teamCacheService.GetAll(Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult<IReadOnlyList<Team>>(Array.Empty<Team>()));
        var projectCacheService = SetupProjectCacheService();
        projectCacheService.GetForTeam(Arg.Any<string>(), Arg.Any<CancellationToken>())
                           .Returns(Task.FromResult<IReadOnlyList<Project>>(Array.Empty<Project>()));
        SetupCacheInvalidationBus();
        return SetupAuditLogClient();
    }

    [Fact]
    public void Api_Returns_Error_Shows_Error_Alert()
    {
        var auditLogClient = SetupWithCacheMocks();
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns<AuditLogPage>(_ => throw new ApiException("error", 500, null,
                          new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal), null));

        var auditLogPage = Render<AuditLog>();

        auditLogPage.FindComponents<MudAlert>().Should().ContainSingle(a => a.Instance.Severity == Severity.Error);
    }

    [Fact]
    public void Api_Returns_Empty_List_Shows_No_Entries_Message()
    {
        var auditLogClient = SetupWithCacheMocks();
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(EmptyAuditLogPage()));

        var auditLogPage = Render<AuditLog>();

        auditLogPage.Markup.Should().Contain("No audit log entries");
    }

    [Fact]
    public void Api_Returns_Entries_Shows_Action_Column()
    {
        var auditLogClient = SetupWithCacheMocks();
        var entry = MakeEntry(action: "project.created");
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AuditLogPageWithItems(new List<AuditLogEntry> { entry })));

        var auditLogPage = Render<AuditLog>();

        auditLogPage.Markup.Should().Contain("project.created");
    }

    [Fact]
    public void Api_Returns_Next_Shows_LoadMore_Button()
    {
        var auditLogClient = SetupWithCacheMocks();
        var entry = MakeEntry();
        var nextPageUri = new Uri("https://api.example.com/audit-log?page_size=20&cursor=abc123");
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AuditLogPageWithItems(new List<AuditLogEntry> { entry }, next: nextPageUri)));

        var auditLogPage = Render<AuditLog>();

        auditLogPage.Markup.Should().Contain("Load more");
    }

    [Fact]
    public void Api_Returns_No_Next_Hides_LoadMore_Button()
    {
        var auditLogClient = SetupWithCacheMocks();
        var entry = MakeEntry();
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AuditLogPageWithItems(new List<AuditLogEntry> { entry })));

        var auditLogPage = Render<AuditLog>();

        auditLogPage.Markup.Should().NotContain("Load more");
    }

    [Fact]
    public void ClickRow_Shows_Detail_Panel()
    {
        var auditLogClient = SetupWithCacheMocks();
        var entry = MakeEntry();
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AuditLogPageWithItems(new List<AuditLogEntry> { entry })));

        var auditLogPage = Render<AuditLog>();
        auditLogPage.Find("tbody tr").Click();

        auditLogPage.Markup.Should().Contain("Entry Details");
    }

    [Fact]
    public void ClickSameRow_Again_Hides_Detail_Panel()
    {
        var auditLogClient = SetupWithCacheMocks();
        var entry = MakeEntry();
        auditLogClient.AuditLogAsync(
                          Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                          Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(AuditLogPageWithItems(new List<AuditLogEntry> { entry })));

        var auditLogPage = Render<AuditLog>();
        auditLogPage.Find("tbody tr").Click();
        auditLogPage.Find("tbody tr").Click();

        auditLogPage.Markup.Should().NotContain("Entry Details");
    }
}
