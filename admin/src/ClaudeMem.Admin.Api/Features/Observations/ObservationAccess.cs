using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Features.Observations;

internal sealed record GetObservationsFilter(
    string? TeamId = null,
    string? ProjectId = null,
    string? SearchText = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;

internal interface IObservationAccess
{
    Task<CursorPageResult<Observation>> GetObservations(GetObservationsFilter filter, CancellationToken cancellationToken);
}

internal sealed class ObservationAccess : IObservationAccess
{
    private readonly NpgsqlDataSource _db;
    private readonly ICursorCodec _cursorCodec;

    public ObservationAccess(NpgsqlDataSource db, ICursorCodec cursorEncoder)
    {
        _db = db;
        _cursorCodec = cursorEncoder;
    }

    public async Task<CursorPageResult<Observation>> GetObservations(GetObservationsFilter filter, CancellationToken cancellationToken)
    {
        var opts = await CursorPageOptions.Create(filter, _cursorCodec, cancellationToken);

        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
            SELECT id, project_id, content, created_at
            FROM observations
            /**where**/
            ORDER BY created_at DESC, id DESC
            LIMIT @FetchCount
            """, new { FetchCount = opts.FetchCount });

        if (filter.ProjectId is not null)
        {
            builder.Where("project_id = @ProjectId", new { ProjectId = filter.ProjectId });
        }

        if (filter.TeamId is not null)
        {
            builder.Where("team_id = @TeamId", new { TeamId = filter.TeamId });
        }

        if (filter.SearchText is not null)
        {
            builder.Where("content_search @@ plainto_tsquery('english', @Search)", new { Search = filter.SearchText });
        }

        if (opts.DecodedCursor is { } cur)
        {
            builder.Where("(created_at, id) < (@CursorCreatedAt, @CursorId)", new { CursorId = cur.Id, CursorCreatedAt = cur.CreatedAt });
        }

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var observationRows = (await connection.QueryAsync<ObservationRow>(template.RawSql, template.Parameters)).AsList();

        var nextCursor = await opts.TrimAndGetNextCursor(observationRows, r => new CursorPayload(r.Id, r.CreatedAt), cancellationToken);

        var observations = observationRows
            .Select(r => new Observation(r.Id, r.ProjectId, r.Content, r.CreatedAt))
            .ToList();

        return new CursorPageResult<Observation>(observations, filter.Cursor, nextCursor);
    }

    private sealed record ObservationRow(string Id, string ProjectId, string Content, DateTimeOffset CreatedAt);
}
