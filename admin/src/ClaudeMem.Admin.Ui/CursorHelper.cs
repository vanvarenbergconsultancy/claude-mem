using System;
using Microsoft.AspNetCore.WebUtilities;

namespace ClaudeMem.Admin.Ui;

internal static class CursorHelper
{
    internal static string? ExtractCursor(Uri? next)
    {
        if (next is null)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(next.Query);
        return query.TryGetValue("cursor", out var value) ? value.ToString() : null;
    }
}
