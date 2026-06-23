using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Features.ApiKeys;

internal sealed record GetApiKeysFilter(
    string? ProjectId = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;

internal sealed record ApiKeyInsertResult(string Id, string ActorId, DateTimeOffset CreatedAt);

internal sealed record ApiKeyLookup(string Id, string TeamId, string ProjectId);

internal interface IApiKeyAccess
{
    Task<CursorPageResult<ApiKey>> GetApiKeys(GetApiKeysFilter filter, CancellationToken cancellationToken);
    Task<ApiKeyInsertResult> InsertApiKey(string teamId, string projectId, string actorId, string keyHash, CancellationToken cancellationToken);
    Task<ApiKeyLookup?> GetApiKey(string keyId, CancellationToken cancellationToken);
    Task RevokeApiKeyById(string keyId, CancellationToken cancellationToken);
}

internal sealed class ApiKeyAccess : IApiKeyAccess
{
    private readonly NpgsqlDataSource _db;
    private readonly ICursorCodec _cursorCodec;

    public ApiKeyAccess(NpgsqlDataSource db, ICursorCodec cursorEncoder)
    {
        _db = db;
        _cursorCodec = cursorEncoder;
    }

    public async Task<CursorPageResult<ApiKey>> GetApiKeys(GetApiKeysFilter filter, CancellationToken cancellationToken)
    {
        var opts = await CursorPageOptions.Create(filter, _cursorCodec, cancellationToken);

        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
            SELECT id, project_id, actor_id, created_at, expires_at
            FROM api_keys
            /**where**/
            ORDER BY created_at DESC, id DESC
            LIMIT @FetchCount
            """, new { FetchCount = opts.FetchCount });

        builder.Where("revoked_at IS NULL");

        if (filter.ProjectId is not null)
        {
            builder.Where("project_id = @ProjectId", new { ProjectId = filter.ProjectId });
        }

        if (opts.DecodedCursor is { } cur)
        {
            builder.Where("(created_at, id) < (@CursorCreatedAt, @CursorId)", new { CursorId = cur.Id, CursorCreatedAt = cur.CreatedAt });
        }

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var apiKeyRows = (await connection.QueryAsync<ApiKeyRow>(template.RawSql, template.Parameters)).AsList();

        var nextCursor = await opts.TrimAndGetNextCursor(apiKeyRows, r => new CursorPayload(r.Id, r.CreatedAt), cancellationToken);

        var apiKeys = apiKeyRows
            .Select(r => new ApiKey(r.Id, r.ProjectId, r.ActorId, r.ExpiresAt, r.CreatedAt))
            .ToList();

        return new CursorPageResult<ApiKey>(apiKeys, filter.Cursor, nextCursor);
    }

    public async Task<ApiKeyInsertResult> InsertApiKey(string teamId, string projectId, string actorId, string keyHash, CancellationToken cancellationToken)
    {
        const string insertApiKeyReturningIdActorIdCreatedAtSql = """
            INSERT INTO api_keys (id, key_hash, team_id, project_id, actor_id, created_at, updated_at)
            VALUES (@Id, @KeyHash, @TeamId, @ProjectId, @ActorId, NOW(), NOW())
            RETURNING id, actor_id, created_at
            """;

        var newApiKeyId = Guid.NewGuid().ToString();

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<ApiKeyInsertRow>(insertApiKeyReturningIdActorIdCreatedAtSql, new
        {
            Id = newApiKeyId,
            KeyHash = keyHash,
            TeamId = teamId,
            ProjectId = projectId,
            ActorId = actorId
        });

        return new ApiKeyInsertResult(row.Id, row.ActorId, row.CreatedAt);
    }

    public async Task<ApiKeyLookup?> GetApiKey(string keyId, CancellationToken cancellationToken)
    {
        const string selectActiveApiKeyOwnershipByIdSql = """
            SELECT id, team_id, project_id
            FROM api_keys
            WHERE id = @KeyId AND revoked_at IS NULL
            """;

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ApiKeyLookupRow>(selectActiveApiKeyOwnershipByIdSql, new { KeyId = keyId });

        if (row is null)
        {
            return null;
        }

        return new ApiKeyLookup(row.Id, row.TeamId, row.ProjectId);
    }

    public async Task RevokeApiKeyById(string keyId, CancellationToken cancellationToken)
    {
        const string revokeActiveApiKeyByIdSql = """
            UPDATE api_keys
            SET revoked_at = NOW(), updated_at = NOW()
            WHERE id = @KeyId AND revoked_at IS NULL
            """;

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(revokeActiveApiKeyByIdSql, new { KeyId = keyId });
    }

    private sealed record ApiKeyRow(
        string Id,
        string ProjectId,
        string ActorId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt);

    private sealed record ApiKeyInsertRow(string Id, string ActorId, DateTimeOffset CreatedAt);

    private sealed record ApiKeyLookupRow(string Id, string TeamId, string ProjectId);
}
