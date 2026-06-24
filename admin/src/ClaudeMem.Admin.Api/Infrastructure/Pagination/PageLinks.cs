using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>Hypermedia links included in every paged API response.</summary>
public sealed record PageLinks(Uri Self, Uri First, Uri? Next);
