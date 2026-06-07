using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;

/// <summary> Abstracts a PostgreSQL instance used in integration tests. </summary>
/// <remarks>
/// Implementations are resolved by <see cref="PostgresProviderFactory"/> based on configuration or environment.
/// Call <see cref="StartAsync"/> before running tests and <see cref="StopAsync"/> in teardown.
/// </remarks>
internal interface IPostgresProvider
{
    /// <summary>Gets the ADO.NET connection string for the running instance.</summary>
    /// <remarks>Only valid after <see cref="StartAsync"/> has completed successfully.</remarks>
    string ConnectionString { get; }

    /// <summary>Starts the PostgreSQL instance and populates <see cref="ConnectionString"/>.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops and disposes the PostgreSQL instance.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
