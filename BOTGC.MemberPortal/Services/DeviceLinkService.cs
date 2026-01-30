using System.Security.Claims;
using System.Security.Cryptography;
using BOTGC.MemberPortal.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace BOTGC.MemberPortal.Services;

public sealed class DeviceLinkService : IDeviceLinkService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(2);

    public DeviceLinkService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Create(ClaimsPrincipal user, string returnUrl)
    {
        var token = CreateToken();

        var identity = user.Identity as ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated)
        {
            throw new InvalidOperationException("User must be authenticated.");
        }

        var claims = user.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, identity.AuthenticationType));

        var payload = new Payload(principal, NormaliseReturnUrl(returnUrl));

        _cache.Set(CacheKey(token), payload, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttl
        });

        return token;
    }

    public (ClaimsPrincipal Principal, string ReturnUrl)? Redeem(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var key = CacheKey(token);

        if (!_cache.TryGetValue(key, out Payload? payload) || payload == null)
        {
            return null;
        }

        _cache.Remove(key);

        return (payload.Principal, payload.ReturnUrl);
    }

    private static string CacheKey(string token) => $"device-link:{token}";

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        var s = Convert.ToBase64String(data);
        s = s.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return s;
    }

    private static string NormaliseReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (!returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return "/" + returnUrl;
        }

        return returnUrl;
    }

    private sealed record Payload(ClaimsPrincipal Principal, string ReturnUrl);
}