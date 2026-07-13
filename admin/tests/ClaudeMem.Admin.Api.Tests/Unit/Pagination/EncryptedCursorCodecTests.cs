using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class EncryptedCursorCodecTests
{
    private readonly EncryptedCursorCodec _sut;

    public EncryptedCursorCodecTests()
    {
        _sut = new EncryptedCursorCodec(new EphemeralDataProtectionProvider());
    }

    [Fact]
    public async Task Tokenize_Detokenize_RoundTrip_ReturnsSameValues()
    {
        var payload = new CursorPayload("encrypted-id", new DateTimeOffset(2025, 1, 20, 12, 0, 0, TimeSpan.Zero));

        var token = await Tokenize(payload);
        var decoded = await Detokenize(token);

        using var scope = new AssertionScope();
        decoded.Should().NotBeNull();
        decoded.Id.Should().Be(payload.Id);
        decoded.CreatedAt.Should().Be(payload.CreatedAt);
    }

    [Fact]
    public async Task Detokenize_NullToken_ReturnsNull()
    {

        var result = await Detokenize(null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detokenize_GarbageToken_ThrowsInvalidCursorException()
    {

        var act = async () => await Detokenize("not-a-valid-token");
        await act.Should().ThrowAsync<InvalidCursorException>();
    }

    [Fact]
    public async Task Detokenize_TamperedToken_ThrowsInvalidCursorException()
    {
        var payload = new CursorPayload("id-flip", DateTimeOffset.UtcNow);
        var token = await Tokenize(payload);

        var chars = token.ToCharArray();
        chars[token.Length / 2] ^= (char)0x01;
        var tampered = new string(chars);

        var act = async () => await Detokenize(tampered);

        await act.Should().ThrowAsync<InvalidCursorException>();
    }

    [Fact]
    public async Task Tokenize_SamePayloadTwice_ProducesDifferentTokens()
    {
        var payload = new CursorPayload("same-id", new DateTimeOffset(2024, 9, 1, 0, 0, 0, TimeSpan.Zero));

        var token1 = await Tokenize(payload);
        var token2 = await Tokenize(payload);

        using var scope = new AssertionScope();
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task Tokenize_OutputDoesNotContainPlaintextId()
    {
        var payload = new CursorPayload("my-private-id", DateTimeOffset.UtcNow);

        var token = await Tokenize(payload);
        
        using var scope = new AssertionScope();
        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("my-private-id");
    }

    private Task<string> Tokenize(CursorPayload payload)
    {
        return _sut.Tokenize(payload, TestContext.Current.CancellationToken);
    }
    private Task<CursorPayload?> Detokenize(string? token)
    {
        return _sut.Detokenize(token, TestContext.Current.CancellationToken);
    }
}
