using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

internal sealed class ProjectAccess : IProjectAccess
{
    private readonly NpgsqlDataSource _db;
    private readonly ICursorCodec _cursorCodec;

    public ProjectAccess(NpgsqlDataSource db, ICursorCodec cursorEncoder)
    {
        _db = db;
        _cursorCodec = cursorEncoder;
    }

    public async Task<ProjectTeamExistence> CheckProjectTeamExistence(string teamId, string projectId, CancellationToken cancellationToken)
    {
        const string selectTeamAndProjectExistenceByIdsSql = """
            SELECT
                EXISTS(SELECT 1 FROM teams WHERE id = @TeamId) AS team_exists,
                EXISTS(SELECT 1 FROM projects WHERE id = @ProjectId AND team_id = @TeamId) AS belongs_to_team
            """;

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var existenceRow = await connection.QuerySingleAsync<ExistenceRow>(selectTeamAndProjectExistenceByIdsSql, new { TeamId = teamId, ProjectId = projectId });

        return new ProjectTeamExistence(existenceRow.TeamExists, existenceRow.BelongsToTeam);
    }

    public async Task<CursorPageResult<Project>> GetProjects(GetProjectsFilter filter, CancellationToken cancellationToken)
    {
        var opts = await CursorPageOptions.Create(filter, _cursorCodec, cancellationToken);

        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
            SELECT id, team_id, name, created_at
            FROM projects
            /**where**/
            ORDER BY created_at DESC, id DESC
            LIMIT @FetchCount
            """, new { FetchCount = opts.FetchCount });

        if (filter.TeamId is not null)
        {
            builder.Where("team_id = @TeamId", new { TeamId = filter.TeamId });
        }

        if (opts.DecodedCursor is { } cur)
        {
            builder.Where("(created_at, id) < (@CursorCreatedAt, @CursorId)", new { CursorId = cur.Id, CursorCreatedAt = cur.CreatedAt });
        }

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var projectRows = (await connection.QueryAsync<ProjectRow>(template.RawSql, template.Parameters)).AsList();

        var nextCursor = await opts.TrimAndGetNextCursor(projectRows, r => new CursorPayload(r.Id, r.CreatedAt), cancellationToken);

        var projects = projectRows
            .Select(r => new Project(r.Id, r.TeamId, r.Name, r.CreatedAt, apiKeyCount: null, observationCount: null))
            .ToList();

        return new CursorPageResult<Project>(projects, filter.Cursor, nextCursor);
    }

    public async Task<Project?> GetProjectById(string teamId, string projectId, CancellationToken cancellationToken)
    {
        const string selectProjectWithCountsByIdSql = """
            SELECT
                p.id,
                p.team_id,
                p.name,
                p.created_at,
                COUNT(DISTINCT ak.id) AS api_key_count,
                COUNT(DISTINCT o.id) AS observation_count
            FROM projects p
            LEFT JOIN api_keys ak ON ak.project_id = p.id AND ak.revoked_at IS NULL
            LEFT JOIN observations o ON o.project_id = p.id
            WHERE p.id = @ProjectId
              AND p.team_id = @TeamId
            GROUP BY p.id, p.team_id, p.name, p.created_at
            """;

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProjectDetailRow>(selectProjectWithCountsByIdSql, new { TeamId = teamId, ProjectId = projectId });

        if (row is null)
        {
            return null;
        }

        return new Project(row.Id, row.TeamId, row.Name, row.CreatedAt, apiKeyCount: (int)row.ApiKeyCount, observationCount: (int)row.ObservationCount);
    }

    public async Task<Project> CreateProject(string teamId, string name, CancellationToken cancellationToken)
    {
        const string insertProjectReturningDetailSql = """
            INSERT INTO projects (id, team_id, name, created_at, updated_at)
            VALUES (@Id, @TeamId, @Name, NOW(), NOW())
            RETURNING id, team_id, name, created_at
            """;

        var newProjectId = Guid.NewGuid().ToString();

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<ProjectRow>(insertProjectReturningDetailSql, new
        {
            Id = newProjectId,
            TeamId = teamId,
            Name = name
        });

        return new Project(row.Id, row.TeamId, row.Name, row.CreatedAt, apiKeyCount: 0, observationCount: 0);
    }

    private sealed record ProjectRow(string Id, string TeamId, string Name, DateTimeOffset CreatedAt);
    private sealed record ProjectDetailRow(string Id, string TeamId, string Name, DateTimeOffset CreatedAt, long ApiKeyCount, long ObservationCount);
    private sealed record ExistenceRow(bool TeamExists, bool BelongsToTeam);
}
