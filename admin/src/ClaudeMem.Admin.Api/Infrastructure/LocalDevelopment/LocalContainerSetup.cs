using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ClaudeMem.Admin.Api.Infrastructure.LocalDevelopment;

/// <summary>
/// Starts an ephemeral local PostgreSQL container for development without a real database dependency.
/// Uses container reuse so restarts are fast; data persists across sessions.
/// To reset: docker rm -f the container with label reuse-id=claude-mem-admin-postgres-local.
/// </summary>
/// <remarks>
/// Requires Docker. On WSL2 without Docker Desktop, set DOCKER_HOST=tcp://127.0.0.1:2375
/// (not localhost — Windows resolves localhost to IPv6 first, causing 30+ second timeouts).
/// </remarks>
internal static class LocalContainerSetup
{
    private const string SchemaResourceName = "ClaudeMem.Admin.Api.Infrastructure.Database.schema.sql";

    public static async Task<string> StartContainer(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2068 // Hard-coded local-only dev credentials, not a production secret
        var container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("claude_mem_admin_local")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithReuse(true)
            .WithLabel("reuse-id", "claude-mem-admin-postgres-local")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(5432))
            .Build();
#pragma warning restore S2068

        try
        {
            await container.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to start local PostgreSQL via Testcontainers. Docker must be running. On WSL2 without Docker Desktop, set DOCKER_HOST=tcp://127.0.0.1:2375 (not localhost).", ex);
        }

        var connectionString = container.GetConnectionString();
        await ApplySchema(connectionString, cancellationToken);
        await SeedIfEmpty(connectionString, cancellationToken);

        return connectionString;
    }

    private static async Task ApplySchema(string connectionString, CancellationToken cancellationToken)
    {
        var assembly = typeof(LocalContainerSetup).Assembly;
        await using var stream = assembly.GetManifestResourceStream(SchemaResourceName)
                                 ?? throw new InvalidOperationException($"Embedded resource '{SchemaResourceName}' not found.");

        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedIfEmpty(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var teamCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM teams");
        if (teamCount > 0)
        {
            return;
        }

        var team1Id = Guid.NewGuid().ToString();
        var team2Id = Guid.NewGuid().ToString();
        var proj1Id = Guid.NewGuid().ToString();
        var proj2Id = Guid.NewGuid().ToString();
        var proj3Id = Guid.NewGuid().ToString();

        await SeedTeams(connection, team1Id, team2Id);
        await SeedProjects(connection, team1Id, team2Id, proj1Id, proj2Id, proj3Id);
        await SeedApiKeys(connection, team1Id, proj1Id, proj2Id);
        await SeedObservations(connection, team1Id, proj1Id);
        await SeedJobs(connection, team1Id, proj1Id);
    }

    private static async Task SeedTeams(NpgsqlConnection connection, string team1Id, string team2Id)
    {
        await connection.ExecuteAsync("""
            INSERT INTO teams (id, name, created_at, updated_at) VALUES
            (@Id1, 'Acme Corp',     NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'),
            (@Id2, 'Startup Labs',  NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days')
            """, new { Id1 = team1Id, Id2 = team2Id });
    }

    private static async Task SeedProjects(NpgsqlConnection connection, string team1Id, string team2Id, string proj1Id, string proj2Id, string proj3Id)
    {
        await connection.ExecuteAsync("""
            INSERT INTO projects (id, team_id, name, created_at, updated_at) VALUES
            (@P1, @T1, 'Main App',      NOW() - INTERVAL '25 days', NOW() - INTERVAL '25 days'),
            (@P2, @T1, 'Data Pipeline', NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'),
            (@P3, @T2, 'Beta Product',  NOW() - INTERVAL '8 days',  NOW() - INTERVAL '8 days')
            """, new { P1 = proj1Id, P2 = proj2Id, P3 = proj3Id, T1 = team1Id, T2 = team2Id });
    }

    private static async Task SeedApiKeys(NpgsqlConnection connection, string teamId, string proj1Id, string proj2Id)
    {
        await connection.ExecuteAsync("""
            INSERT INTO api_keys (id, key_hash, team_id, project_id, actor_id, created_at, updated_at) VALUES
            (@K1, 'local-seed-key-hash-1', @T, @P1, 'seed-actor', NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days'),
            (@K2, 'local-seed-key-hash-2', @T, @P2, 'seed-actor', NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days')
            """, new { K1 = Guid.NewGuid().ToString(), K2 = Guid.NewGuid().ToString(), T = teamId, P1 = proj1Id, P2 = proj2Id });
    }

    private static async Task SeedObservations(NpgsqlConnection connection, string teamId, string projectId)
    {
        string[] contents =
        [
            "The user navigated to the settings page and updated notification preferences for email alerts.",
            "Implemented cursor-based pagination for the teams list endpoint with stable ordering by created_at DESC.",
            "Investigated a slow query on observations filtered by project — added composite index on (team_id, project_id, created_at).",
            "Deployed schema migration to add content_search TSVECTOR column for full-text search on observations.",
            "Reviewed API key revocation flow; revoked_at is set but the key record is retained for audit purposes.",
        ];

        for (var i = 0; i < contents.Length; i++)
        {
            await connection.ExecuteAsync("""
                INSERT INTO observations (id, project_id, team_id, content, created_at, updated_at)
                VALUES (@Id, @ProjId, @TeamId, @Content, NOW() - INTERVAL '1 hour' * @Offset, NOW() - INTERVAL '1 hour' * @Offset)
                """, new { Id = Guid.NewGuid().ToString(), ProjId = projectId, TeamId = teamId, Content = contents[i], Offset = i + 1 });
        }
    }

    private static async Task SeedJobs(NpgsqlConnection connection, string teamId, string projectId)
    {
        await connection.ExecuteAsync("""
            INSERT INTO observation_generation_jobs
              (id, project_id, team_id, source_type, source_id, job_type, status, idempotency_key, completed_at, created_at, updated_at)
            VALUES
              (@J1, @P, @T, 'observation_reindex', 'reindex-seed-1', 'generate_observations', 'completed', 'seed-job-1', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
              (@J2, @P, @T, 'observation_reindex', 'reindex-seed-2', 'generate_observations', 'failed',    'seed-job-2', NULL,                      NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days')
            """, new { J1 = Guid.NewGuid().ToString(), J2 = Guid.NewGuid().ToString(), P = projectId, T = teamId });
    }
}
