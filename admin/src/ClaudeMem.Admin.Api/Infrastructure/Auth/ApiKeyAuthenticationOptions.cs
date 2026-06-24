using Microsoft.AspNetCore.Authentication;

namespace ClaudeMem.Admin.Api.Infrastructure.Auth;

/// <summary>Options for the API key authentication scheme.</summary>
internal sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>The expected raw API key value read from configuration.</summary>
    public string Key { get; set; } = string.Empty;
}
