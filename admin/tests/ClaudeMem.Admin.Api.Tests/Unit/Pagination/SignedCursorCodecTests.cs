using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class SignedCursorCodecTests
{
    private static SignedCursorCodec CreateCodec(string signingKey = "test-signing-key-that-is-long-enough-for-hmac")
    {
        return new SignedCursorCodec(signingKey);
    }

    [Fact]
    public async Task Tokenize_Detokenize_RoundTrip_ReturnsSameValues()
    {
        var codec = CreateCodec();
        var payload = new CursorPayload("test-id-abc", new DateTimeOffset(2024, 3, 10, 8, 0, 0, TimeSpan.Zero));

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
    public async Task Detokenize_TokenWithoutDotSeparator_ThrowsInvalidCursorException()
    {
        var codec = CreateCodec();

        var act = async () => await codec.Detokenize("nodotanywhere", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidCursorException>();
    }

    [Fact]
    public async Task Detokenize_CorruptedSignature_ThrowsInvalidCursorException()
    {
        var codec = CreateCodec();
        var payload = new CursorPayload("id-1", DateTimeOffset.UtcNow);
        var token = await codec.Tokenize(payload, TestContext.Current.CancellationToken);

        var dotIndex = token.LastIndexOf('.');
        var corrupted = token[..dotIndex] + ".AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        var act = async () => await codec.Detokenize(corrupted, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidCursorException>();
    }

    [Fact]
    public async Task Detokenize_WrongSigningKey_ThrowsInvalidCursorException()
    {
        var codec1 = CreateCodec("key-one-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        var codec2 = CreateCodec("key-two-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        var payload = new CursorPayload("id-2", DateTimeOffset.UtcNow);

        var token = await codec1.Tokenize(payload, TestContext.Current.CancellationToken);

        var act = async () => await codec2.Detokenize(token, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidCursorException>();
    }

    [Fact]
    public async Task TwoCodecs_DifferentKeys_CannotDecodeEachOthersTokens()
    {
        var codecA = CreateCodec("alpha-key-xxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        var codecB = CreateCodec("beta-key-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        var payload = new CursorPayload("shared-id", DateTimeOffset.UtcNow);

        var tokenFromA = await codecA.Tokenize(payload, TestContext.Current.CancellationToken);
        var tokenFromB = await codecB.Tokenize(payload, TestContext.Current.CancellationToken);

        var actAb = async () => await codecA.Detokenize(tokenFromB, TestContext.Current.CancellationToken);
        var actBa = async () => await codecB.Detokenize(tokenFromA, TestContext.Current.CancellationToken);

        await actAb.Should().ThrowAsync<InvalidCursorException>();
        await actBa.Should().ThrowAsync<InvalidCursorException>();
    }
}
