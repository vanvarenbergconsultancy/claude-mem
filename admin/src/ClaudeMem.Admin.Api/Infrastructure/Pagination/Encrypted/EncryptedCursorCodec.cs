using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;

/// <summary>Cursor codec that encrypts payloads using ASP.NET Core Data Protection, making them fully opaque and tamper-proof.</summary>
internal sealed class EncryptedCursorCodec : ICursorCodec
{
    private readonly IDataProtector _protector;

    public EncryptedCursorCodec(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ClaudeMem.Admin.Api.Pagination.Cursor.v1");
    }

    public Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload);

        return Task.FromResult(_protector.Protect(json));
    }

    public Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            return Task.FromResult<CursorPayload?>(null);
        }

        try
        {
            var json = _protector.Unprotect(token);

            return Task.FromResult(JsonSerializer.Deserialize<CursorPayload>(json));
        }
        catch (CryptographicException)
        {
            throw new InvalidCursorException();
        }
    }
}
