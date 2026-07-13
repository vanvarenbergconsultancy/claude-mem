using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class MemoryCacheCursorStoreTests
{
    private static MemoryCacheCursorStore CreateStore(TimeSpan? expiry = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = new ServerStoredCursorOptions
        {
            TokenExpiry = expiry ?? TimeSpan.FromDays(3)
        };
        return new MemoryCacheCursorStore(cache, options);
    }

    [Fact]
    public async Task StoreAsync_RetrieveAsync_RoundTrip_ReturnsSamePayload()
    {
        var store = CreateStore();
        var payload = new CursorPayload("cache-id", new DateTimeOffset(2025, 8, 15, 0, 0, 0, TimeSpan.Zero));

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
    public async Task StoreAsync_SamePayloadTwice_ProducesUniqueTokens()
    {
        var store = CreateStore();
        var payload = new CursorPayload("same-cache-id", DateTimeOffset.UtcNow);

        var token1 = await store.Store(payload, TestContext.Current.CancellationToken);
        var token2 = await store.Store(payload, TestContext.Current.CancellationToken);

        token1.Should().NotBe(token2);
    }
}
