namespace BOTGC.Mobile.Interfaces;

public interface IAppAuthService
{
    public sealed record AuthCheckResult(bool IsAuthenticated, string? Error);

    public sealed class AppAuthRedeemResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }

    public sealed class AuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresUtc { get; set; }
    }

    Task<(string Code, DateTimeOffset ExpiresUtc)?> IssueWebSsoCodeAsync(CancellationToken cancellationToken = default);

    Task<AuthCheckResult> CheckAndRefreshIfPossibleAsync(CancellationToken cancellationToken = default);

    Task<AppAuthRedeemResponse> RedeemAsync(string code, CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> IssueTokenAsync(string sessionId, DateOnly dateOfBirth, CancellationToken cancellationToken = default);

    Task SaveTokensAsync(AuthTokenResponse tokens);

    Task<string?> GetRefreshTokenAsync();

    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);

    Task ClearTokensAsync();

    string? ExtractCodeFromRedeemUrl(string raw);
}
