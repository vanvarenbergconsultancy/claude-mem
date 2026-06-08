using System;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeMem.Admin.Api.Contracts;

public static class AdminApiServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApiClients(this IServiceCollection services, string baseUrl, string apiKey)
    {
        services.AddHttpClient<ITeamsClient, TeamsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IProjectsClient, ProjectsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IApiKeysClient, ApiKeysClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IObservationsClient, ObservationsClient>(client => Configure(client, baseUrl, apiKey));
        services.AddHttpClient<IJobsClient, JobsClient>(client => Configure(client, baseUrl, apiKey));

        return services;
    }

    private static void Configure(System.Net.Http.HttpClient client, string baseUrl, string apiKey)
    {
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/');
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }
}
