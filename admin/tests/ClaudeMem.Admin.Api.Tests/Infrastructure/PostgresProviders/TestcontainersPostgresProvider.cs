using System;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;

/// <summary> <see cref="IPostgresProvider"/> that spins up an ephemeral <c>postgres:17-alpine</c> container via Testcontainers. </summary>
/// <remarks>
/// Requires Docker to be available and running.
/// On WSL2 without Docker Desktop, expose the Docker daemon over TCP and set <c>DOCKER_HOST=tcp://localhost:2375</c> on Windows before running the tests.
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
                "DOCKER_HOST=tcp://localhost:2375 on Windows. See README.md for full setup instructions.",
                ex);
        }

        ConnectionString = _container.GetConnectionString();
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
