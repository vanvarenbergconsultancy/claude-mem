using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure.PostgresProviders;

/// <summary>
/// Resolves the <see cref="IPostgresProvider"/> to use for a test run.
/// </summary>
/// <remarks>
/// Resolution order: <c>forceProvider</c> argument → <c>appsettings["PostgresProvider"]</c> →
/// <c>TEST_POSTGRES_PROVIDER</c> environment variable → default (<see cref="PostgresProviderType.Testcontainers"/>).
/// </remarks>
internal static class PostgresProviderFactory
{
    private const string ConfigKey = "PostgresProvider";
    private const string DefaultProviderName = "testcontainers";
    private const string EnvVarKey = "TEST_POSTGRES_PROVIDER";

    private static readonly List<string> ValidProviderNames = [DefaultProviderName];

    /// <summary>Creates an <see cref="IPostgresProvider"/> using the resolved provider type.</summary>
    /// <param name="configuration">Optional test configuration. Checked for the <c>PostgresProvider</c> key.</param>
    /// <param name="forceProvider">When set, bypasses all other resolution and uses this provider directly.</param>
    public static IPostgresProvider Create(IConfiguration? configuration = null, PostgresProviderType? forceProvider = null)
    {
        var providerName = FindPostgresProviderFromConfigSourcesOrUseDefaultTestContainers(configuration);
        var providerType = forceProvider ?? FindTypeFromName(providerName);
        var provider = ResolveProvider(providerType);

        return provider;
    }

    private static string FindPostgresProviderFromConfigSourcesOrUseDefaultTestContainers(IConfiguration? configuration)
    {
        return configuration?[ConfigKey] ?? Environment.GetEnvironmentVariable(EnvVarKey) ?? DefaultProviderName;
    }

    private static PostgresProviderType FindTypeFromName(string providerName)
    {
        var validProviderNamesCommaSeparated = string.Join(", ", ValidProviderNames);

        return providerName.Trim().ToLowerInvariant() switch
        {
            "testcontainers" => PostgresProviderType.Testcontainers,
            _ => throw new InvalidOperationException($"Unknown PostgresProvider value: '{providerName}'. Valid values: {validProviderNamesCommaSeparated}. Set via appsettings.Test.json key '{ConfigKey}' or environment variable '{EnvVarKey}'.")
        };
    }

    // We return the interface type here to allow for more flexible provider implementations in the future, even though currently we only have one concrete provider.
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    private static IPostgresProvider ResolveProvider(PostgresProviderType providerType)
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
    {
        return providerType switch
        {
            PostgresProviderType.Testcontainers => new TestcontainersPostgresProvider(),
            _ => throw new InvalidOperationException($"Unknown PostgresProviderType: {providerType}")
        };
    }
}
