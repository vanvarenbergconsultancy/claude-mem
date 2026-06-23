using System;
using System.Collections.Generic;
using AwesomeAssertions;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal static class ResponsePageAssertions
{
    public static void ShouldBeEmptyPage<T>(this IResponsePage page, IEnumerable<T> items)
    {
        page.Should().NotBeNull();
        items.Should().BeEmpty();
        page.Next.Should().BeNull();
    }

    public static void ShouldHaveItems<T>(this IResponsePage page, IEnumerable<T> items, int count)
    {
        page.Should().NotBeNull();
        items.Should()
            .NotBeNullOrEmpty()
            .And.HaveCount(count);
    }

    public static void ShouldBeFirstPage(this IResponsePage page)
    {
        page.Should().NotBeNull();
        page.Self.Should().NotBeNull();
        page.First.Should().NotBeNull();
        page.Next.Should().NotBeNull();
        page.Self.Should().Be(page.First);
    }

    public static void ShouldBeMidPage(this IResponsePage page, Uri expectedFirst)
    {
        page.Should().NotBeNull();
        page.Self.Should().NotBeNull();
        page.First.Should().Be(expectedFirst);
        page.Next.Should().NotBeNull();
        page.Self.Should().NotBe(page.First);
    }

    public static void ShouldBeLastPage(this IResponsePage page, Uri expectedFirst)
    {
        page.Should().NotBeNull();
        page.Self.Should().NotBeNull();
        page.First.Should().Be(expectedFirst);
        page.Next.Should().BeNull();
    }

    public static string? NextCursor(this IResponsePage page)
    {
        if (page.Next is null)
        {
            return null;
        }

        var query = page.Next.Query.TrimStart('?');
        var querySegments = query.Split('&');

        foreach (var segment in querySegments)
        {
            var idx = segment.IndexOf('=');
            if (idx > 0 && segment[..idx] == "cursor")
            {
                return Uri.UnescapeDataString(segment[(idx + 1)..]);
            }
        }

        return null;
    }
}
