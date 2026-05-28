using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

public sealed record PageLinks(Uri Self, Uri First, Uri? Next);
