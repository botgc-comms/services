using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BOTGC.MembershipApplication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.MembershipApplication.Services
{
    public interface IReferralService
    {
        Task<bool> AddParticipantAsync(Models.MembershipApplication application, string clientId);
        Task<ParticipantModel?> GetParticipantByIdAsync(string participantId);
        string GetReferrerId();
    }

    public class GrowSurfService : IReferralService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GrowSurfService> _logger;

        public GrowSurfService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> settings, ILogger<GrowSurfService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> AddParticipantAsync(Models.MembershipApplication application, string clientId)
        {
            var participant = new
            {
                email = application.Email,
                firstName = application.Forename,
                lastName = application.Surname,
                phoneNumber = application.Telephone,
                referredBy = string.IsNullOrEmpty(application.ReferrerId) ? null : application.ReferrerId,
                ipAddress = string.IsNullOrEmpty(clientId) ? null : clientId,
                fingerprint = string.IsNullOrEmpty(application.Fingerprint) ? null : application.Fingerprint,
                metadata = new
                {
                    MembershipCategory = application.MembershipCategory,
                    ApplicationId = application.ApplicationId,
                    Gender = application.Gender,
                    DateOfBirth = application.DateOfBirth!.Value.ToString("yyyy-MM-dd"),
                    Town = application.Town,
                    County = application.County,
                    Postcode = application.Postcode,
                    HasCdhId = application.HasCdhId,
                    CdhId = application.CdhId
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.growsurf.com/v2/campaign/{_settings.GrowSurfSettings.CampaignId}/participant")
            {
                Content = JsonContent.Create(participant)
            };

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.GrowSurfSettings.ApiKey);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully added participant to GrowSurf: {Email}", application.Email);
                    return true;
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to add participant to GrowSurf. Status: {StatusCode}, Response: {ResponseBody}",
                        response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while adding participant to GrowSurf.");
                return false;
            }
        }

        public async Task<ParticipantModel?> GetParticipantByIdAsync(string participantId)
        {
            var url = $"https://api.growsurf.com/v2/campaign/{_settings.GrowSurfSettings.CampaignId}/participant/{participantId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.GrowSurfSettings.ApiKey);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var participant = JsonSerializer.Deserialize<ParticipantModel>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Fetched participant from GrowSurf: {ParticipantId}", participantId);
                    return participant;
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to fetch participant from GrowSurf. Status: {StatusCode}, Response: {ResponseBody}",
                        response.StatusCode, responseBody);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching participant from GrowSurf.");
                return null;
            }
        }

        public string GetReferrerId()
        {
            var query = _httpContextAccessor.HttpContext?.Request?.Query;
            if (query != null && query.ContainsKey("grsf"))
            {
                return query["grsf"].ToString();
            }

            var cookies = _httpContextAccessor.HttpContext?.Request?.Cookies;
            if (cookies != null && cookies.ContainsKey($"{_settings.GrowSurfSettings.CampaignId}.ref"))
            {
                return cookies[$"{_settings.GrowSurfSettings.CampaignId}.ref"];
            }
            return null;
        }
    }

    public class GrowSurfCookie
    {
        [JsonPropertyName("participantReferralCode")]
        public string ParticipantReferralCode { get; set; }
    }
}
