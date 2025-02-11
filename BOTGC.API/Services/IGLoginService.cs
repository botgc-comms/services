using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.Services
{
    public class IGLoginService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IGLoginService> _logger;
        private readonly AppSettings _settings;
        private readonly CookieContainer _cookieContainer;

        public IGLoginService(HttpClient httpClient, CookieContainer cookieContainer, IOptions<AppSettings> settings, ILogger<IGLoginService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));

            _httpClient.BaseAddress = new Uri(_settings.IG.BaseUrl);

            // Restore all headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _httpClient.DefaultRequestHeaders.Add("Origin", _settings.IG.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _httpClient.DefaultRequestHeaders.Add("Referer", $"{_settings.IG.BaseUrl}/login.php");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-GB,en;q=0.9");
        }

        public async Task<bool> LoginAsync()
        {
            _logger.LogInformation("Starting member login process...");

            var loginUrl = $"{_settings.IG.BaseUrl}/login.php";
            var adminLoginUrl = $"{_settings.IG.BaseUrl}/membership2.php";

            var memberId = _settings.IG.MemberId;
            var memberPin = _settings.IG.MemberPassword;
            var adminPassword = _settings.IG.AdminPassword;

            if (string.IsNullOrEmpty(memberId) || string.IsNullOrEmpty(memberPin) || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogError("Member or Admin credentials are missing in environment variables.");
                return false;
            }

            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("task", "login"),
                new KeyValuePair<string, string>("topmenu", "1"),
                new KeyValuePair<string, string>("memberid", memberId),
                new KeyValuePair<string, string>("pin", memberPin),
                new KeyValuePair<string, string>("cachemid", "1"),
                new KeyValuePair<string, string>("Submit", "Login")
            });

            try
            {
                if (IsSessionActive())
                {
                    _logger.LogInformation("Authenticated session already active.");
                }

                // Perform Member Login
                var memberResponse = await _httpClient.PostAsync(loginUrl, loginData);
                if (!memberResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Member login failed. Status Code: {StatusCode}, Response: {Response}",
                        memberResponse.StatusCode, await memberResponse.Content.ReadAsStringAsync());
                    return false;
                }

                var cookies = _cookieContainer.GetCookies(new Uri(loginUrl));
                foreach (Cookie cookie in cookies)
                {
                    _logger.LogInformation($"Cookie {cookie.Name} set with value {cookie.Value}");
                }


                _logger.LogInformation("Member login successful. Proceeding with admin login...");

                var adminData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("leveltwopassword", adminPassword)
                });

                // Perform Admin Login
                var adminResponse = await _httpClient.PostAsync(adminLoginUrl, adminData);
                if (!adminResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Admin login failed. Status Code: {StatusCode}, Response: {Response}",
                        adminResponse.StatusCode, await adminResponse.Content.ReadAsStringAsync());
                    return false;
                }

                _logger.LogInformation("Admin login successful. Authentication process completed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during login process.");
                return false;
            }
        }
        private bool IsSessionActive()
        {
            var uri = new Uri(_settings.IG.BaseUrl);
            var cookies = _cookieContainer.GetCookies(uri);
            return cookies["PHPSESSID"] != null;
        }
    }
}
