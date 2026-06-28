using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Ui.Components;
using ClaudeMem.Admin.Ui.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents(options =>
    options.DetailedErrors = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local"))
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddOptions<AdminApiOptions>()
    .BindConfiguration("AdminApi")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.Retry.DisableForUnsafeHttpMethods();
    });
});

builder.Services.AddAdminApiClients(
    builder.Configuration["AdminApi:BaseUrl"]!,
    builder.Configuration["AdminApi:ApiKey"]!);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheInvalidationBus, CacheInvalidationBus>();
builder.Services.AddSingleton<ITeamCacheService, TeamCacheService>();
builder.Services.AddSingleton<IProjectCacheService, ProjectCacheService>();

if (builder.Environment.IsEnvironment("Local"))
{
    builder.WebHost.UseStaticWebAssets();

    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    builder.Configuration.AddUserSecrets<App>(optional: true);
}

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
