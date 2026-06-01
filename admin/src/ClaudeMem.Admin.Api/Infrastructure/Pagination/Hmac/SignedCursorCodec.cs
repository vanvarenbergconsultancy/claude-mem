using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;

internal sealed class SignedCursorCodec : ICursorCodec
{
    private readonly byte[] _keyBytes;

    public SignedCursorCodec(string signingKey)
    {
        _keyBytes = Encoding.UTF8.GetBytes(signingKey);
    }

    public Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var b64 = Convert.ToBase64String(jsonBytes);
        var b64Bytes = Encoding.UTF8.GetBytes(b64);
        var sig = HMACSHA256.HashData(_keyBytes, b64Bytes);

        return Task.FromResult($"{b64}.{Convert.ToBase64String(sig)}");
    }

    public Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            return Task.FromResult<CursorPayload?>(null);
        }

        var dotIndex = token.LastIndexOf('.');
        if (dotIndex < 0)
        {
            throw new InvalidCursorException();
        }

        var b64Part = token[..dotIndex];
        var sigPart = token[(dotIndex + 1)..];

        try
        {
            var b64Bytes = Encoding.UTF8.GetBytes(b64Part);
            var expectedSig = HMACSHA256.HashData(_keyBytes, b64Bytes);
            var actualSig = Convert.FromBase64String(sigPart);

            if (!CryptographicOperations.FixedTimeEquals(expectedSig, actualSig))
            {
                throw new InvalidCursorException();
            }

            var jsonBytes = Convert.FromBase64String(b64Part);
            return Task.FromResult(JsonSerializer.Deserialize<CursorPayload>(jsonBytes));
        }
        catch (InvalidCursorException)
        {
            throw;
        }
        catch
        {
            throw new InvalidCursorException();
        }
    }
}
