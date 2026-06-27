using System;
using AwesomeAssertions;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Unit;

public sealed class CursorHelperTests
{
    [Fact]
    public void ExtractCursor_NullUri_ReturnsNull()
    {
        var result = CursorHelper.ExtractCursor(null);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractCursor_UriWithCursor_ReturnsCursorValue()
    {
        var uri = new Uri("https://api.example.com/teams?cursor=abc123");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().Be("abc123");
    }

    [Fact]
    public void ExtractCursor_UriWithCursorAndOtherParams_ReturnsCursorValue()
    {
        var uri = new Uri("https://api.example.com/teams?cursor=xyz789&pageSize=20&status=active");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().Be("xyz789");
    }

    [Fact]
    public void ExtractCursor_UriWithNoCursorParam_ReturnsNull()
    {
        var uri = new Uri("https://api.example.com/teams?pageSize=20");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractCursor_UriWithNoQueryString_ReturnsNull()
    {
        var uri = new Uri("https://api.example.com/teams");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractCursor_UriWithUrlEncodedCursorValue_ReturnsDecodedValue()
    {
        var uri = new Uri("https://api.example.com/teams?cursor=eyJpZCI6ImFiYyIsImNyZWF0ZWRfYXQiOiIyMDI0LTAxLTAxIn0%3D");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().Be("eyJpZCI6ImFiYyIsImNyZWF0ZWRfYXQiOiIyMDI0LTAxLTAxIn0=");
    }

    [Fact]
    public void ExtractCursor_UriWithCursorBeforeOtherParams_ReturnsCursorValue()
    {
        var uri = new Uri("https://api.example.com/teams?cursor=first&pageSize=10");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().Be("first");
    }

    [Fact]
    public void ExtractCursor_UriWithCursorAfterOtherParams_ReturnsCursorValue()
    {
        var uri = new Uri("https://api.example.com/teams?pageSize=10&cursor=last");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().Be("last");
    }

    [Fact]
    public void ExtractCursor_UriWithEmptyCursorValue_ReturnsEmptyString()
    {
        var uri = new Uri("https://api.example.com/teams?cursor=");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractCursor_UriWithOpaqueBase64Cursor_ReturnsCursorVerbatim()
    {
        const string opaqueCursor = "eyJwb3NpdGlvbiI6eyJpZCI6IjEyMyIsImNyZWF0ZWRfYXQiOiIyMDI0LTAxLTAxVDAwOjAwOjAwWiJ9fQ==";
        var uri = new Uri($"https://api.example.com/teams?cursor={Uri.EscapeDataString(opaqueCursor)}");

        var result = CursorHelper.ExtractCursor(uri);

        result.Should().Be(opaqueCursor);
    }
}
