using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class CursorStoreCleanupServiceTests
{
    private sealed class FakeStore : ISupportsCursorPurge
    {
        public int PurgeCallCount { get; private set; }

        public Task PurgeExpired(CancellationToken cancellationToken = default)
        {
            PurgeCallCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExecuteAsync_CallsPurgeOnRegisteredStores()
    {
        var store = new FakeStore();
        var options = new ServerStoredCursorOptions { CleanupInterval = TimeSpan.FromMilliseconds(50) };
        var service = new CursorStoreCleanupService([store], options);

        using var cts = new CancellationTokenSource();
        _ = service.StartAsync(cts.Token);

        await Task.Delay(180, TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        await service.StopAsync(TestContext.Current.CancellationToken);

        store.PurgeCallCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_StopsCleanlyOnCancellation()
    {
        var store = new FakeStore();
        var options = new ServerStoredCursorOptions { CleanupInterval = TimeSpan.FromSeconds(60) };
        var service = new CursorStoreCleanupService([store], options);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await cts.CancelAsync();

        var act = async () => await service.StopAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
