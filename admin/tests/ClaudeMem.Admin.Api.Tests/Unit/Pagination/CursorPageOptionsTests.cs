using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class CursorPageOptionsTests
{
    private sealed record TestFilter(string? Cursor, int? PageSize) : ICursorFilter;

    private static readonly PlainCursorCodec PlainCodec = new();

    private static void AssertPageOptions(CursorPageOptions opts, int expectedPageSize)
    {
        using var scope = new AssertionScope();
        opts.Should().NotBeNull();
        opts.PageSize.Should().Be(expectedPageSize);
        opts.FetchCount.Should().Be(expectedPageSize + 1);
    }

    [Fact]
    public async Task CreateAsync_NullPageSize_UsesDefault()
    {
        var filter = new TestFilter(null, null);

        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);

        AssertPageOptions(opts, CursorPageOptions.DefaultPageSize);
    }

    [Fact]
    public async Task CreateAsync_PageSizeBelowMin_ClampsToMin()
    {
        var filter = new TestFilter(null, 0);

        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);

        AssertPageOptions(opts, CursorPageOptions.MinPageSize);
    }

    [Fact]
    public async Task CreateAsync_PageSizeAboveMax_ClampsToMax()
    {
        var filter = new TestFilter(null, 500);

        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);

        AssertPageOptions(opts, CursorPageOptions.MaxPageSize);
    }

    [Fact]
    public async Task CreateAsync_ValidPageSize_UsesAsIs()
    {
        var filter = new TestFilter(null, 42);

        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);

        AssertPageOptions(opts, 42);
    }

    [Fact]
    public async Task CreateAsync_NullCursor_DecodedCursorIsNull()
    {
        var filter = new TestFilter(null, null);

        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);

        AssertPageOptions(opts, CursorPageOptions.DefaultPageSize);
        opts.DecodedCursor.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidCursor_DecodesPayload()
    {
        var payload = new CursorPayload("abc", DateTimeOffset.UtcNow);
        var token = await PlainCodec.Tokenize(payload, TestContext.Current.CancellationToken);
        var filter = new TestFilter(token, null);

        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);

        AssertPageOptions(opts, CursorPageOptions.DefaultPageSize);
        opts.DecodedCursor.Should().NotBeNull();
        opts.DecodedCursor!.Id.Should().Be("abc");
    }

    [Fact]
    public async Task TrimAndGetNextCursorAsync_FewerRowsThanFetchCount_ReturnsNull()
    {
        var filter = new TestFilter(null, 3);
        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);
        var rows = new List<CursorPayload>
        {
            new("a", DateTimeOffset.UtcNow),
            new("b", DateTimeOffset.UtcNow)
        };

        var cursor = await opts.TrimAndGetNextCursor(rows, r => r, TestContext.Current.CancellationToken);

        cursor.Should().BeNull();
        rows.Should().NotBeNullOrEmpty();
        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task TrimAndGetNextCursorAsync_ExactlyFetchCount_RemovesSentinelAndReturnsToken()
    {
        var filter = new TestFilter(null, 3);
        var opts = await CursorPageOptions.Create(filter, PlainCodec, TestContext.Current.CancellationToken);
        var lastPageRow = new CursorPayload("last", new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero));
        var rows = new List<CursorPayload>
        {
            new("a", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new("b", new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)),
            new("c", new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero)),
            lastPageRow
        };

        var cursor = await opts.TrimAndGetNextCursor(rows, r => r, TestContext.Current.CancellationToken);

        cursor.Should().NotBeNull();
        rows.Should().NotBeNullOrEmpty();
        rows.Should().HaveCount(3);
        rows.Should().NotContain(lastPageRow);

        var decoded = await PlainCodec.Detokenize(cursor, TestContext.Current.CancellationToken);
        decoded.Should().NotBeNull();
        decoded.Id.Should().Be("c");
    }

    [Fact]
    public async Task CreateAsync_SignedCodec_TamperedCursor_ThrowsInvalidCursorException()
    {
        var codec = new ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac.SignedCursorCodec("test-signing-key-that-is-long-enough");
        var tampered = new TestFilter("tampered.invalidsignature", null);

        var act = async () => await CursorPageOptions.Create(tampered, codec, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidCursorException>();
    }
}
