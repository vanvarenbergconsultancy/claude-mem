using Microsoft.AspNetCore.Authentication;

namespace ClaudeMem.Admin.Api.Infrastructure.Auth;

internal sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Key { get; set; } = string.Empty;
}
