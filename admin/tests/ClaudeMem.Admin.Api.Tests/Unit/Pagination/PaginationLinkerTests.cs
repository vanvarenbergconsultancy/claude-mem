using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Pagination;

public sealed class PaginationLinkerTests
{
    private static PaginationLinker CreateLinker(HttpContext httpContext)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return new PaginationLinker(accessor);
    }

    private static DefaultHttpContext CreateContext(string path, string queryString = "")
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("api.example.com"),
                Path = new PathString(path),
                QueryString = new QueryString(queryString)
            }
        };

        return context;
    }

    [Fact]
    public void Build_FirstPage()
    {
        var context = CreateContext("/teams", "?page_size=10");
        var linker = CreateLinker(context);

        var links = linker.Build(selfCursor: null, nextCursor: null, pageSize: 10);

        using var scope = new AssertionScope();
        links.Should().NotBeNull();
        links.Self.Should().Be(links.First);
        links.Self.Query.Should().NotContain("cursor");
        links.First.Query.Should().NotContain("cursor");
        links.Next.Should().BeNull();
    }

    [Fact]
    public void Build_MidPage_SelfContainsSelfCursor()
    {
        var selfCursor = "cursor-abc";
        var context = CreateContext("/teams", $"?page_size=5&cursor={selfCursor}");
        var linker = CreateLinker(context);

        var links = linker.Build(selfCursor: selfCursor, nextCursor: "cursor-xyz", pageSize: 5);

        using var scope = new AssertionScope();
        links.Should().NotBeNull();
        links.Self.Query.Should().Contain($"cursor={Uri.EscapeDataString(selfCursor)}");
        links.First.Query.Should().NotContain("cursor=");
    }

    [Fact]
    public void Build_FilterParamsPreserved_InAllLinks()
    {
        var context = CreateContext("/jobs", "?page_size=5&status=failed");
        var linker = CreateLinker(context);

        var links = linker.Build(selfCursor: null, nextCursor: "cursor-next", pageSize: 5);

        using var scope = new AssertionScope();
        links.Should().NotBeNull();
        links.Self.Query.Should().Contain("status=failed");
        links.First.Query.Should().Contain("status=failed");
        links.Next!.Query.Should().Contain("status=failed");
    }

    [Fact]
    public void Build_NextContainsNextCursor()
    {
        var nextCursor = "next-cursor-value";
        var context = CreateContext("/teams", "?page_size=3");
        var linker = CreateLinker(context);

        var links = linker.Build(selfCursor: null, nextCursor: nextCursor, pageSize: 3);

        using var scope = new AssertionScope();
        links.Should().NotBeNull();
        links.Next.Should().NotBeNull();
        links.Next!.Query.Should().Contain($"cursor={Uri.EscapeDataString(nextCursor)}");
    }

    [Fact]
    public void Build_PageSizeAppearInLinks()
    {
        var context = CreateContext("/teams", "?page_size=7");
        var linker = CreateLinker(context);

        var links = linker.Build(selfCursor: null, nextCursor: null, pageSize: 7);

        using var scope = new AssertionScope();
        links.Should().NotBeNull();
        links.Self.Query.Should().Contain("page_size=7");
        links.First.Query.Should().Contain("page_size=7");
    }
}
