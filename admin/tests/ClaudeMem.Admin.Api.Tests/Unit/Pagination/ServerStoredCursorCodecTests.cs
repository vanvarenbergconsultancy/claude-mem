using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class ServerStoredCursorCodecTests
{
    private static ServerStoredCursorCodec CreateCodec()
    {
        var store = new InMemoryCursorStore(new ServerStoredCursorOptions());
        return new ServerStoredCursorCodec(store);
    }

    [Fact]
    public async Task Tokenize_Detokenize_RoundTrip_ReturnsSameValues()
    {
        var codec = CreateCodec();
        var payload = new CursorPayload("round-trip-id", new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero));

        var token = await codec.Tokenize(payload, TestContext.Current.CancellationToken);
        var decoded = await codec.Detokenize(token, TestContext.Current.CancellationToken);

        decoded.Should().NotBeNull();
        decoded.Id.Should().Be(payload.Id);
        decoded.CreatedAt.Should().Be(payload.CreatedAt);
    }

    [Fact]
    public async Task Detokenize_NullToken_ReturnsNull()
    {
        var codec = CreateCodec();

        var result = await codec.Detokenize(null, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detokenize_UnknownToken_ThrowsInvalidCursorException()
    {
        var codec = CreateCodec();

        var act = async () => await codec.Detokenize("00000000000000000000000000000000", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidCursorException>();
    }

    [Fact]
    public async Task Tokenize_SamePayloadTwice_ProducesDifferentTokens()
    {
        var codec = CreateCodec();
        var payload = new CursorPayload("dup-id", DateTimeOffset.UtcNow);

        var token1 = await codec.Tokenize(payload, TestContext.Current.CancellationToken);
        var token2 = await codec.Tokenize(payload, TestContext.Current.CancellationToken);

        token1.Should().NotBe(token2);
    }
}
