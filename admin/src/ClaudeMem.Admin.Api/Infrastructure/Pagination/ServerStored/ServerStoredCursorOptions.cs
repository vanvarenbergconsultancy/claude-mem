using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

public sealed record ServerStoredCursorOptions
{
    public TimeSpan TokenExpiry { get; init; } = TimeSpan.FromDays(3);
}
