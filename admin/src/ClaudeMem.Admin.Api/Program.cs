using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Infrastructure;
using ClaudeMem.Admin.Api.Infrastructure.Auth;
using ClaudeMem.Admin.Api.Infrastructure.Database;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Validation;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ClaudeMem.Admin.Api;

public class Program
{
    protected Program() { }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isLocalEnvironment = builder.Environment.IsEnvironment(Constants.Environments.Local);
        var services = builder.Services;

        AddApiKeyAuth(services, builder.Configuration);
        AddErrorHandling(services);
        AddApplicationLayer(services);
        
        if (isLocalEnvironment && builder.Configuration.GetValue<bool>(Constants.AppSettings.UseLocalContainerKey))
        {
            var connectionString = await Infrastructure.LocalDevelopment.LocalContainerSetup.StartContainer(CancellationToken.None);
            AddAccessLayer(services, connectionString);
        }
        else
        {
            AddAccessLayer(services, builder.Configuration);
        }

        AddApiLayer(services);

        if (isLocalEnvironment)
        {
            builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });

            builder.Configuration.AddUserSecrets<Program>(optional: true);
        }

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment() || isLocalEnvironment)
        {
            app.MapGet("/openapi/v1.yaml", async (HttpContext ctx, IWebHostEnvironment env) =>
            {
                var openApiPath = Path.Combine(env.ContentRootPath, "openapi", "v1.yaml");
                ctx.Response.ContentType = "application/yaml; charset=utf-8";
                await ctx.Response.SendFileAsync(openApiPath, ctx.RequestAborted);
            })
                .ExcludeFromDescription()
                .AllowAnonymous();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }

    private static void AddApiKeyAuth(IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = LoadApiKey(configuration);

        services
            .AddAuthentication(Constants.Authentication.ApiKeySchemeName)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(Constants.Authentication.ApiKeySchemeName, options =>
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
        const string secretPath = Constants.Docker.AdminApiKeySecretPath;
        if (File.Exists(secretPath))
        {
            return File.ReadAllText(secretPath).Trim();
        }

        var adminApiKey = configuration[Constants.AppSettings.AdminApiKey];
        if (string.IsNullOrWhiteSpace(adminApiKey))
        {
            throw new InvalidOperationException($"Admin API key is not configured. Mount it as a Docker secret at {Constants.Docker.AdminApiKeySecretPath} or set {Constants.AppSettings.AdminApiKey} in configuration.");
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

    private static void AddAccessLayer(IServiceCollection services, string connectionString)
    {
        ConfigureDapperGlobalSettings();
        services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
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
        var connectionString = configuration.GetConnectionString(Constants.ConnectionStrings.ConnectionStringName)
                               ?? throw new InvalidOperationException($"Connection string '{Constants.ConnectionStrings.ConnectionStringName}' is not configured.");

        services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
    }

    private static void AddAccessLayerServices(IServiceCollection services)
    {
        services.AddScoped<Features.Teams.ITeamAccess, Features.Teams.TeamAccess>();
        services.AddScoped<Features.Projects.Shared.IProjectAccess, Features.Projects.Shared.ProjectAccess>();
        services.AddScoped<Features.ApiKeys.IApiKeyAccess, Features.ApiKeys.ApiKeyAccess>();
        services.AddScoped<Features.Jobs.IJobAccess, Features.Jobs.JobAccess>();
        services.AddScoped<Features.Observations.IObservationAccess, Features.Observations.ObservationAccess>();
        services.AddScoped<Features.AuditLog.IAuditLogAccess, Features.AuditLog.AuditLogAccess>();
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
