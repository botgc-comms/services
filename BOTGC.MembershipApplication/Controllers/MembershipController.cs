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
    
    public MembershipController(
        IReferralService referrerService,
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        IMembershipCategoryCache categoryCache)
    {
        _httpClientFactory = httpClientFactory;
        _referrerService = referrerService;
        _settings = settings.Value;
        _categoryCache = categoryCache;
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
        string channel;

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
        else
        {
            channel = !string.IsNullOrWhiteSpace(channelFromQuery) ? channelFromQuery : "Direct";
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
}
