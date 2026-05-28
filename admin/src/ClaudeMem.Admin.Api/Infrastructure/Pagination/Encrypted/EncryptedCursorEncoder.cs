using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;

internal sealed class EncryptedCursorEncoder : ICursorEncoder
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _keyBytes;

    public EncryptedCursorEncoder(IOptions<EncryptedCursorEncoderOptions> options)
    {
        _keyBytes = Convert.FromBase64String(options.Value.EncryptionKeyBase64);
    }

    public string Encode(CursorPayload payload)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_keyBytes, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var combined = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
        nonce.CopyTo(combined, 0);
        ciphertext.CopyTo(combined, NonceSizeBytes);
        tag.CopyTo(combined, NonceSizeBytes + ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public CursorPayload? Decode(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        try
        {
            var combined = Convert.FromBase64String(cursor);

            if (combined.Length < NonceSizeBytes + TagSizeBytes + 1)
            {
                throw new InvalidCursorException();
            }

            var nonce = combined.AsSpan(0, NonceSizeBytes);
            var ciphertext = combined.AsSpan(NonceSizeBytes, combined.Length - NonceSizeBytes - TagSizeBytes);
            var tag = combined.AsSpan(combined.Length - TagSizeBytes, TagSizeBytes);
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(_keyBytes, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return JsonSerializer.Deserialize<CursorPayload>(plaintext);
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
