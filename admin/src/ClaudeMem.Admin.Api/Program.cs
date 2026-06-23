using ClaudeMem.Admin.Api.Infrastructure.Auth;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Validation;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;
using System.IO;
using System.Linq;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Database;

namespace ClaudeMem.Admin.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        AddApiKeyAuth(services, builder.Configuration);
        AddErrorHandling(services);
        AddApplicationLayer(services);

        AddAccessLayer(services, builder.Configuration);
        AddApiLayer(services);

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static void AddApiKeyAuth(IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = LoadApiKey(configuration);

        services
            .AddAuthentication("ApiKey")
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options =>
            {
                options.Key = apiKey;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
    }

    private static string LoadApiKey(IConfiguration configuration)
    {
        const string secretPath = "/run/secrets/admin_api_key";

        if (File.Exists(secretPath))
        {
            return File.ReadAllText(secretPath).Trim();
        }

        var adminApiKey = configuration["AdminApiKey"];
        if (string.IsNullOrWhiteSpace(adminApiKey))
        {
            throw new InvalidOperationException("Admin API key is not configured. Mount it as a Docker secret at /run/secrets/admin_api_key or set AdminApiKey in configuration.");
        }

        return adminApiKey;
    }

    private static void AddErrorHandling(IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
    }

    private static void AddApplicationLayer(IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
    }

    private static void AddAccessLayer(IServiceCollection services, IConfiguration configuration)
    {
        ConfigureDapperGlobalSettings();
        AddDbDataSource(services, configuration);
        AddAccessLayerServices(services);
    }

    private static void ConfigureDapperGlobalSettings()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeOffsetTypeHandler());
    }

    private static void AddDbDataSource(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
    }

    private static void AddAccessLayerServices(IServiceCollection services)
    {
        services.AddScoped<Features.Teams.ITeamAccess, Features.Teams.TeamAccess>();
        services.AddScoped<Features.Projects.Shared.IProjectAccess, Features.Projects.Shared.ProjectAccess>();
        services.AddScoped<Features.ApiKeys.IApiKeyAccess, Features.ApiKeys.ApiKeyAccess>();
        services.AddScoped<Features.Jobs.IJobAccess, Features.Jobs.JobAccess>();
        services.AddScoped<Features.Observations.IObservationAccess, Features.Observations.ObservationAccess>();
    }

    private static void AddApiLayer(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddPlainCursorPagination();

        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);

        services.Configure<ApiBehaviorOptions>(options =>
        {
            ConfigureInvalidModelStateOptionsForCorrectReturnCodesAndResults(options);
        });

        services.AddOpenApi();
    }

    /// <summary>
    /// Return 422 for semantic validation failures (data annotation constraints like MinimumLength),
    /// 400 only for true binding failures where an exception was thrown (e.g. wrong type, malformed JSON)
    /// </summary>
    /// <param name="apiBehaviorOptions"></param>
    private static void ConfigureInvalidModelStateOptionsForCorrectReturnCodesAndResults(ApiBehaviorOptions apiBehaviorOptions)
    {
        apiBehaviorOptions.InvalidModelStateResponseFactory = context =>
        {
            var hasBindingFailure = context.ModelState.Values
                .Any(v => v.Errors.Any(e => e.Exception is not null));

            int status = hasBindingFailure ? StatusCodes.Status400BadRequest : Constants.StatusCodeConventions.ValidationFailedStatusCode;

            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = status
            };

            return hasBindingFailure ? new BadRequestObjectResult(problemDetails) : new UnprocessableEntityObjectResult(problemDetails);
        };
    }
}
