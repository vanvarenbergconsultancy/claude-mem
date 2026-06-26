using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

internal sealed class DbHelper
{
    private readonly string _connectionString;

    public DbHelper(AdminApiFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<string> InsertTeam(string name = "Test Team")
    {
        var id = Guid.NewGuid().ToString();

        await using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "INSERT INTO teams (id, name, created_at, updated_at) VALUES (@Id, @Name, NOW(), NOW())",
            new { Id = id, Name = name });

        return id;
    }

    public async Task<string> InsertProject(string teamId, string name = "Test Project")
    {
        var id = Guid.NewGuid().ToString();

        await using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "INSERT INTO projects (id, team_id, name, created_at, updated_at) VALUES (@Id, @TeamId, @Name, NOW(), NOW())",
            new { Id = id, TeamId = teamId, Name = name });

        return id;
    }

    public async Task<string> InsertApiKey(string teamId, string projectId, string actorId = "test-actor", string? keyHash = null)
    {
        var id = Guid.NewGuid().ToString();
        var hash = keyHash ?? Guid.NewGuid().ToString("N");

        await using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "INSERT INTO api_keys (id, key_hash, team_id, project_id, actor_id, created_at, updated_at) VALUES (@Id, @KeyHash, @TeamId, @ProjectId, @ActorId, NOW(), NOW())",
            new { Id = id, KeyHash = hash, TeamId = teamId, ProjectId = projectId, ActorId = actorId });

        return id;
    }

    public async Task<string> InsertObservation(string teamId, string projectId, string content = "Test observation content.")
    {
        var id = Guid.NewGuid().ToString();

        await using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "INSERT INTO observations (id, project_id, team_id, content, created_at, updated_at) VALUES (@Id, @ProjectId, @TeamId, @Content, NOW(), NOW())",
            new { Id = id, ProjectId = projectId, TeamId = teamId, Content = content });

        return id;
    }

    public async Task<string> InsertJob(string teamId, string projectId, string status = "queued", string sourceType = "observation_reindex")
    {
        var id = Guid.NewGuid().ToString();
        var idempotencyKey = Guid.NewGuid().ToString();

        await using var connection = CreateConnection();

        await connection.ExecuteAsync(
            """
            INSERT INTO observation_generation_jobs
              (id, project_id, team_id, source_type, source_id, job_type, status, idempotency_key, created_at, updated_at)
            VALUES
              (@Id, @ProjectId, @TeamId, @SourceType, @SourceId, 'generate', @Status, @IdempotencyKey, NOW(), NOW())
            """,
            new
            {
                Id = id,
                ProjectId = projectId,
                TeamId = teamId,
                SourceType = sourceType,
                SourceId = id,
                Status = status,
                IdempotencyKey = idempotencyKey
            });

        return id;
    }

    public async Task RevokeApiKey(string keyId)
    {
        await using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "UPDATE api_keys SET revoked_at = NOW(), updated_at = NOW() WHERE id = @KeyId",
            new { KeyId = keyId });
    }
}
