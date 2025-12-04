using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BOTGC.MemberPortal.Common;

public static class AccessToken
{
    private static string B64Url(byte[] bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] B64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }

    public static string Create(string secret, TimeSpan lifetime, string? nonce = null)
    {
        var payload = new { exp = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds(), n = nonce ?? Guid.NewGuid().ToString("N") };
        var json = JsonSerializer.Serialize(payload);
        var payloadB64 = B64Url(Encoding.UTF8.GetBytes(json));

        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = h.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
        var sigB64 = B64Url(sig);

        return $"{payloadB64}.{sigB64}";
    }

    public static bool Validate(string secret, string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2) return false;
        var payloadB64 = parts[0];
        var sigB64 = parts[1];

        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = B64Url(h.ComputeHash(Encoding.UTF8.GetBytes(payloadB64)));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sig), Encoding.UTF8.GetBytes(sigB64)))
            return false;

        var json = Encoding.UTF8.GetString(B64UrlDecode(payloadB64));
        var doc = JsonDocument.Parse(json);
        var exp = doc.RootElement.GetProperty("exp").GetInt64();
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() < exp;
    }
}
