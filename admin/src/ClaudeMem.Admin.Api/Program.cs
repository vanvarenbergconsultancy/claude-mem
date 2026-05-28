using ClaudeMem.Admin.Api.Infrastructure.Auth;
using System.Linq;
using Dapper;
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
using ClaudeMem.Admin.Api.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

// ── API key ───────────────────────────────────────────────────────────────
var apiKey = LoadApiKey(builder.Configuration);
builder.Services.AddSingleton(new ApiKeyOptions { Key = apiKey });
builder.Services.AddSingleton<ApiKeyMiddleware>();

// ── Database ──────────────────────────────────────────────────────────────
DefaultTypeMap.MatchNamesWithUnderscores = true;
SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
SqlMapper.AddTypeHandler(new NullableDateTimeOffsetTypeHandler());
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));

// ── MVC ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);

// Return 422 for semantic validation failures (data annotation constraints like MinimumLength),
// 400 only for true binding failures where an exception was thrown (e.g. wrong type, malformed JSON).
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    ConfigureInvalidModelStateOptionsForCorrectReturnCodesAndResults(options);
});

builder.Services.AddOpenApi();

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────
static string LoadApiKey(IConfiguration configuration)
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

static void ConfigureInvalidModelStateOptionsForCorrectReturnCodesAndResults(ApiBehaviorOptions apiBehaviorOptions)
{
    apiBehaviorOptions.InvalidModelStateResponseFactory = context =>
    {
        var hasBindingFailure = context.ModelState.Values
            .Any(v => v.Errors.Any(e => e.Exception is not null));

        int status = hasBindingFailure ? StatusCodes.Status400BadRequest : StatusCodes.Status422UnprocessableEntity;

        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = status
        };

        return hasBindingFailure ? new BadRequestObjectResult(problemDetails) : new UnprocessableEntityObjectResult(problemDetails);
    };
}

// Expose for WebApplicationFactory in integration tests
public partial class Program { }
