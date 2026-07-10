using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using System;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class CursorPayloadTests
{
    [Fact]
    public void Encode_Decode_RoundTrip_ReturnsSameValues()
    {
        var createdAt = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var cursorPayload = new CursorPayload("some-id-123", createdAt);

        var encoded = CursorPayload.Encode(cursorPayload);
        var decoded = CursorPayload.Decode(encoded);

        using var scope = new AssertionScope();
        decoded.Should().NotBeNull();
        decoded.Id.Should().Be(cursorPayload.Id);
        decoded.CreatedAt.Should().Be(cursorPayload.CreatedAt);
    }

    [Fact]
    public void Decode_Null_ReturnsNull()
    {
        var result = CursorPayload.Decode(null);

        result.Should().BeNull();
    }

    [Fact]
    public void Decode_MalformedBase64_ReturnsNull()
    {
        var result = CursorPayload.Decode("not-valid-base64!!!");

        result.Should().BeNull();
    }

    [Fact]
    public void Decode_ValidBase64ButNotJson_ReturnsNull()
    {
        var notJson = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hello world"));

        var result = CursorPayload.Decode(notJson);

        result.Should().BeNull();
    }

    [Fact]
    public void Encode_ProducesOpaqueString_NotEqualToId()
    {
        var payload = new CursorPayload("my-id", DateTimeOffset.UtcNow);

        var encoded = CursorPayload.Encode(payload);
        
        using var scope = new AssertionScope();
        encoded.Should().NotBeNullOrEmpty();
        encoded.Should().NotBe("my-id");
        encoded.Should().NotContain("my-id");
    }
}
