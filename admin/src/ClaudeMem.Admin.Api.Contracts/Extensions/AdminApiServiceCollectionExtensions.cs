using System;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeMem.Admin.Api.Contracts;

/// <summary>Extension methods for registering Admin API typed HTTP clients.</summary>
public static class AdminApiServiceCollectionExtensions
{
    /// <summary>Registers typed HTTP clients for all Admin API resources, configured with the given base URL and API key.</summary>
    public static IServiceCollection AddAdminApiClients(this IServiceCollection services, string baseUrl, string apiKey)
    {
        services.AddHttpClient<ITeamsClient, TeamsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IProjectsClient, ProjectsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IApiKeysClient, ApiKeysClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IObservationsClient, ObservationsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IJobsClient, JobsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IAuditLogClient, AuditLogClient>(client => Configure(client, baseUrl, apiKey));

        return services;
    }

    private static void Configure(System.Net.Http.HttpClient client, string baseUrl, string apiKey)
    {
        var trimmedBaseUrl = $"{baseUrl.TrimEnd('/')}/";
        client.BaseAddress = new Uri(trimmedBaseUrl);
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }
}
