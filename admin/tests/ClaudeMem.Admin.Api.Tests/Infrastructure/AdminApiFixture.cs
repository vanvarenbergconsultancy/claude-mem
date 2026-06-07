using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

public sealed class AdminApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestApiKey = "test-admin-api-key-for-integration-tests";

    private const string EnvVarAdminApiKey = "AdminApiKey";
    private const string EnvVarConnectionString = "ConnectionStrings__Default";

    private IPostgresProvider? _provider;
    private Respawner? _respawner;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Replace the NpgsqlDataSource that Program.cs registered with one that points at the test container, in case the env var is read at a different time than DI registration.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(_provider!.ConnectionString));
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .Build();

        _provider = PostgresProviderFactory.Create(config);
        await _provider.StartAsync();

        // Env vars are the only configuration source readable before WebApplicationFactory callbacks run.
        // Double-underscore maps to colon in .NET config.
        Environment.SetEnvironmentVariable(EnvVarConnectionString, _provider.ConnectionString);
        Environment.SetEnvironmentVariable(EnvVarAdminApiKey, TestApiKey);

        await CreateSchemaAsync();

        await using var connection = new NpgsqlConnection(_provider.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore =
            [
                new Respawn.Graph.Table("server_beta_schema_migrations")
            ]
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();

        if (_provider is not null)
        {
            await _provider.StopAsync();
        }

        Environment.SetEnvironmentVariable(EnvVarConnectionString, null);
        Environment.SetEnvironmentVariable(EnvVarAdminApiKey, null);
    }

    public string ConnectionString => _provider!.ConnectionString;

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);

        return client;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_provider!.ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    private async Task CreateSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(_provider!.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = LoadSchemaSql();
        await cmd.ExecuteNonQueryAsync();
    }

    private static string LoadSchemaSql()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string sqlManifestResourceName = "ClaudeMem.Admin.Api.Tests.Infrastructure.schema.sql";
        using var stream = assembly.GetManifestResourceStream(sqlManifestResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to load embedded resource '{sqlManifestResourceName}'.");
        }

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
