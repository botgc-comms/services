using BOTGC.MembershipApplication;
using BOTGC.MembershipApplication.Common;
using BOTGC.MembershipApplication.Interfaces;
using BOTGC.MembershipApplication.Models;
using BOTGC.MembershipApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace BOTGC.MembershipApplication;

public class MembershipController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReferralService _referrerService;
    private readonly IMembershipCategoryCache _categoryCache;
    private readonly AppSettings _settings;
    private readonly IWebHostEnvironment _env;

    public MembershipController(
        IReferralService referrerService,
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        IMembershipCategoryCache categoryCache,
        IWebHostEnvironment env)
    {
        _httpClientFactory = httpClientFactory;
        _referrerService = referrerService;
        _settings = settings.Value;
        _categoryCache = categoryCache;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Apply()
    {
        var newApplication = new MembershipApplication.Models.MembershipApplication();
        var categories = await _categoryCache.GetAll();

        var supressLogo = "false";
        var suppressLogoParamKey = Request.Query.Keys.FirstOrDefault(k => k.ToLower() == "suppresslogo");
        if (!string.IsNullOrEmpty(suppressLogoParamKey))
        {
            supressLogo = string.Equals(suppressLogoParamKey, "true", StringComparison.OrdinalIgnoreCase) ? "true": "false";
        }

        var channelFromQuery = Request.Query["channel"].ToString();

        var referrerId = _referrerService.GetReferrerId();
        string channel = "Direct";

        if (!string.IsNullOrWhiteSpace(referrerId))
        {
            var referrer = await _referrerService.GetParticipantByIdAsync(referrerId);
            if (referrer != null)
            {

                TempData["GrowSurfReferrer"] = referrerId;
                newApplication.ReferrerId = referrerId;
                newApplication.ReferrerName = $"{referrer.FirstName} {referrer.LastName}";
                channel = "Referral";
            }
            else
            {
                channel = !string.IsNullOrWhiteSpace(channelFromQuery) ? channelFromQuery : "Direct";
            }
        }

        if (channel == "Direct")
        {
            if (!string.IsNullOrWhiteSpace(channelFromQuery))
            {
                channel = channelFromQuery;
            }
            else
            {
                var referer = Request.Headers["Referer"].ToString()?.ToLower();

                if (!string.IsNullOrWhiteSpace(referer))
                {
                    if (referer.Contains("facebook.com")) channel = "Social";
                    else if (referer.Contains("instagram.com")) channel = "Social";
                    else if (referer.Contains("botgc.link/application")) channel = "Social";
                    else if (referer.Contains("botgc.link/apply")) channel = "Direct";
                    else if (referer.Contains("botgc.co.uk")) channel = "Website";
                }
            }
        }

        newApplication.Channel = channel;

        // This is the only line you need to pass to JS
        ViewData["MembershipCategories"] = categories;
        ViewData["GrowSurfCampaignId"] = _settings.GrowSurfSettings.CampaignId;
        ViewData["GetAddressApiKey"] = _settings.GetAddressIOSettings.ApiKey;
        ViewData["SupressLogo"] = supressLogo;
        ViewData["Channel"] = channel;

        return View(newApplication);
    }

    [HttpGet("{channel:regex(^fb$|^ig$|^card$|^ds$|^web$)}")]
    public IActionResult ApplyFromChannel(string channel)
    {
        string mappedChannel = channel.ToLower() switch
        {
            "fb" or "ig" => "Social",
            "card" => "Card",
            "ds" => "Screens",
            "web" => "Web",
            _ => "Direct"
        };

        return RedirectToAction("Apply", new { channel = mappedChannel });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(BOTGC.MembershipApplication.Models.MembershipApplication application)
    {
        var supressLogo = "false";
        var suppressLogoParamKey = Request.Query.Keys.FirstOrDefault(k => k.ToLower() == "suppresslogo");
        if (!string.IsNullOrEmpty(suppressLogoParamKey))
        {
            supressLogo = string.Equals(suppressLogoParamKey, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        }

        var categories = await _categoryCache.GetAll();

        ViewData["MembershipCategories"] = categories;
        ViewData["GrowSurfCampaignId"] = _settings.GrowSurfSettings.CampaignId;
        ViewData["GetAddressApiKey"] = _settings.GetAddressIOSettings.ApiKey;
        ViewData["SupressLogo"] = supressLogo;
        ViewData["Channel"] = application.Channel ?? "Direct";

        // Check that the application is valid
        if (!ModelState.IsValid)
            return View(application);

        // Pass the appliccation data to the API
        var client = _httpClientFactory.CreateClient("MembershipApi");
        var response = await client.PostAsJsonAsync("api/members/application", application);

        // Create the Growsurf Participant and redirect the user to the Thankyou page
        if (response.IsSuccessStatusCode)
        {
            var clientIp = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
                           ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            await _referrerService.AddParticipantAsync(application, clientIp);

            TempData["Participant.Email"] = application.Email;
            TempData["Participant.FirstName"] = application.Forename;
            TempData["Participant.LastName"] = application.Surname;
            TempData["Referrer.Id"] = application.ReferrerId;
            TempData["Referrer.Name"] = application.ReferrerName;

            return RedirectToAction("Thanks");
        }

        ModelState.AddModelError("", "An error occurred submitting your application. Please try again.");

        return View(application);
    }

    [HttpGet]
    public IActionResult Thanks()
    {
        ViewData["GrowSurfCampaignId"] = _settings.GrowSurfSettings.CampaignId;

        var participantEmail = TempData["Participant.Email"]?.ToString();

        ViewData["Participant.Email"] = participantEmail;
        ViewData["Participant.FirstName"] = TempData["Participant.FirstName"];
        ViewData["Participant.LastName"] = TempData["Participant.LastName"];
        ViewData["Referrer.Id"] = TempData["Referrer.Id"];
        ViewData["Referrer.Name"] = TempData["Referrer.Name"];

        // Calculate the participant auth hash
        if (participantEmail != null)
        {
            var participantHash = HmacHelper.GenerateSha256Hmac(_settings.GrowSurfSettings.ParticipantAuthSecret, participantEmail);
            ViewData["Participant.Hash"] = participantHash;
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _categoryCache.GetAll();

        var supressLogo = "false";
        var suppressLogoParamKey = Request.Query.Keys.FirstOrDefault(k => k.ToLower() == "suppresslogo");
        if (!string.IsNullOrEmpty(suppressLogoParamKey))
        {
            supressLogo = string.Equals(suppressLogoParamKey, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        }

        // This is the only line you need to pass to JS
        ViewData["MembershipCategories"] = categories;
        ViewData["SupressLogo"] = supressLogo;

        return View();
    }

    [HttpGet("/access/recent-applications")]
    public IActionResult GateRecentApplications()
    {
        var diag = new Dictionary<string, string?>();
        var debug = Request.Query.ContainsKey("debug");

        try
        {
            diag["Environment"] = _env.EnvironmentName;
            var secret = _settings.RecentApplicants.SharedSecret;
            var allowedHost = _settings.RecentApplicants.AllowedReferrerHost;
            var ttl = (_settings.RecentApplicants.TokenTtlMinutes ?? 10) > 0 ? (_settings.RecentApplicants.TokenTtlMinutes ?? 10) : 10;
            var cookieName = string.IsNullOrWhiteSpace(_settings.RecentApplicants.CookieName) ? "ra_tok" : _settings.RecentApplicants.CookieName;

            diag["AllowedReferrerHost"] = allowedHost;
            diag["TTL_Min"] = ttl.ToString();
            diag["CookieName"] = cookieName;

            if (string.IsNullOrWhiteSpace(secret))
                return DebugFail("Missing SharedSecret.", diag, debug);

            var refererRaw = Request.Headers.Referer.ToString();
            diag["RefererRaw"] = refererRaw;

            if (_env.IsDevelopment())
            {
                // Accept missing or invalid referer in development
                diag["RefererRaw"] = refererRaw;
                diag["RefererHost"] = "(skipped in development)";
            }
            else
            {
                if (!Uri.TryCreate(refererRaw, UriKind.Absolute, out var refUri))
                    return DebugFail("Invalid or missing Referer header.", diag, debug);

                diag["RefererHost"] = refUri.Host;

                if (!string.Equals(refUri.Host, allowedHost, StringComparison.OrdinalIgnoreCase))
                    return DebugFail("Referer host does not match AllowedReferrerHost.", diag, debug);
            }

            var targetPath = "/members/recent-applications";
            diag["TargetPath"] = targetPath;

            var ua = Request.Headers.TryGetValue("User-Agent", out var headerVal) ? headerVal.ToString() : string.Empty;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            diag["UserAgent"] = string.IsNullOrEmpty(ua) ? "(empty)" : ua.Substring(0, Math.Min(120, ua.Length));
            diag["ClientIP"] = ip;

            var token = AccessToken.Issue(targetPath, DateTimeOffset.UtcNow.AddMinutes(ttl), secret, ua, ip);
            diag["TokenIssued"] = $"yes (length {token.Length})";

            var cookieDomain = _env.IsDevelopment() ? null : "apply.botgc.co.uk";
            diag["CookieDomain"] = cookieDomain ?? "(host-only)";

            Response.Cookies.Append(cookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = targetPath,
                MaxAge = TimeSpan.FromMinutes(ttl),
                Domain = cookieDomain
            });

            if (debug)
            {
                var html = BuildDebugPage("Gate succeeded", diag, targetPath);
                return Content(html, "text/html");
            }

            return Redirect(targetPath);
        }
        catch (Exception ex)
        {
            diag["Exception"] = ex.GetType().FullName;
            diag["Message"] = ex.Message;
            diag["Stack"] = ex.StackTrace;
            var html = BuildDebugPage("Unhandled exception", diag, null);
            return StatusCode(500, html);
        }
    }

    private IActionResult DebugFail(string reason, IDictionary<string, string?> diag, bool debug)
    {
        if (!debug) return Forbid();
        var html = BuildDebugPage(reason, diag, null);
        return Content(html, "text/html");
    }

    private string BuildDebugPage(string title, IDictionary<string, string?> diag, string? continueUrl)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>Recent Applications – Debug</title>");
        sb.Append("<style>body{font-family:Arial,Helvetica,sans-serif;margin:20px;}h1{margin:0 0 12px;}pre{background:#f6f8fa;padding:12px;border:1px solid #ddd;border-radius:6px;overflow:auto;}a.btn{display:inline-block;margin-top:10px;padding:8px 12px;border-radius:6px;border:1px solid #007bff;text-decoration:none}</style>");
        sb.Append("</head><body>");
        sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(title)).Append("</h1>");
        sb.Append("<pre>");
        foreach (var kv in diag)
        {
            sb.Append(System.Net.WebUtility.HtmlEncode($"{kv.Key}: {kv.Value}")).Append("\n");
        }
        sb.Append("</pre>");
        if (!string.IsNullOrEmpty(continueUrl))
        {
            sb.Append("<a class='btn' href='").Append(continueUrl).Append("'>Continue</a>");
        }
        sb.Append("</body></html>");
        return sb.ToString();
    }


    [HttpGet("/members/recent-applications")]
    public async Task<IActionResult> NewApplications()
    {
        var secret = _settings.RecentApplicants.SharedSecret;
        var cookieName = _settings.RecentApplicants.CookieName ?? "ra_tok";
        var thisPath = "/members/recent-applications";

        if (string.IsNullOrWhiteSpace(secret)) return Forbid();
        if (!Request.Cookies.TryGetValue(cookieName, out var token)) return Forbid();

        var ua = Request.Headers.TryGetValue("User-Agent", out var headerVal) ? headerVal.ToString() : string.Empty;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (!AccessToken.Validate(token, thisPath, secret, ua, ip)) return Forbid();

        // Pass the application data to the API
        var client = _httpClientFactory.CreateClient("MembershipApi");

        var applicants = await client.GetFromJsonAsync<List<MemberModel>>("api/members/waiting") ?? new List<MemberModel>();
        var joinedRaw = await client.GetFromJsonAsync<List<RecentMemberApiModel>>("api/members/new") ?? new List<RecentMemberApiModel>();

        static string NormaliseName(MemberModel m)
        {
            var name = !string.IsNullOrWhiteSpace(m.FullName) ? m.FullName! : $"{m.FirstName} {m.LastName}";
            name = name.Trim().ToUpperInvariant();
            return string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        static string NormalisePostcode(string? pc)
        {
            return (pc ?? string.Empty).ToUpperInvariant().Replace(" ", "");
        }

        var cutoff = DateTime.Now.Date.AddDays(-14);

        var recentJoiners = joinedRaw
           .Where(j => (j.ApplicationDate ?? j.JoinDate) != null && (j.ApplicationDate ?? j.JoinDate) >= cutoff)
           .Select(j => new MemberModel
       {
           MemberNumber = j.MemberNumber,
           Title = null,
           FirstName = j.Forename,
           LastName = j.Surname,
           FullName = $"{j.Forename} {j.Surname}".Trim(),
           Gender = null,
           MembershipCategory = j.MembershipCategory,
           MembershipCategoryGroup = null,
           MembershipStatus = j.MembershipStatus,
           PrimaryCategory = (MembershipPrimaryCategories)j.PrimaryCategory,
           Postcode = j.Postcode,
           Email = j.Email,
           DateOfBirth = j.DateOfBirth,
           JoinDate = j.JoinDate,
           LeaveDate = null,
           ApplicationDate = j.ApplicationDate
       })
       .ToList();

        var newApplications = applicants
            .Where(m => m.ApplicationDate.HasValue && m.ApplicationDate.Value.Date >= cutoff)
            .GroupBy(m => new { Name = NormaliseName(m), Postcode = NormalisePostcode(m.Postcode) })
            .Select(g => g.OrderByDescending(m => m.ApplicationDate ?? DateTime.MinValue).First())
            .ToList();

        var joinedKey = recentJoiners
            .ToDictionary(m => (Name: NormaliseName(m), Postcode: NormalisePostcode(m.Postcode)));

        var applicantsOnly = newApplications
            .Where(a => !joinedKey.ContainsKey((NormaliseName(a), NormalisePostcode(a.Postcode))))
            .ToList();

        var combined = recentJoiners
            .Concat(applicantsOnly)
            .OrderByDescending(m => m.JoinDate ?? m.ApplicationDate ?? DateTime.MinValue)
            .ToList();

        var supressLogo = "false";
        var suppressLogoParamKey = Request.Query.Keys.FirstOrDefault(k => k.ToLower() == "suppresslogo");
        if (!string.IsNullOrEmpty(suppressLogoParamKey))
        {
            supressLogo = string.Equals(suppressLogoParamKey, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        }

        ViewData["SupressLogo"] = supressLogo;

        return View(combined);
    }
}
