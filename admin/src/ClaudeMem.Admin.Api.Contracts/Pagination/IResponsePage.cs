using System;
using System.Diagnostics.CodeAnalysis;

namespace ClaudeMem.Admin.Api.Contracts;

/// <summary>Cursor-based pagination links shared by all paged response types.</summary>
public interface IResponsePage
{
    /// <summary>The URL for the current page.</summary>
    Uri Self { get; init; }

    /// <summary>The URL for the first page (no cursor).</summary>
    Uri First { get; init; }

    /// <summary>The URL for the next page, or <c>null</c> when this is the last page.</summary>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Matches generated ResponsePage.Next property from OpenAPI spec.")]
    Uri? Next { get; init; }
}
