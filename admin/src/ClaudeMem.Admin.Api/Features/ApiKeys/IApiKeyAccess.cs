using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.ApiKeys;

/// <summary>Database access contract for API key operations.</summary>
internal interface IApiKeyAccess
{
    /// <summary>Returns a cursor-paged list of API keys matching the filter.</summary>
    Task<CursorPageResult<ApiKey>> GetApiKeys(GetApiKeysFilter filter, CancellationToken cancellationToken);

    /// <summary>Inserts a new API key row and returns the created record metadata.</summary>
    Task<ApiKeyInsertResult> InsertApiKey(string teamId, string projectId, string actorId, string keyHash, CancellationToken cancellationToken);

    /// <summary>Returns team/project ownership info for the given key ID, or <c>null</c> if not found.</summary>
    Task<ApiKeyLookup?> GetApiKey(string keyId, CancellationToken cancellationToken);

    /// <summary>Marks the given API key as revoked.</summary>
    Task RevokeApiKeyById(string keyId, CancellationToken cancellationToken);
}
