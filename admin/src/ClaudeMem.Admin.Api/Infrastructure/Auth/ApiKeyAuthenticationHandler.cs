using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaudeMem.Admin.Api.Infrastructure.Auth;

/// <summary>ASP.NET Core authentication handler that validates the <c>X-Api-Key</c> header against a constant-time hash comparison.</summary>
internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeader = "X-Api-Key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var headerValues) || headerValues.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = Encoding.UTF8.GetBytes(headerValues.ToString());
        var expectedKey = Encoding.UTF8.GetBytes(Options.Key);

        if (!CryptographicOperations.FixedTimeEquals(providedKey, expectedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyClient") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
