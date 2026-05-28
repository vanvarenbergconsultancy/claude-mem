using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

public sealed class PaginationLinker(IHttpContextAccessor httpContextAccessor)
{
    public PageLinks Build(string? selfCursor, string? nextCursor, int pageSize)
    {
        var req = httpContextAccessor.HttpContext!.Request;
        var first = BuildUri(req, cursor: null, pageSize);
        var self = BuildUri(req, selfCursor, pageSize);
        var next = nextCursor is null ? null : BuildUri(req, nextCursor, pageSize);
        return new PageLinks(self, first, next);
    }

    private static Uri BuildUri(HttpRequest req, string? cursor, int pageSize)
    {
        var qs = QueryString.Empty;

        foreach (var (key, value) in req.Query)
        {
            if (key is not "cursor" and not "page_size")
            {
                qs = qs.Add(key, value.ToString());
            }
        }

        qs = qs.Add("page_size", pageSize.ToString(CultureInfo.InvariantCulture));

        if (cursor is not null)
        {
            qs = qs.Add("cursor", cursor);
        }

        return new Uri($"{req.Scheme}://{req.Host}{req.Path}{qs}");
    }
}
