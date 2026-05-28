using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ClaudeMem.Admin.Api.Infrastructure.Auth;

/// <summary>
/// Middleware that enforces API key authentication on every request.
/// The expected key is pre-encoded once at startup and compared using a constant-time algorithm to prevent timing-based attacks.
/// </summary>
internal sealed class ApiKeyMiddleware : IMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly byte[] _expectedKeyBytes;

    public ApiKeyMiddleware(ApiKeyOptions options)
    {
        _expectedKeyBytes = Encoding.UTF8.GetBytes(options.Key);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Reject early when the header is absent or empty — no key provided.
        var (hasAuthHeaderAndKey, apiKeyHeaderValues) = HasAuthHeaderAndValue(context);
        if (!hasAuthHeaderAndKey)
        {
            SetUnauthorizedResponse(context);

            return;
        }

        var providedApiKeyInBytesFromHeader = Encoding.UTF8.GetBytes(apiKeyHeaderValues);
        if (!AreBytesEqualUsingTamperingCheck(providedApiKeyInBytesFromHeader, _expectedKeyBytes))
        {
            SetUnauthorizedResponse(context);

            return;
        }

        await next(context);
    }

    private static (bool hasValue, string value) HasAuthHeaderAndValue(HttpContext context)
    {
        var hasHeader = context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyHeaderValues);
        var headerHasValues = apiKeyHeaderValues.Count > 0;
        if (hasHeader && headerHasValues)
        {
            return (true, apiKeyHeaderValues.ToString());
        }

        return (false, string.Empty);
    }

    /// <summary> FixedTimeEquals compares every byte regardless of where a mismatch occurs, preventing attackers from inferring the key length or value via timing differences. </summary>
    /// <param name="a">The first byte array to compare.</param>
    /// <param name="b">The second byte array to compare.</param>
    /// <returns>True if the byte arrays are equal; otherwise, false.</returns>
    private static bool AreBytesEqualUsingTamperingCheck(byte[] a, byte[] b)
    {
       return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static void SetUnauthorizedResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }
}
