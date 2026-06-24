using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;

/// <summary>Configuration for the server-stored cursor store.</summary>
public sealed record ServerStoredCursorOptions
{
    /// <summary>How long a stored cursor token remains valid. Defaults to 3 days.</summary>
    public TimeSpan TokenExpiry { get; init; } = TimeSpan.FromDays(3);

    /// <summary>How often the cleanup service purges expired tokens. Defaults to 15 minutes.</summary>
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>When <c>true</c>, registers a background service to periodically purge expired tokens. Defaults to <c>true</c>.</summary>
    public bool EnableAutoCleanup { get; init; } = true;
}
