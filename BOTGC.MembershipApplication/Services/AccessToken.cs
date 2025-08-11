using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace BOTGC.MembershipApplication.Services
{
    public static class AccessToken
    {
        private record Payload(string Path, long Exp, string UaHash, string? Ip);

        public static string Issue(string path, DateTimeOffset expUtc, string secret, string userAgent, string? ip)
        {
            var uaHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(userAgent ?? string.Empty)));
            var payload = new Payload(path, expUtc.ToUnixTimeSeconds(), uaHash, ip);
            var json = JsonSerializer.Serialize(payload);
            var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var sig = Sign(data, secret);
            return $"{data}.{sig}";
        }

        public static bool Validate(string token, string path, string secret, string userAgent, string? ip)
        {
            var parts = token.Split('.', 2);
            if (parts.Length != 2) return false;

            var data = parts[0];
            var sig = parts[1];
            var expected = Sign(data, secret);
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sig), Encoding.UTF8.GetBytes(expected))) return false;

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));
            var payload = JsonSerializer.Deserialize<Payload>(json);
            if (payload is null) return false;

            if (!string.Equals(payload.Path, path, StringComparison.Ordinal)) return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now > payload.Exp) return false;

            var uaHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(userAgent ?? string.Empty)));
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(uaHash), Encoding.UTF8.GetBytes(payload.UaHash))) return false;

            if (!string.IsNullOrEmpty(payload.Ip) && !string.Equals(payload.Ip, ip, StringComparison.Ordinal)) return false;

            return true;
        }

        private static string Sign(string base64Payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(base64Payload));
            return Convert.ToBase64String(hash);
        }
    }
}
