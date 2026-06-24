using System;
using System.IO;
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

    public async Task<string> Tokenize(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var b64 = Convert.ToBase64String(jsonBytes);
        var b64Bytes = Encoding.UTF8.GetBytes(b64);

        await using var b64Stream = new MemoryStream(b64Bytes);
        var sig = await HMACSHA256.HashDataAsync(_keyBytes, b64Stream, cancellationToken);

        return $"{b64}.{Convert.ToBase64String(sig)}";
    }

    public async Task<CursorPayload?> Detokenize(string? token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            return null;
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

            await using var b64Stream = new MemoryStream(b64Bytes);
            var expectedSig = await HMACSHA256.HashDataAsync(_keyBytes, b64Stream, cancellationToken);
            var actualSig = Convert.FromBase64String(sigPart);

            if (!CryptographicOperations.FixedTimeEquals(expectedSig, actualSig))
            {
                throw new InvalidCursorException();
            }

            var jsonBytes = Convert.FromBase64String(b64Part);
            return JsonSerializer.Deserialize<CursorPayload>(jsonBytes);
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
