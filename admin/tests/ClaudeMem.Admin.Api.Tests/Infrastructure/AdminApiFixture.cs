using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Npgsql;
using Respawn;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

public sealed class AdminApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestApiKey = "test-admin-api-key-for-integration-tests";

    private const string EnvVarAdminApiKey = "AdminApiKey";
    private const string EnvVarConnectionString = "ConnectionStrings__Default";

    private readonly IPostgresProvider _provider;
    private Respawner? _respawner;
    private Respawner DatabaseRespawner => _respawner ?? throw new InvalidOperationException($"{nameof(AdminApiFixture)}.{nameof(IAsyncLifetime.InitializeAsync)} has not completed.");

    private HttpClient? _authenticatedClient;

    public AdminApiFixture()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .Build();

        _provider = PostgresProviderFactory.Create(config);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Replace the NpgsqlDataSource that Program.cs registered with one that points at the test container, in case the env var is read at a different time than DI registration.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(_provider.ConnectionString));

            services.AddAdminApiClients("http://localhost", TestApiKey);

            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b =>
                    b.PrimaryHandler = Server.CreateHandler());
            });
        });
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
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

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        await _provider.StopAsync();

        Environment.SetEnvironmentVariable(EnvVarConnectionString, null);
        Environment.SetEnvironmentVariable(EnvVarAdminApiKey, null);
    }

    public string ConnectionString => _provider.ConnectionString;

    private ITeamsClient? _teamsClient;
    public ITeamsClient TeamsClient => _teamsClient ??= Services.GetRequiredService<ITeamsClient>();

    private IProjectsClient? _projectsClient;
    public IProjectsClient ProjectsClient => _projectsClient ??= Services.GetRequiredService<IProjectsClient>();

    private IApiKeysClient? _apiKeysClient;
    public IApiKeysClient ApiKeysClient => _apiKeysClient ??= Services.GetRequiredService<IApiKeysClient>();

    private IObservationsClient? _observationsClient;
    public IObservationsClient ObservationsClient => _observationsClient ??= Services.GetRequiredService<IObservationsClient>();

    private IJobsClient? _jobsClient;
    public IJobsClient JobsClient => _jobsClient ??= Services.GetRequiredService<IJobsClient>();

    private IAuditLogClient? _auditLogClient;
    public IAuditLogClient AuditLogClient => _auditLogClient ??= Services.GetRequiredService<IAuditLogClient>();

    public HttpClient CreateAuthenticatedClient()
    {
        if (_authenticatedClient is not null)
        {
            return _authenticatedClient;
        }

        _authenticatedClient = CreateClient();
        _authenticatedClient.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);

        return _authenticatedClient;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_provider.ConnectionString);
        await connection.OpenAsync();
        await DatabaseRespawner.ResetAsync(connection);
    }

    private async Task CreateSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(_provider.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = LoadSchemaSql();
        await cmd.ExecuteNonQueryAsync();
    }

    private static string LoadSchemaSql()
    {
        var assembly = typeof(Program).Assembly;
        const string sqlManifestResourceName = "ClaudeMem.Admin.Api.Infrastructure.Database.schema.sql";
        using var stream = assembly.GetManifestResourceStream(sqlManifestResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to load embedded resource '{sqlManifestResourceName}'.");
        }

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
