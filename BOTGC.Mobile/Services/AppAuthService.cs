using System.Net.Http;
using System.Net.Http.Json;
using System.Web;
using BOTGC.Mobile.Interfaces;
using Microsoft.Maui.Storage;

namespace BOTGC.Mobile.Services;

public sealed class AppAuthService : IAppAuthService
{
    private const string ApiClientName = "BotgcApi";
    
    private const string StorageAccessToken = "botgc.access_token";
    private const string StorageAccessTokenExpiresUtc = "botgc.access_token_expires_utc";
    private const string StorageRefreshToken = "botgc.refresh_token";
    private const string StorageRefreshTokenExpiresUtc = "botgc.refresh_token_expires_utc";

    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _tokenGate = new SemaphoreSlim(1, 1);

    public AppAuthService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(string Code, DateTimeOffset ExpiresUtc)?> IssueWebSsoCodeAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await GetValidAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var client = _httpClientFactory.CreateClient(ApiClientName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/app/web-sso");
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        req.Content = JsonContent.Create(new WebSsoRequest());

        var res = await client.SendAsync(req, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await res.Content.ReadFromJsonAsync<WebSsoResponse>(cancellationToken: cancellationToken);
        if (body == null || string.IsNullOrWhiteSpace(body.Code))
        {
            return null;
        }

        return (body.Code.Trim(), body.ExpiresUtc);
    }

    public async Task<IAppAuthService.AuthCheckResult> CheckAndRefreshIfPossibleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetValidAccessTokenAsync(cancellationToken);
            return new IAppAuthService.AuthCheckResult(!string.IsNullOrWhiteSpace(accessToken), null);
        }
        catch (Exception ex)
        {
            await ClearTokensAsync();
            return new IAppAuthService.AuthCheckResult(false, ex.Message);
        }
    }

    public async Task<IAppAuthService.AppAuthRedeemResponse> RedeemAsync(string code, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ApiClientName);

        var res = await client.PostAsJsonAsync("api/auth/app/redeem", new AppAuthRedeemRequest { Code = code }, cancellationToken);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<IAppAuthService.AppAuthRedeemResponse>(cancellationToken: cancellationToken);
        if (body == null || string.IsNullOrWhiteSpace(body.SessionId))
        {
            throw new InvalidOperationException("Redeem response invalid.");
        }

        return body;
    }

    public async Task<IAppAuthService.AuthTokenResponse> IssueTokenAsync(string sessionId, DateOnly dateOfBirth, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ApiClientName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/app/token");

        req.Content = JsonContent.Create(new AppAuthIssueTokenRequest
        {
            SessionId = sessionId,
            DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
        });

        var res = await client.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<IAppAuthService.AuthTokenResponse>(cancellationToken: cancellationToken);
        if (body == null || string.IsNullOrWhiteSpace(body.AccessToken) || string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            throw new InvalidOperationException("Token response invalid.");
        }

        return body;
    }

    public async Task SaveTokensAsync(IAppAuthService.AuthTokenResponse tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (string.IsNullOrWhiteSpace(tokens.AccessToken) || string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            throw new InvalidOperationException("Token response invalid.");
        }

        await SecureStorage.SetAsync(StorageAccessToken, tokens.AccessToken);
        await SecureStorage.SetAsync(StorageAccessTokenExpiresUtc, tokens.AccessTokenExpiresUtc.ToUnixTimeSeconds().ToString());

        await SecureStorage.SetAsync(StorageRefreshToken, tokens.RefreshToken);
        await SecureStorage.SetAsync(StorageRefreshTokenExpiresUtc, tokens.RefreshTokenExpiresUtc.ToUnixTimeSeconds().ToString());
    }

    public Task<string?> GetRefreshTokenAsync()
    {
        return SecureStorage.GetAsync(StorageRefreshToken);
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            var existingAccess = await SecureStorage.GetAsync(StorageAccessToken);
            var existingAccessExpires = await SecureStorage.GetAsync(StorageAccessTokenExpiresUtc);

            if (!string.IsNullOrWhiteSpace(existingAccess) && TryParseUnixSeconds(existingAccessExpires, out var accessExpiresUtc))
            {
                if (DateTimeOffset.UtcNow + ExpirySkew < accessExpiresUtc)
                {
                    return existingAccess;
                }
            }

            var refresh = await SecureStorage.GetAsync(StorageRefreshToken);
            var refreshExpires = await SecureStorage.GetAsync(StorageRefreshTokenExpiresUtc);

            if (string.IsNullOrWhiteSpace(refresh))
            {
                await ClearTokensAsync();
                return null;
            }

            if (TryParseUnixSeconds(refreshExpires, out var refreshExpiresUtc))
            {
                if (DateTimeOffset.UtcNow + ExpirySkew >= refreshExpiresUtc)
                {
                    await ClearTokensAsync();
                    return null;
                }
            }

            var tokens = await RefreshAsync(refresh, cancellationToken);
            await SaveTokensAsync(tokens);

            return tokens.AccessToken;
        }
        catch
        {
            await ClearTokensAsync();
            return null;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    public Task ClearTokensAsync()
    {
        SecureStorage.Remove(StorageAccessToken);
        SecureStorage.Remove(StorageAccessTokenExpiresUtc);
        SecureStorage.Remove(StorageRefreshToken);
        SecureStorage.Remove(StorageRefreshTokenExpiresUtc);
        return Task.CompletedTask;
    }

    public string? ExtractCodeFromRedeemUrl(string raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var qs = HttpUtility.ParseQueryString(uri.Query);

        var code = qs.Get("code");
        if (!string.IsNullOrWhiteSpace(code))
        {
            return code.Trim();
        }

        var q = qs.Get("q");
        if (!string.IsNullOrWhiteSpace(q))
        {
            return q.Trim();
        }

        return null;
    }

    private async Task<IAppAuthService.AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(ApiClientName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/app/refresh");

        req.Content = JsonContent.Create(new AuthRefreshRequest { RefreshToken = refreshToken });

        var res = await client.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<IAppAuthService.AuthTokenResponse>(cancellationToken: cancellationToken);
        if (body == null || string.IsNullOrWhiteSpace(body.AccessToken) || string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            throw new InvalidOperationException("Refresh response invalid.");
        }

        return body;
    }

    private static bool TryParseUnixSeconds(string? value, out DateTimeOffset utc)
    {
        utc = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!long.TryParse(value, out var seconds))
        {
            return false;
        }

        utc = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }

    private static string GetApiKey()
    {
        return Preferences.Get("botgc.api_key", string.Empty);
    }

    private static string GetClientId()
    {
        return Preferences.Get("botgc.client_id", "botgc-maui");
    }

    private sealed class AppAuthRedeemRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class AppAuthIssueTokenRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
    }

    private sealed class AuthRefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class WebSsoRequest
    {
    }

    private sealed class WebSsoResponse
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }
}
