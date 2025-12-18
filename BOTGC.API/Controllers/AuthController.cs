using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRCoder;

namespace BOTGC.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private const string CacheKeyPrefixCode = "AppAuth-Code-";
        private const string CacheKeyPrefixSession = "AppAuth-Session-";
        private const string CacheKeyPrefixRefresh = "AppAuth-Refresh-";

        private readonly ILogger<AuthController> _logger;
        private readonly IMediator _mediator;
        private readonly ICacheService _cacheService;
        private readonly AppSettings _settings;

        public AuthController(
            ILogger<AuthController> logger,
            IMediator mediator,
            ICacheService cacheService,
            IOptions<AppSettings> settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Generates a QR code image that can be scanned by the iPhone app to start an app sign-in session.
        /// </summary>
        /// <param name="q">
        /// Base64-encoded JSON object (URL-encoded in the query string).
        /// Expected shape: <c>{"id":"646-83642","name":"...","user_level":"...","membertype":"..."}</c>.
        /// The <c>id</c> field is used to resolve the member.
        /// </param>
        /// <remarks>
        /// The QR code does not contain member PII; PII is held server-side for the short session TTL.
        /// </remarks>
        /// <response code="200">Returns a PNG image containing the QR code.</response>
        /// <response code="400">The payload is missing/invalid, or does not contain a usable member identifier.</response>
        /// <response code="403">The identified member is not eligible for app access.</response>
        /// <response code="404">No member could be found for the identifier provided.</response>
        [HttpGet("app/qr")]
        [AllowAnonymous]
        [Produces("image/png")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppAuthQr([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(Problem(title: "Missing payload.", detail: "Query string parameter 'q' is required."));
            }

            AppAuthProbeProperties props;
            try
            {
                var json = Encoding.UTF8.GetString(DecodeBase64Lenient(q));
                props = JsonSerializer.Deserialize<AppAuthProbeProperties>(json, JsonOptions()) ?? new AppAuthProbeProperties();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid base64/JSON in app auth probe.");
                return BadRequest(Problem(title: "Invalid payload.", detail: "Parameter 'q' must be valid base64-encoded JSON."));
            }

            if (string.IsNullOrWhiteSpace(props.Id))
            {
                return BadRequest(Problem(title: "Missing member identifier.", detail: "The decoded JSON must contain 'id'."));
            }

            var (left, right) = ParseTwoPartNumericId(props.Id);
            if (left == null && right == null)
            {
                return BadRequest(Problem(title: "Invalid member identifier.", detail: "Expected 'id' in the form '646-83642' or a single numeric value."));
            }

            var query = new GetCurrentMembersQuery();
            var currentMembers = await _mediator.Send(query, HttpContext.RequestAborted);

            if (currentMembers == null || currentMembers.Count == 0)
            {
                return NotFound(Problem(title: "Not found.", detail: "Member not found."));
            }

            var member = ResolveMember(currentMembers, left, right);
            if (member == null)
            {
                return NotFound(Problem(title: "Not found.", detail: "Member not found."));
            }

            if (!ProbeMatchesMember(props, member))
            {
                return NotFound(Problem(title: "Not found.", detail: "Member not found."));
            }

            if (!IsEligibleForApp(member))
            {
                return NotFound(Problem(title: "Not found.", detail: "Member not found."));
            }

            if (member.MemberNumber == null || member.PlayerId == null || member.DateOfBirth == null)
            {
                return NotFound(Problem(title: "Not found.", detail: "Member not found."));
            }

            var payload = BuildPayload(member);

            var code = Guid.NewGuid().ToString("N");
            var cacheKey = CacheKeyPrefixCode + code;

            var codeRecord = new AppAuthCodeRecord
            {
                Code = code,
                CreatedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_settings.Auth.App.QrCodeTtlMinutes),
                Payload = payload,
                Redeemed = false
            };

            await _cacheService.SetAsync(cacheKey, codeRecord, TimeSpan.FromMinutes(_settings.Auth.App.QrCodeTtlMinutes));

            var qrText = BuildQrText(code);

            byte[] png;
            using (var generator = new QRCodeGenerator())
            using (var qrData = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            {
                var qr = new PngByteQRCode(qrData);
                png = qr.GetGraphic(pixelsPerModule: 8);
            }

            return File(png, "image/png");
        }

        private static bool ProbeMatchesMember(AppAuthProbeProperties props, MemberDto member)
        {
            if (!string.IsNullOrWhiteSpace(props.Name))
            {
                var probeName = NormaliseName(props.Name);
                var memberName = NormaliseName(member.FullName ?? $"{member.FirstName} {member.LastName}");
                if (!string.Equals(probeName, memberName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(props.MemberType))
            {
                var probeType = NormaliseMemberType(props.MemberType);
                var memberType = NormaliseMemberType(member.MembershipCategory ?? string.Empty);
                if (!string.Equals(probeType, memberType, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormaliseName(string value)
        {
            var s = value.Trim();
            s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            return s.ToUpperInvariant();
        }

        private static string NormaliseMemberType(string value)
        {
            var s = value.Trim();
            s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            return s.ToUpperInvariant();
        }


        [HttpPost("app/redeem")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthRedeemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Redeem([FromBody] AppAuthRedeemRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return NotFound(Problem(title: "Code not found.", detail: "The code is missing."));
            }

            try
            {
                var result = await RedeemInternal(request.Code, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch
            {
                return NotFound(Problem(title: "Code not found.", detail: "The code is unknown, expired, or already redeemed."));
            }
        }

        [HttpGet("app/redeem")]
        [AllowAnonymous]
        [Produces("text/html")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RedeemGet([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return NotFound(Problem(title: "Code not found.", detail: "The code is missing."));
            }

            try
            {
                var result = await RedeemInternal(code, HttpContext.RequestAborted);

                var html =
                    "<!doctype html><html><head><meta charset=\"utf-8\" />" +
                    "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />" +
                    "<title>BOTGC App Sign-in</title></head><body style=\"font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif; padding:24px;\">" +
                    "<h1>QR code scanned</h1>" +
                    "<p>Session created.</p>" +
                    $"<p><strong>sessionId</strong><br /><code style=\"word-break:break-all;\">{result.SessionId}</code></p>" +
                    $"<p><strong>expiresUtc</strong><br /><code>{result.ExpiresUtc:O}</code></p>" +
                    "</body></html>";

                return Content(html, "text/html", Encoding.UTF8);
            }
            catch
            {
                return NotFound(Problem(title: "Code not found.", detail: "The code is unknown, expired, or already redeemed."));
            }
        }

        /// <summary>
        /// Exchanges a redeemed app sign-in session and date of birth for an app auth token.
        /// </summary>
        /// <param name="request">The token request containing the session identifier and date of birth.</param>
        /// <remarks>
        /// This endpoint is intended to be called by the mobile app. It relies on the API key middleware plus an
        /// allow-listed client identifier (X-CLIENT-ID) as an additional speed bump.
        /// </remarks>
        /// <response code="200">Tokens were issued.</response>
        /// <response code="400">The session is invalid, expired, or the date of birth does not match.</response>
        /// <response code="401">The client identifier was missing or not allowed.</response>
        [HttpPost("app/token")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> IssueToken([FromHeader(Name = "X-CLIENT-ID")] string clientId, [FromBody] AppAuthVerifyDobRequest request)
        {
            clientId = clientId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Unauthorized(Problem(title: "Unauthorised.", detail: "Missing X-CLIENT-ID header."));
            }

            if (_settings.Auth.App.AllowedClientIds == null
                || _settings.Auth.App.AllowedClientIds.Length == 0
                || !_settings.Auth.App.AllowedClientIds.Contains(clientId, StringComparer.OrdinalIgnoreCase))
            {
                return Unauthorized(Problem(title: "Unauthorised.", detail: "Client is not allowed."));
            }

            if (request == null || string.IsNullOrWhiteSpace(request.SessionId) || request.DateOfBirth == null)
            {
                return BadRequest(Problem(title: "Invalid request.", detail: "sessionId and dateOfBirth are required."));
            }

            var sessionId = request.SessionId.Trim();
            var sessionKey = CacheKeyPrefixSession + sessionId;

            var session = await _cacheService.GetAsync<AppAuthSessionRecord>(sessionKey);
            if (session == null || session.ExpiresUtc <= DateTimeOffset.UtcNow)
            {
                return BadRequest(Problem(title: "Session expired.", detail: "The sign-in session is invalid or has expired."));
            }

            if (session.FailedDobAttempts >= _settings.Auth.App.MaxDobAttempts)
            {
                await _cacheService.RemoveAsync(sessionKey);
                return BadRequest(Problem(title: "Too many attempts.", detail: "Too many invalid attempts. Please restart sign-in."));
            }

            var expected = session.Payload.DateOfBirth.Date;
            var provided = request.DateOfBirth.Value.Date;

            if (expected != provided)
            {
                session.FailedDobAttempts++;
                await _cacheService.SetAsync(sessionKey, session, TimeSpan.FromMinutes(_settings.Auth.App.SessionTtlMinutes));
                return BadRequest(Problem(title: "Date of birth did not match.", detail: "The details provided did not match our records."));
            }

            await _cacheService.RemoveAsync(sessionKey);

            var nowUtc = DateTimeOffset.UtcNow;

            var accessToken = CreateJwtAccessToken(session.Payload);
            var refreshToken = CreateRefreshToken();
            var refreshKey = CacheKeyPrefixRefresh + refreshToken;

            var refreshRecord = new AppAuthRefreshRecord
            {
                RefreshToken = refreshToken,
                IssuedUtc = nowUtc,
                ExpiresUtc = nowUtc.AddDays(_settings.Auth.App.RefreshTokenTtlDays),
                MembershipNumber = session.Payload.MembershipNumber
            };

            await _cacheService.SetAsync(refreshKey, refreshRecord, TimeSpan.FromDays(_settings.Auth.App.RefreshTokenTtlDays));

            return Ok(new AppAuthTokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresUtc = nowUtc.AddMinutes(_settings.Auth.App.AccessTokenTtlMinutes),
                RefreshToken = refreshToken,
                RefreshTokenExpiresUtc = refreshRecord.ExpiresUtc
            });
        }


        private async Task<AppAuthRedeemResponse> RedeemInternal(string code, CancellationToken cancellationToken)
        {
            code = code.Trim();
            var codeKey = CacheKeyPrefixCode + code;

            var codeRecord = await _cacheService.GetAsync<AppAuthCodeRecord>(codeKey);
            if (codeRecord == null || codeRecord.Redeemed || codeRecord.ExpiresUtc <= DateTimeOffset.UtcNow)
            {
                throw new KeyNotFoundException("Code not found.");
            }

            codeRecord.Redeemed = true;
            await _cacheService.SetAsync(codeKey, codeRecord, TimeSpan.FromMinutes(_settings.Auth.App.QrCodeTtlMinutes));

            var sessionId = Guid.NewGuid().ToString("N");
            var sessionKey = CacheKeyPrefixSession + sessionId;

            var session = new AppAuthSessionRecord
            {
                SessionId = sessionId,
                CreatedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_settings.Auth.App.SessionTtlMinutes),
                Payload = codeRecord.Payload,
                FailedDobAttempts = 0
            };

            await _cacheService.SetAsync(sessionKey, session, TimeSpan.FromMinutes(_settings.Auth.App.SessionTtlMinutes));

            return new AppAuthRedeemResponse
            {
                SessionId = sessionId,
                ExpiresUtc = session.ExpiresUtc
            };
        }


        private static (int? Left, int? Right) ParseTwoPartNumericId(string id)
        {
            id = id.Trim();

            var parts = id.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 2
                && int.TryParse(parts[0], out var left)
                && int.TryParse(parts[1], out var right))
            {
                return (left, right);
            }

            if (parts.Length == 1 && int.TryParse(parts[0], out var single))
            {
                return (single, null);
            }

            return (null, null);
        }

        private static MemberDto? ResolveMember(IReadOnlyCollection<MemberDto> members, int? left, int? right)
        {
            if (left.HasValue && right.HasValue)
            {
                var byMemberNumber = members.FirstOrDefault(m => m.MemberNumber == right.Value);
                if (byMemberNumber != null)
                {
                    return byMemberNumber;
                }

                var byPlayerId = members.FirstOrDefault(m => m.PlayerId == right.Value);
                if (byPlayerId != null)
                {
                    return byPlayerId;
                }

                var swappedByMemberNumber = members.FirstOrDefault(m => m.MemberNumber == left.Value);
                if (swappedByMemberNumber != null)
                {
                    return swappedByMemberNumber;
                }

                var swappedByPlayerId = members.FirstOrDefault(m => m.PlayerId == left.Value);
                if (swappedByPlayerId != null)
                {
                    return swappedByPlayerId;
                }

                return null;
            }

            if (left.HasValue)
            {
                var byMemberNumber = members.FirstOrDefault(m => m.MemberNumber == left.Value);
                if (byMemberNumber != null)
                {
                    return byMemberNumber;
                }

                return members.FirstOrDefault(m => m.PlayerId == left.Value);
            }

            return null;
        }

        private bool IsEligibleForApp(MemberDto member)
        {
            if (member.IsActive != true)
            {
                return false;
            }

            if (_settings.Auth.App.AllowedMembershipCategories != null && _settings.Auth.App.AllowedMembershipCategories.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(member.MembershipCategory))
                {
                    return false;
                }

                return _settings.Auth.App.AllowedMembershipCategories.Contains(member.MembershipCategory, StringComparer.OrdinalIgnoreCase);
            }

            if (_settings.Auth.App.AllowedMembershipCategoryGroups != null && _settings.Auth.App.AllowedMembershipCategoryGroups.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(member.MembershipCategoryGroup))
                {
                    return false;
                }

                return _settings.Auth.App.AllowedMembershipCategoryGroups.Contains(member.MembershipCategoryGroup, StringComparer.OrdinalIgnoreCase);
            }

            return true;
        }

        private static AppAuthPayload BuildPayload(MemberDto member)
        {
            return new AppAuthPayload
            {
                FirstName = member.FirstName ?? string.Empty,
                Surname = member.LastName ?? string.Empty,
                DateOfBirth = member.DateOfBirth!.Value,
                MembershipId = member.PlayerId!.Value,
                MembershipNumber = member.MemberNumber!.Value,
                CurrentCategory = member.MembershipCategory ?? string.Empty,
                EmailAddress = member.Email ?? string.Empty,
                Parents = Array.Empty<AppAuthParentPayload>()
            };
        }

        private string BuildQrText(string code)
        {
            if (!string.IsNullOrWhiteSpace(_settings.Auth.App.QrBaseUrl))
            {
                return $"{_settings.Auth.App.QrBaseUrl.TrimEnd('/')}/api/auth/app/redeem?code={Uri.EscapeDataString(code)}";
            }

            var host = $"{Request.Scheme}://{Request.Host.Value}";
            return $"{host}/api/auth/app/redeem?code={Uri.EscapeDataString(code)}";
        }

        private string CreateJwtAccessToken(AppAuthPayload payload)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_settings.Auth.App.JwtSigningKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_settings.Auth.App.AccessTokenTtlMinutes);

            var claims = new List<Claim>
            {
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, payload.MembershipNumber.ToString()),
                new Claim("memberNumber", payload.MembershipNumber.ToString()),
                new Claim("memberId", payload.MembershipId.ToString()),
                new Claim("firstName", payload.FirstName),
                new Claim("surname", payload.Surname),
                new Claim("category", payload.CurrentCategory),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _settings.Auth.App.JwtIssuer,
                Audience = _settings.Auth.App.JwtAudience,
                Subject = new ClaimsIdentity(claims),
                NotBefore = now,
                Expires = expires,
                SigningCredentials = credentials,
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);

            return handler.WriteToken(token);
        }

        private static string CreateRefreshToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static byte[] DecodeBase64Lenient(string value)
        {
            value = value.Trim();
            value = value.Replace('-', '+').Replace('_', '/');

            var mod = value.Length % 4;
            if (mod == 2) value += "==";
            else if (mod == 3) value += "=";
            else if (mod != 0) throw new FormatException("Invalid base64 length.");

            return Convert.FromBase64String(value);
        }
    }

    public sealed class AppAuthProbeProperties
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("user_level")]
        public string? User_Level { get; set; }

        [JsonPropertyName("membertype")]
        public string? MemberType { get; set; }
    }

    public sealed class AppAuthPayload
    {
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int MembershipId { get; set; }
        public int MembershipNumber { get; set; }
        public string CurrentCategory { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public IReadOnlyCollection<AppAuthParentPayload> Parents { get; set; } = Array.Empty<AppAuthParentPayload>();
    }

    public sealed class AppAuthParentPayload
    {
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public int MembershipNumber { get; set; }
    }

    public sealed class AppAuthCodeRecord
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public bool Redeemed { get; set; }
        public AppAuthPayload Payload { get; set; } = new AppAuthPayload();
    }

    public sealed class AppAuthSessionRecord
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public int FailedDobAttempts { get; set; }
        public AppAuthPayload Payload { get; set; } = new AppAuthPayload();
    }

    public sealed class AppAuthRefreshRecord
    {
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset IssuedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public int MembershipNumber { get; set; }
    }

    public sealed class AppAuthRedeemRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public sealed class AppAuthRedeemResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }

    public sealed class AppAuthVerifyDobRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
    }

    public sealed class AppAuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresUtc { get; set; }
    }
}
