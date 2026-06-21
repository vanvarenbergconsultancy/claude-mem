using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;

/// <summary> <see cref="IPostgresProvider"/> that spins up an ephemeral <c>postgres:17-alpine</c> container via Testcontainers. </summary>
/// <remarks>
/// Requires Docker to be available and running.
/// On WSL2 without Docker Desktop, expose the Docker daemon over TCP and set <c>DOCKER_HOST=tcp://127.0.0.1:2375</c>
/// on Windows before running the tests. Use 127.0.0.1, not localhost — Windows resolves localhost to IPv6 first,
/// causing a 1-3s timeout per Docker API call before falling back to IPv4.
/// </remarks>
internal sealed class TestcontainersPostgresProvider : IPostgresProvider
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("claude_mem_admin_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithReuse(true)
            .WithLabel("reuse-id", "claude-mem-admin-postgres-test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(5432))
            .Build();

        try
        {
            await _container.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to start PostgreSQL via Testcontainers. Docker must be available and running. " +
                "On WSL2 without Docker Desktop, expose the Docker daemon over TCP and set " +
                "DOCKER_HOST=tcp://127.0.0.1:2375 on Windows (not localhost — see remarks). " +
                "See README.md for full setup instructions.",
                ex);
        }

        ConnectionString = _container.GetConnectionString();
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        // Leave the container alive for the next run; Testcontainers reattaches via the reuse hash.
        _container = null;
        return Task.CompletedTask;
    }
}
