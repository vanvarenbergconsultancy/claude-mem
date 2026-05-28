using System;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeMem.Admin.Api.Contracts;

public static class AdminApiServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApiClients(this IServiceCollection services, string baseUrl, string apiKey)
    {
        void Configure(System.Net.Http.HttpClient client)
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/');
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        services.AddHttpClient<ITeamsClient, TeamsClient>(Configure);
        services.AddHttpClient<IProjectsClient, ProjectsClient>(Configure);
        services.AddHttpClient<IApiKeysClient, ApiKeysClient>(Configure);
        services.AddHttpClient<IObservationsClient, ObservationsClient>(Configure);
        services.AddHttpClient<IJobsClient, JobsClient>(Configure);

        return services;
    }
}
