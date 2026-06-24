using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Features.Jobs;

internal sealed class JobAccess : IJobAccess
{
    private readonly NpgsqlDataSource _db;
    private readonly ICursorCodec _cursorCodec;

    public JobAccess(NpgsqlDataSource db, ICursorCodec cursorEncoder)
    {
        _db = db;
        _cursorCodec = cursorEncoder;
    }

    public async Task<CursorPageResult<Job>> GetJobs(GetJobsFilter filter, CancellationToken cancellationToken)
    {
        var opts = await CursorPageOptions.Create(filter, _cursorCodec, cancellationToken);

        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
            SELECT id, project_id, status, created_at, completed_at, failed_at
            FROM observation_generation_jobs
            /**where**/
            ORDER BY created_at DESC, id DESC
            LIMIT @FetchCount
            """, new { FetchCount = opts.FetchCount });

        if (filter.Status is not null)
        {
            builder.Where("status = @Status", new { Status = filter.Status });
        }

        if (filter.ProjectId is not null)
        {
            builder.Where("project_id = @ProjectId", new { ProjectId = filter.ProjectId });
        }

        if (opts.DecodedCursor is { } cur)
        {
            builder.Where("(created_at, id) < (@CursorCreatedAt, @CursorId)", new { CursorId = cur.Id, CursorCreatedAt = cur.CreatedAt });
        }

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        var jobRows = (await connection.QueryAsync<JobRow>(template.RawSql, template.Parameters)).AsList();

        var nextCursor = await opts.TrimAndGetNextCursor(jobRows, r => new CursorPayload(r.Id, r.CreatedAt), cancellationToken);

        var jobs = jobRows
            .Select(r => new Job(r.Id, r.ProjectId, r.Status, r.CreatedAt, r.CompletedAt, r.FailedAt))
            .ToList();

        return new CursorPageResult<Job>(jobs, filter.Cursor, nextCursor);
    }

    public async Task<string?> GetJobStatus(string jobId, CancellationToken cancellationToken)
    {
        const string selectJobStatusByIdSql =
            "SELECT status FROM observation_generation_jobs WHERE id = @JobId";

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(selectJobStatusByIdSql, new { JobId = jobId });
    }

    public async Task ResetJobToQueued(string jobId, CancellationToken cancellationToken)
    {
        const string resetFailedJobToQueuedByIdSql =
            "UPDATE observation_generation_jobs SET status = 'queued', failed_at = NULL, last_error = NULL, updated_at = NOW() WHERE id = @JobId";

        await using var connection = await _db.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(resetFailedJobToQueuedByIdSql, new { JobId = jobId });
    }

    private sealed record JobRow(
        string Id,
        string? ProjectId,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? FailedAt);
}
