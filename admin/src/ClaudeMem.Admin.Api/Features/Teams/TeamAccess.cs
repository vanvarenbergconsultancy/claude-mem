using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Features.Teams;

internal sealed record GetTeamsFilter(
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;

internal interface ITeamAccess
{
    Task<bool> TeamExists(string teamId, CancellationToken cancellationToken);
    Task<CursorPageResult<Team>> GetTeams(GetTeamsFilter filter, CancellationToken cancellationToken);
    Task<Team?> GetTeamById(string teamId, CancellationToken cancellationToken);
    Task<Team> CreateTeam(string name, CancellationToken cancellationToken);
}

internal sealed class TeamAccess : ITeamAccess
{
    private readonly NpgsqlDataSource _db;
    private readonly ICursorCodec _cursorCodec;

    public TeamAccess(NpgsqlDataSource db, ICursorCodec cursorEncoder)
    {
        _db = db;
        _cursorCodec = cursorEncoder;
    }

    public async Task<bool> TeamExists(string teamId, CancellationToken cancellationToken)
    {
        const string selectTeamExistenceByIdSql = "SELECT EXISTS(SELECT 1 FROM teams WHERE id = @TeamId)";

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(selectTeamExistenceByIdSql, new { TeamId = teamId });
    }

    public async Task<CursorPageResult<Team>> GetTeams(GetTeamsFilter filter, CancellationToken cancellationToken)
    {
        var opts = await CursorPageOptions.Create(filter, _cursorCodec, cancellationToken);

        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
            SELECT id, name, created_at
            FROM teams
            /**where**/
            ORDER BY created_at DESC, id DESC
            LIMIT @FetchCount
            """, new { FetchCount = opts.FetchCount });

        if (opts.DecodedCursor is { } cur)
        {
            builder.Where("(created_at, id) < (@CursorCreatedAt, @CursorId)", new { CursorId = cur.Id, CursorCreatedAt = cur.CreatedAt });
        }

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var teamRows = (await connection.QueryAsync<TeamRow>(template.RawSql, template.Parameters)).AsList();

        var nextCursor = await opts.TrimAndGetNextCursor(teamRows, r => new CursorPayload(r.Id, r.CreatedAt), cancellationToken);

        var teams = teamRows
            .Select(r => new Team(r.Id, r.Name, r.CreatedAt, projectCount: null))
            .ToList();

        return new CursorPageResult<Team>(teams, filter.Cursor, nextCursor);
    }

    public async Task<Team?> GetTeamById(string teamId, CancellationToken cancellationToken)
    {
        const string selectTeamWithProjectCountByIdSql = """
            SELECT
                t.id,
                t.name,
                t.created_at,
                COUNT(p.id) AS project_count
            FROM teams t
            LEFT JOIN projects p ON p.team_id = t.id
            WHERE t.id = @TeamId
            GROUP BY t.id, t.name, t.created_at
            """;

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<TeamDetailRow>(selectTeamWithProjectCountByIdSql, new { TeamId = teamId });

        if (row is null)
        {
            return null;
        }

        return new Team(row.Id, row.Name, row.CreatedAt, projectCount: (int)row.ProjectCount);
    }

    public async Task<Team> CreateTeam(string name, CancellationToken cancellationToken)
    {
        const string insertTeamReturningDetailSql = """
            INSERT INTO teams (id, name, created_at, updated_at)
            VALUES (@Id, @Name, NOW(), NOW())
            RETURNING id, name, created_at
            """;

        var newTeamId = Guid.NewGuid().ToString();

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<TeamRow>(insertTeamReturningDetailSql, new { Id = newTeamId, Name = name });

        return new Team(row.Id, row.Name, row.CreatedAt, projectCount: 0);
    }

    private sealed record TeamRow(string Id, string Name, DateTimeOffset CreatedAt);
    private sealed record TeamDetailRow(string Id, string Name, DateTimeOffset CreatedAt, long ProjectCount);
}
