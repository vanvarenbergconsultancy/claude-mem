using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.ApiKeys;

internal interface IApiKeyAccess
{
    Task<CursorPageResult<ApiKey>> GetApiKeys(GetApiKeysFilter filter, CancellationToken cancellationToken);
    Task<ApiKeyInsertResult> InsertApiKey(string teamId, string projectId, string actorId, string keyHash, CancellationToken cancellationToken);
    Task<ApiKeyLookup?> GetApiKey(string keyId, CancellationToken cancellationToken);
    Task RevokeApiKeyById(string keyId, CancellationToken cancellationToken);
}
