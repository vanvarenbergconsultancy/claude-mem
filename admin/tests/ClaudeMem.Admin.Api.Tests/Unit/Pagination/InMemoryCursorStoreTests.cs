using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class InMemoryCursorStoreTests
{
    private static InMemoryCursorStore CreateStore(TimeSpan? expiry = null)
    {
        var options = new ServerStoredCursorOptions
        {
            TokenExpiry = expiry ?? TimeSpan.FromDays(3)
        };

        return new InMemoryCursorStore(options);
    }

    [Fact]
    public async Task StoreAsync_RetrieveAsync_RoundTrip_ReturnsSamePayload()
    {
        var store = CreateStore();
        var payload = new CursorPayload("store-id", new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var token = await store.Store(payload, TestContext.Current.CancellationToken);
        var retrieved = await store.Retrieve(token, TestContext.Current.CancellationToken);

        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(payload.Id);
        retrieved.CreatedAt.Should().Be(payload.CreatedAt);
    }

    [Fact]
    public async Task RetrieveAsync_UnknownToken_ReturnsNull()
    {
        var store = CreateStore();

        var result = await store.Retrieve("00000000000000000000000000000000", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_ExpiredToken_ReturnsNull()
    {
        var store = CreateStore(expiry: TimeSpan.FromMilliseconds(1));
        var payload = new CursorPayload("expired-id", DateTimeOffset.UtcNow);
        var token = await store.Store(payload, TestContext.Current.CancellationToken);

        await Task.Delay(10, TestContext.Current.CancellationToken);

        var result = await store.Retrieve(token, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_AfterSuccessfulRetrieve_ReturnsNull()
    {
        var store = CreateStore();
        var payload = new CursorPayload("one-use-id", DateTimeOffset.UtcNow);
        var token = await store.Store(payload, TestContext.Current.CancellationToken);

        var first = await store.Retrieve(token, TestContext.Current.CancellationToken);
        var second = await store.Retrieve(token, TestContext.Current.CancellationToken);

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public async Task StoreAsync_SamePayloadTwice_ProducesUniqueTokens()
    {
        var store = CreateStore();
        var payload = new CursorPayload("same-id", DateTimeOffset.UtcNow);

        var token1 = await store.Store(payload, TestContext.Current.CancellationToken);
        var token2 = await store.Store(payload, TestContext.Current.CancellationToken);

        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task PurgeExpiredAsync_RemovesExpiredEntries()
    {
        var store = CreateStore(expiry: TimeSpan.FromMilliseconds(1));
        var token = await store.Store(new CursorPayload("expired", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        await Task.Delay(10, TestContext.Current.CancellationToken);
        await store.PurgeExpired(TestContext.Current.CancellationToken);

        var result = await store.Retrieve(token, TestContext.Current.CancellationToken);
        result.Should().BeNull();
    }

    [Fact]
    public async Task PurgeExpiredAsync_DoesNotRemoveActiveEntries()
    {
        var store = CreateStore(expiry: TimeSpan.FromDays(3));
        var payload = new CursorPayload("active", DateTimeOffset.UtcNow);
        var token = await store.Store(payload, TestContext.Current.CancellationToken);

        await store.PurgeExpired(TestContext.Current.CancellationToken);

        var result = await store.Retrieve(token, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PurgeExpiredAsync_EmptyStore_DoesNotThrow()
    {
        var store = CreateStore();

        var act = async () => await store.PurgeExpired(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
