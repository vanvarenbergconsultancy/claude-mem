using System;

namespace ClaudeMem.Admin.Api.Features.ApiKeys;

internal sealed record ApiKeyInsertResult(string Id, string ActorId, DateTimeOffset CreatedAt);
