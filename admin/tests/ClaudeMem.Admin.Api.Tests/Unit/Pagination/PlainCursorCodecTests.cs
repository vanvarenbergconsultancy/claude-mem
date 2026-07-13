using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class PlainCursorCodecTests
{
    private static readonly PlainCursorCodec Codec = new();

    [Fact]
    public async Task Tokenize_Detokenize_RoundTrip_ReturnsSameValues()
    {
        var payload = new CursorPayload("test-id-123", new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero));

        var token = await Codec.Tokenize(payload, TestContext.Current.CancellationToken);
        var decoded = await Codec.Detokenize(token, TestContext.Current.CancellationToken);

        decoded.Should().NotBeNull();
        decoded.Id.Should().Be(payload.Id);
        decoded.CreatedAt.Should().Be(payload.CreatedAt);
    }

    [Fact]
    public async Task Detokenize_NullToken_ReturnsNull()
    {
        var result = await Codec.Detokenize(null, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detokenize_InvalidBase64_ReturnsNull()
    {
        var result = await Codec.Detokenize("not-valid-base64!!!", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detokenize_ValidBase64ButNotJson_ReturnsNull()
    {
        var notJson = Convert.ToBase64String("hello world"u8.ToArray());

        var result = await Codec.Detokenize(notJson, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Tokenize_OutputIsOpaque_DoesNotContainPlaintextId()
    {
        var payload = new CursorPayload("my-secret-id", DateTimeOffset.UtcNow);

        var token = await Codec.Tokenize(payload, TestContext.Current.CancellationToken);

        token.Should().NotContain("my-secret-id");
    }
}
