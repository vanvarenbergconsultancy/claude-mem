using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;

internal sealed class HmacCursorEncoder : ICursorEncoder
{
    private readonly byte[] _keyBytes;

    public HmacCursorEncoder(IOptions<HmacCursorEncoderOptions> options)
    {
        _keyBytes = Encoding.UTF8.GetBytes(options.Value.SigningKey);
    }

    public string Encode(CursorPayload payload)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var b64 = Convert.ToBase64String(jsonBytes);
        var b64Bytes = Encoding.UTF8.GetBytes(b64);
        var sig = HMACSHA256.HashData(_keyBytes, b64Bytes);
        
        return $"{b64}.{Convert.ToBase64String(sig)}";
    }

    public CursorPayload? Decode(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        var dotIndex = cursor.LastIndexOf('.');
        if (dotIndex < 0)
        {
            throw new InvalidCursorException();
        }

        var b64Part = cursor[..dotIndex];
        var sigPart = cursor[(dotIndex + 1)..];

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
