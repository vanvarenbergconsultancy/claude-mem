using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

public sealed record ServerStoredCursorOptions
{
    public TimeSpan TokenExpiry { get; init; } = TimeSpan.FromDays(3);
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(15);
    public bool EnableAutoCleanup { get; init; } = true;
}
