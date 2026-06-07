namespace ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;

/// <summary>Identifies the PostgreSQL backend used during test execution.</summary>
/// <seealso cref="PostgresProviderFactory"/>
internal enum PostgresProviderType
{
    /// <summary>Spins up an ephemeral PostgreSQL container via Testcontainers. Requires Docker.</summary>
    Testcontainers
}
