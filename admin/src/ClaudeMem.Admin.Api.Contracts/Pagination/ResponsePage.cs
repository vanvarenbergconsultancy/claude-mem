using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Contracts;

public sealed record ResponsePage<T>(IReadOnlyList<T> Items, string? NextCursor);
