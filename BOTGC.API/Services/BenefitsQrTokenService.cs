using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BOTGC.API.Services;

public sealed class BenefitsQrTokenService : IBenefitsQrTokenService
{
    private readonly byte[] _key;

    public BenefitsQrTokenService(IOptions<AppSettings> options)
    {
        var settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

        var keyBase64 = settings.EposBenefits?.QrEncryptionKey;
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException("EposBenefits.QrEncryptionKey is not configured.");
        }

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
        {
            throw new InvalidOperationException("EposBenefits.QrEncryptionKey must be a 32-byte key encoded as Base64.");
        }
    }

    public string CreateToken(BenefitsQrPayloadDto payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var plaintext = Encoding.UTF8.GetBytes(json);

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(_key))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var result = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
        result[0] = 1;

        Buffer.BlockCopy(nonce, 0, result, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, 1 + nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public BenefitsQrPayloadDto? TryDecrypt(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        byte[] data;

        try
        {
            data = Convert.FromBase64String(token);
        }
        catch
        {
            return null;
        }

        if (data.Length < 1 + 12 + 16)
        {
            return null;
        }

        var version = data[0];
        if (version != 1)
        {
            return null;
        }

        var nonce = new byte[12];
        var tag = new byte[16];
        var ciphertext = new byte[data.Length - 1 - nonce.Length - tag.Length];

        Buffer.BlockCopy(data, 1, nonce, 0, nonce.Length);
        Buffer.BlockCopy(data, 1 + nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(data, 1 + nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];

        try
        {
            using (var aes = new AesGcm(_key))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }
        }
        catch
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(plaintext);
            return JsonSerializer.Deserialize<BenefitsQrPayloadDto>(json);
        }
        catch
        {
            return null;
        }
    }
}
