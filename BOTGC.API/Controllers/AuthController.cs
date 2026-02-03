using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace BOTGC.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private const string CacheKeyPrefixCode = "AppAuth:Code-";
        private const string CacheKeyPrefixSession = "AppAuth:Session-";
        private const string CacheKeyPrefixRefresh = "AppAuth:Refresh-";
        private const string CacheKeyPrefixWebSso = "AppAuth:WebSSO-";
        private const string CacheKeyPrefixParentLink = "AppAuth:ParentLink-";

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

        [HttpPost("app/web-sso")]
        [Authorize]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthWebSsoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> IssueWebSso([FromHeader(Name = "X-CLIENT-ID")] string clientId)
        {
            Response.Headers.CacheControl = "no-store";

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

            int memberNumber;
            int memberId;

            string firstName;
            string surname;
            string category;
            string jti;

            string sub;
            try
            {
                memberNumber = GetRequiredClaimInt("memberNumber");
                memberId = GetRequiredClaimInt("memberId");

                firstName = GetOptionalClaim("firstName") ?? string.Empty;
                surname = GetOptionalClaim("surname") ?? string.Empty;
                category = GetOptionalClaim("category") ?? string.Empty;

                jti = GetOptionalClaim(JwtRegisteredClaimNames.Jti) ?? string.Empty;
                sub = GetOptionalClaim(JwtRegisteredClaimNames.Sub) ?? string.Empty;
            }
            catch
            {
                return Unauthorized(Problem(title: "Unauthorised.", detail: "Access token is missing required claims."));
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var ttlSeconds = 60;
            var expiresUtc = nowUtc.AddSeconds(ttlSeconds);

            var code = Guid.NewGuid().ToString("N");
            var key = CacheKeyPrefixWebSso + code;

            var record = new AppAuthWebSsoRecord
            {
                Code = code,
                IssuedUtc = nowUtc,
                ExpiresUtc = expiresUtc,
                ClientId = clientId,
                JwtJti = jti,
                JwtSub = sub,
                MembershipNumber = memberNumber,
                MembershipId = memberId,
                FirstName = firstName,
                Surname = surname,
                Category = category
            };

            await _cacheService.SetAsync(key, record, TimeSpan.FromSeconds(ttlSeconds));

            return Ok(new AppAuthWebSsoResponse
            {
                Code = code,
                ExpiresUtc = expiresUtc
            });
        }

        /// <summary>
        /// Creates an app sign-in code from the probe payload, and either:
        /// - returns a PNG QR code (mode=qr), or
        /// - redirects to a Universal Link (mode=activate).
        /// </summary>
        /// <param name="q">
        /// Base64-encoded JSON object (URL-encoded in the query string).
        /// Expected shape: <c>{"id":"646-83642","name":"...","user_level":"...","membertype":"..."}</c>.
        /// </param>
        /// <param name="mode">qr | activate</param>
        [HttpGet("app/code")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppAuthCode([FromQuery] string q, [FromQuery] string? mode)
        {
            Response.Headers.CacheControl = "no-store";

            mode = (mode ?? "qr").Trim().ToLowerInvariant();
            if (mode != "qr" && mode != "activate")
            {
                return BadRequest(Problem(title: "Invalid mode.", detail: "Query string parameter 'mode' must be 'qr' or 'activate'."));
            }

            var codeRecord = await CreateCodeRecordFromProbeAsync(q, HttpContext.RequestAborted);
            var code = codeRecord.Code;

            if (mode == "activate")
            {
                var openUrl = BuildUniversalOpenUrl(code);
                return Redirect(openUrl);
            }

            var qrText = BuildRedeemUrl(code);

            byte[] png;
            using (var generator = new QRCodeGenerator())
            using (var qrData = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            {
                var qr = new PngByteQRCode(qrData);
                png = qr.GetGraphic(pixelsPerModule: 8);
            }

            return File(png, "image/png");
        }

        [HttpPost("app/redeem")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthRedeemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Redeem([FromBody] AppAuthRedeemRequest request)
        {
            Response.Headers.CacheControl = "no-store";

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
            Response.Headers.CacheControl = "no-store";

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

        [HttpPost("app/refresh")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromHeader(Name = "X-CLIENT-ID")] string clientId, [FromBody] AppAuthRefreshRequest request)
        {
            Response.Headers.CacheControl = "no-store";

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

            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(Problem(title: "Invalid request.", detail: "refreshToken is required."));
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var oldToken = request.RefreshToken.Trim();
            var oldKey = CacheKeyPrefixRefresh + oldToken;

            var record = await _cacheService.GetAsync<AppAuthRefreshRecord>(oldKey);
            if (record == null || record.ExpiresUtc <= nowUtc)
            {
                return Unauthorized(Problem(title: "Unauthorised.", detail: "Refresh token is invalid or expired."));
            }

            var accessToken = CreateJwtAccessToken(record.Payload);

            var newRefreshToken = CreateRefreshToken();
            var newKey = CacheKeyPrefixRefresh + newRefreshToken;

            var newRecord = new AppAuthRefreshRecord
            {
                RefreshToken = newRefreshToken,
                IssuedUtc = nowUtc,
                ExpiresUtc = nowUtc.AddDays(_settings.Auth.App.RefreshTokenTtlDays),
                MembershipNumber = record.MembershipNumber,
                Payload = record.Payload
            };

            await _cacheService.RemoveAsync(oldKey);
            await _cacheService.SetAsync(newKey, newRecord, TimeSpan.FromDays(_settings.Auth.App.RefreshTokenTtlDays));

            return Ok(new AppAuthTokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresUtc = nowUtc.AddMinutes(_settings.Auth.App.AccessTokenTtlMinutes),
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresUtc = newRecord.ExpiresUtc
            });
        }

        [HttpPost("app/parent-link")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthWebSsoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IssueParentLink([FromBody] AppAuthIssueParentLinkRequest request)
        {
            Response.Headers.CacheControl = "no-store";

            if (request == null || request.ChildMembershipId <= 0 || request.ChildMembershipNumber <= 0)
            {
                return BadRequest(Problem(title: "Invalid request.", detail: "childMembershipId and childMembershipNumber are required."));
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var ttlSeconds = 120;
            var expiresUtc = nowUtc.AddSeconds(ttlSeconds);

            var code = Guid.NewGuid().ToString("N");
            var key = CacheKeyPrefixParentLink + code;

            var record = new AppAuthParentLinkRecord
            {
                Code = code,
                IssuedUtc = nowUtc,
                ExpiresUtc = expiresUtc,
                ChildMembershipNumber = request.ChildMembershipNumber,
                ChildMembershipId = request.ChildMembershipId,
                ChildFirstName = request.ChildFirstName?.Trim() ?? string.Empty,
                ChildSurname = request.ChildSurname?.Trim() ?? string.Empty,
                ChildCategory = request.ChildCategory?.Trim() ?? string.Empty
            };

            await _cacheService.SetAsync(key, record, TimeSpan.FromSeconds(ttlSeconds));

            return Ok(new AppAuthWebSsoResponse
            {
                Code = code,
                ExpiresUtc = expiresUtc
            });
        }


        [HttpPost("app/token")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AppAuthTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> IssueToken([FromHeader(Name = "X-CLIENT-ID")] string clientId, [FromBody] AppAuthVerifyDobRequest request)
        {

            Response.Headers.CacheControl = "no-store";

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
                MembershipNumber = session.Payload.MembershipNumber,
                Payload = session.Payload
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

        [HttpPut("app/linkpage")]
        public async Task<IActionResult> UpdateAppLinkPage()
        {
            var updateAppLinkPageCommand = new UpdateAppLinkPageCommand();
            var results = await _mediator.Send(updateAppLinkPageCommand, HttpContext.RequestAborted);
            return Ok(results);
        }

        #region Helper Methods

        private async Task<AppAuthCodeRecord> CreateCodeRecordFromProbeAsync(string q, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new InvalidOperationException("Missing payload.");
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
                throw new ArgumentException("Invalid payload.");
            }

            if (string.IsNullOrWhiteSpace(props.Id))
            {
                throw new ArgumentException("Missing member identifier.");
            }

            var (left, right) = ParseTwoPartNumericId(props.Id);
            if (left == null && right == null)
            {
                throw new ArgumentException("Invalid member identifier.");
            }

            var currentMembers = await _mediator.Send(new GetCurrentMembersQuery(), cancellationToken);
            if (currentMembers == null || currentMembers.Count == 0)
            {
                throw new KeyNotFoundException("Member not found.");
            }

            var member = ResolveMember(currentMembers, left, right);
            if (member == null || !ProbeMatchesMember(props, member) || !IsEligibleForApp(member))
            {
                throw new KeyNotFoundException("Member not found.");
            }

            if (member.MemberNumber == null || member.PlayerId == null || member.DateOfBirth == null)
            {
                throw new KeyNotFoundException("Member not found.");
            }

            var getMemberQuery = new GetMemberQuery { MemberNumber = member.MemberNumber };
            var memberDetails = await _mediator.Send(getMemberQuery, cancellationToken);

            var payload = await BuildPayloadAsync(memberDetails, cancellationToken);

            var code = Guid.NewGuid().ToString("N");
            var cacheKey = CacheKeyPrefixCode + code;

            var nowUtc = DateTimeOffset.UtcNow;

            var codeRecord = new AppAuthCodeRecord
            {
                Code = code,
                CreatedUtc = nowUtc,
                ExpiresUtc = nowUtc.AddMinutes(_settings.Auth.App.QrCodeTtlMinutes),
                Payload = payload,
                Redeemed = false
            };

            await _cacheService.SetAsync(cacheKey, codeRecord, TimeSpan.FromMinutes(_settings.Auth.App.QrCodeTtlMinutes));

            return codeRecord;
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

        private async Task<AppAuthRedeemResponse> RedeemInternal(string code, CancellationToken cancellationToken)
        {
            code = code.Trim();
            var nowUtc = DateTimeOffset.UtcNow;

            var codeKey = CacheKeyPrefixCode + code;

            var codeRecord = await _cacheService.GetAsync<AppAuthCodeRecord>(codeKey);
            if (codeRecord == null || codeRecord.ExpiresUtc <= nowUtc)
            {
                throw new KeyNotFoundException("Code not found.");
            }

            if (codeRecord.Redeemed)
            {
                if (string.IsNullOrWhiteSpace(codeRecord.SessionId))
                {
                    throw new KeyNotFoundException("Code not found.");
                }

                var existingSessionKey = CacheKeyPrefixSession + codeRecord.SessionId;
                var existingSession = await _cacheService.GetAsync<AppAuthSessionRecord>(existingSessionKey);

                if (existingSession == null || existingSession.ExpiresUtc <= nowUtc)
                {
                    throw new KeyNotFoundException("Code not found.");
                }

                return new AppAuthRedeemResponse
                {
                    SessionId = existingSession.SessionId,
                    ExpiresUtc = existingSession.ExpiresUtc
                };
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var sessionKey = CacheKeyPrefixSession + sessionId;

            var session = new AppAuthSessionRecord
            {
                SessionId = sessionId,
                CreatedUtc = nowUtc,
                ExpiresUtc = nowUtc.AddMinutes(_settings.Auth.App.SessionTtlMinutes),
                Payload = codeRecord.Payload,
                FailedDobAttempts = 0
            };

            await _cacheService.SetAsync(sessionKey, session, TimeSpan.FromMinutes(_settings.Auth.App.SessionTtlMinutes));

            codeRecord.Redeemed = true;
            codeRecord.SessionId = sessionId;

            var remainingCodeTtl = codeRecord.ExpiresUtc - nowUtc;
            if (remainingCodeTtl < TimeSpan.Zero)
            {
                remainingCodeTtl = TimeSpan.Zero;
            }

            await _cacheService.SetAsync(codeKey, codeRecord, remainingCodeTtl);

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

        private async Task<AppAuthPayload> BuildPayloadAsync(MemberDetailsDto member, CancellationToken cancellationToken)
        {
            var childLinks = await ExtractChildLinksAsync(member, cancellationToken);

            return new AppAuthPayload
            {
                FirstName = member.Forename ?? string.Empty,
                Surname = member.Surname ?? string.Empty,
                DateOfBirth = member.DateOfBirth!.Value,
                MembershipId = member.ID,
                MembershipNumber = member.MemberNumber,
                CurrentCategory = member.MembershipCategory ?? string.Empty,
                EmailAddress = member.Email ?? string.Empty,
                ChildLinks = childLinks
            };
        }

        private async Task<IReadOnlyCollection<AppAuthChildLink>> ExtractChildLinksAsync(MemberDetailsDto member, CancellationToken cancellationToken)
        {
            if (member.FurtherInformation == null)
            {
                return Array.Empty<AppAuthChildLink>();
            }

            if (!member.FurtherInformation.TryGetValue("Parent-Child Relationship", out var values) || values == null)
            {
                return Array.Empty<AppAuthChildLink>();
            }

            var ids = new List<int>();
            foreach (var v in values)
            {
                if (int.TryParse((v ?? string.Empty).Trim(), out var id) && id > 0)
                {
                    ids.Add(id);
                }
            }

            ids = ids.Distinct().Take(3).ToList();
            if (ids.Count == 0)
            {
                return Array.Empty<AppAuthChildLink>();
            }

            var tasks = ids.Select(async id =>
            {
                var childDetailsQuery = new GetMemberQuery { MemberNumber = id };
                var childDetails = await _mediator.Send(childDetailsQuery, cancellationToken);

                if (childDetails == null)
                {
                    return null;
                }

                return new AppAuthChildLink
                {
                    MembershipId = id,
                    Name = childDetails.Forename ?? string.Empty,
                    Category = childDetails.MembershipCategory ?? string.Empty
                };
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            return results.Where(x => x != null).Cast<AppAuthChildLink>().ToArray();
        }

        private string BuildRedeemUrl(string code)
        {
            if (!string.IsNullOrWhiteSpace(_settings.Auth.App.QrBaseUrl))
            {
                return $"{_settings.Auth.App.QrBaseUrl.TrimEnd('/')}/api/auth/app/redeem?code={Uri.EscapeDataString(code)}";
            }

            var host = $"{Request.Scheme}://{Request.Host.Value}";
            return $"{host}/api/auth/app/redeem?code={Uri.EscapeDataString(code)}";
        }

        private string BuildUniversalOpenUrl(string code)
        {
            var baseUrl = (_settings.Auth.App.UniversalLinkBaseUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var host = $"{Request.Scheme}://{Request.Host.Value}";
                baseUrl = $"{host}/appauth/open";
            }

            var sep = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{baseUrl.TrimEnd('/')}{sep}code={Uri.EscapeDataString(code)}";
        }

        private string CreateJwtAccessToken(AppAuthPayload payload)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_settings.Auth.App.JwtSigningKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_settings.Auth.App.AccessTokenTtlMinutes);

            var childLinksJson = JsonSerializer.Serialize(payload.ChildLinks ?? Array.Empty<AppAuthChildLink>());

            var claims = new List<Claim>
            {
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, payload.MembershipNumber.ToString()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim("memberNumber", payload.MembershipNumber.ToString()),
                new Claim("memberId", payload.MembershipId.ToString()),
                new Claim("firstName", payload.FirstName),
                new Claim("surname", payload.Surname),
                new Claim("category", payload.CurrentCategory),
                new Claim("child_links", childLinksJson)
            };

            if (payload.ChildLinks != null && payload.ChildLinks.Count > 0)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Parent"));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, "Child"));
            }

            if (payload.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

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

        private string? GetOptionalClaim(string type)
        {
            return User?.Claims?.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.Ordinal))?.Value;
        }

        private int GetRequiredClaimInt(string type)
        {
            var value = GetOptionalClaim(type);
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var parsed))
            {
                throw new SecurityTokenException($"Missing or invalid claim '{type}'.");
            }

            return parsed;
        }

        #endregion
    }

    public sealed class AppAuthWebSsoRecord
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset IssuedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string JwtJti { get; set; } = string.Empty;
        public string JwtSub { get; set; } = string.Empty;

        public int MembershipNumber { get; set; }
        public int MembershipId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public sealed class AppAuthIssueParentLinkRequest
    {
        public int ChildMembershipId { get; set; }
        public int ChildMembershipNumber { get; set; }
        public string ChildFirstName { get; set; } = string.Empty;
        public string ChildSurname { get; set; } = string.Empty;
        public string ChildCategory { get; set; } = string.Empty;
    }


    public sealed class AppAuthWebSsoResponse
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
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

    public sealed class AppAuthChildLink
    {
        public int MembershipId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
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
        public IReadOnlyCollection<AppAuthChildLink> ChildLinks { get; set; } = Array.Empty<AppAuthChildLink>();
        public bool IsAdmin { get; set; } = false;
    }

    public sealed class AppAuthCodeRecord
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public bool Redeemed { get; set; }
        public string? SessionId { get; set; }
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
        public AppAuthPayload Payload { get; set; } = new AppAuthPayload();
    }

    public sealed class AppAuthRedeemRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public sealed class AppAuthRefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
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
