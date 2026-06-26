using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Features.AuditLog;

internal sealed class AuditLogAccess : IAuditLogAccess
{
    private readonly NpgsqlDataSource _db;
    private readonly ICursorCodec _cursorCodec;

    public AuditLogAccess(NpgsqlDataSource db, ICursorCodec cursorCodec)
    {
        _db = db;
        _cursorCodec = cursorCodec;
    }

    public async Task<CursorPageResult<AuditLogEntry>> GetAuditLog(GetAuditLogFilter filter, CancellationToken cancellationToken)
    {
        var opts = await CursorPageOptions.Create(filter, _cursorCodec, cancellationToken);

        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
            SELECT
                al.id,
                al.team_id,
                t.name AS team_name,
                al.project_id,
                p.name AS project_name,
                al.actor_id,
                al.api_key_id,
                al.action,
                al.resource_type,
                al.resource_id,
                al.details::text AS details_json,
                al.created_at
            FROM audit_log al
            LEFT JOIN teams t ON t.id = al.team_id
            LEFT JOIN projects p ON p.id = al.project_id
            /**where**/
            ORDER BY al.created_at DESC, al.id DESC
            LIMIT @FetchCount
            """, new { FetchCount = opts.FetchCount });

        ApplyFilters(builder, filter, opts);

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<AuditLogRow>(template.RawSql, template.Parameters)).AsList();

        var nextCursor = await opts.TrimAndGetNextCursor(rows, r => new CursorPayload(r.Id, r.CreatedAt), cancellationToken);

        var entries = rows
            .Select(r => new AuditLogEntry(
                r.Id,
                r.TeamId,
                r.TeamName,
                r.ProjectId,
                r.ProjectName,
                r.ActorId,
                r.ApiKeyId,
                r.Action,
                r.ResourceType,
                r.ResourceId,
                string.IsNullOrEmpty(r.DetailsJson) ? null : JsonSerializer.Deserialize<IDictionary<string, object>>(r.DetailsJson),
                r.CreatedAt))
            .ToList();

        return new CursorPageResult<AuditLogEntry>(entries, filter.Cursor, nextCursor);
    }

    public async Task WriteEntry(AuditLogWriteData data, CancellationToken cancellationToken)
    {
        const string insertAuditLogEntrySql = """
            INSERT INTO audit_log (id, team_id, project_id, actor_id, api_key_id, action, resource_type, resource_id, details, created_at)
            VALUES (@Id, @TeamId, @ProjectId, @ActorId, @ApiKeyId, @Action, @ResourceType, @ResourceId, @Details::jsonb, NOW())
            """;

        try
        {
            await using var connection = await _db.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(insertAuditLogEntrySql, new
            {
                Id = Guid.NewGuid().ToString(),
                data.TeamId,
                data.ProjectId,
                data.ActorId,
                data.ApiKeyId,
                data.Action,
                data.ResourceType,
                data.ResourceId,
                data.Details
            });
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    private static void ApplyFilters(SqlBuilder builder, GetAuditLogFilter filter, CursorPageOptions opts)
    {
        if (filter.TeamId is not null)
        {
            builder.Where("al.team_id = @TeamId", new { TeamId = filter.TeamId });
        }

        if (filter.ProjectId is not null)
        {
            builder.Where("al.project_id = @ProjectId", new { ProjectId = filter.ProjectId });
        }

        if (filter.ApiKeyId is not null)
        {
            builder.Where("al.api_key_id = @ApiKeyId", new { ApiKeyId = filter.ApiKeyId });
        }

        if (filter.ActorId is not null)
        {
            builder.Where("al.actor_id = @ActorId", new { ActorId = filter.ActorId });
        }

        if (filter.Action is not null)
        {
            builder.Where("al.action = @Action", new { Action = filter.Action });
        }

        if (filter.ResourceType is not null)
        {
            builder.Where("al.resource_type = @ResourceType", new { ResourceType = filter.ResourceType });
        }

        if (filter.From is not null)
        {
            builder.Where("al.created_at >= @From", new { From = filter.From.Value.ToUniversalTime() });
        }

        if (filter.To is not null)
        {
            builder.Where("al.created_at <= @To", new { To = filter.To.Value.ToUniversalTime() });
        }

        if (opts.DecodedCursor is { } cur)
        {
            builder.Where("(al.created_at, al.id) < (@CursorCreatedAt, @CursorId)", new { CursorId = cur.Id, CursorCreatedAt = cur.CreatedAt });
        }
    }

    private sealed record AuditLogRow(
        string Id,
        string? TeamId,
        string? TeamName,
        string? ProjectId,
        string? ProjectName,
        string? ActorId,
        string? ApiKeyId,
        string Action,
        string ResourceType,
        string? ResourceId,
        string? DetailsJson,
        DateTimeOffset CreatedAt);
}
