using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services;
using Services.Services;

namespace BOTGC.API.Services
{

    public class IGSessionService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly IGLoginService _igLoginService;
        private readonly ILogger<IGSessionService> _logger;
        private readonly TimeSpan _sessionRefreshInterval;
        private readonly AppSettings _settings;
        private readonly object _loginLock = new object();

        private DateTime _lastLoginTime;
        private TaskCompletionSource<bool> _loginCompletionSource = new TaskCompletionSource<bool>();

        public IGSessionService(IGLoginService igLoginService, HttpClient httpClient, IOptions<AppSettings> settings, ILogger<IGSessionService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _igLoginService = igLoginService ?? throw new ArgumentNullException(nameof(igLoginService));

            _sessionRefreshInterval = TimeSpan.FromMinutes(_settings.IG.LoginEveryNMinutes);
            _lastLoginTime = DateTime.MinValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow - _lastLoginTime >= _sessionRefreshInterval)
                {
                    await PerformLoginAsync();
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        public async Task WaitForLoginAsync()
        {
            await _loginCompletionSource.Task;
        }

        public async Task<HtmlDocument?> PostPageContent(string pageUrl, Dictionary<string, string> data)
        {
            var content = new FormUrlEncodedContent(data);

            var response = await _httpClient.PostAsync(pageUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch {pageUrl}. Status: {StatusCode}", pageUrl, response.StatusCode);
                return null;
            }

            var pageContent = await response.Content.ReadAsStringAsync();

            var doc = SafeParseResponse(pageContent);

            var titleElement = doc.DocumentNode.SelectSingleNode("//title");

            if (titleElement != null && Regex.IsMatch(titleElement.InnerText, "Login Required"))
            {
                _logger.LogWarning("Login required detected. Ensuring a valid session...");

                await EnsureLoggedInAsync(); // Thread-safe login handling

                response = await _httpClient.PostAsync(pageUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Second attempt to fetch {pageUrl} failed. Status: {StatusCode}", pageUrl, response.StatusCode);
                    return null;
                }

                pageContent = await response.Content.ReadAsStringAsync();
                doc = SafeParseResponse(pageContent);

                titleElement = doc.DocumentNode.SelectSingleNode("//title");
                if (titleElement != null && Regex.IsMatch(titleElement.InnerText, "Login Required"))
                {
                    _logger.LogError("Login attempt failed. Unable to fetch {pageUrl}", pageUrl);
                    return null;
                }
            }

            _logger.LogInformation("Successfully fetched {pageUrl}.", pageUrl);
            return doc;
        }

        public async Task<HtmlDocument?> GetPageContent(string pageUrl)
        {
            var response = await _httpClient.GetAsync(pageUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch {pageUrl}. Status: {StatusCode}", pageUrl, response.StatusCode);
                return null;
            }

            var pageContent = await response.Content.ReadAsStringAsync();
            var doc = SafeParseResponse(pageContent);

            var titleElement = doc.DocumentNode.SelectSingleNode("//title");

            if (titleElement != null && Regex.IsMatch(titleElement.InnerText, "Login Required"))
            {
                _logger.LogWarning("Login required detected. Ensuring a valid session...");

                await EnsureLoggedInAsync(); // Thread-safe login handling

                response = await _httpClient.GetAsync(pageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Second attempt to fetch {pageUrl} failed. Status: {StatusCode}", pageUrl, response.StatusCode);
                    return null;
                }

                pageContent = await response.Content.ReadAsStringAsync();
                doc = SafeParseResponse(pageContent);

                titleElement = doc.DocumentNode.SelectSingleNode("//title");
                if (titleElement != null && Regex.IsMatch(titleElement.InnerText, "Login Required"))
                {
                    _logger.LogError("Login attempt failed. Unable to fetch {pageUrl}", pageUrl);
                    return null;
                }
            }

            _logger.LogInformation("Successfully fetched {pageUrl}.", pageUrl);
            return doc;
        }

        private async Task EnsureLoggedInAsync()
        {
            // Avoid unnecessary login attempts
            if (_loginCompletionSource.Task.IsCompleted && _loginCompletionSource.Task.Result)
            {
                _logger.LogInformation("Session already active. No need to log in.");
                return;
            }

            TaskCompletionSource<bool> tcs;

            lock (_loginLock)
            {
                if (_loginCompletionSource.Task.IsCompleted)
                {
                    _loginCompletionSource = new TaskCompletionSource<bool>();
                }
                tcs = _loginCompletionSource;
            }

            if (!tcs.Task.IsCompleted)
            {
                await PerformLoginAsync();
            }

            await tcs.Task;
        }

        private async Task PerformLoginAsync()
        {
            lock (_loginLock)
            {
                if (_loginCompletionSource.Task.IsCompleted)
                {
                    _loginCompletionSource = new TaskCompletionSource<bool>();
                }
            }

            try
            {
                _logger.LogInformation("Performing login...");
                var loginSuccessful = await _igLoginService.LoginAsync();

                if (loginSuccessful)
                {
                    _lastLoginTime = DateTime.UtcNow;
                    _logger.LogInformation("Session refreshed successfully.");
                    _loginCompletionSource.SetResult(true);
                }
                else
                {
                    _logger.LogError("Login failed.");
                    _loginCompletionSource.SetResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging in.");
                _loginCompletionSource.SetResult(false);
            }
        }

        private HtmlDocument SafeParseResponse(string raw)
        {
            // Check if the response is JSON (starts with { or [)
            if (raw.TrimStart().StartsWith("{") || raw.TrimStart().StartsWith("["))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(raw);
                    if (jsonDoc.RootElement.TryGetProperty("actions", out var actions))
                    {
                        foreach (var action in actions.EnumerateArray())
                        {
                            if (action.TryGetProperty("html", out var htmlElement))
                            {
                                var encodedHtml = htmlElement.GetString();
                                if (!string.IsNullOrEmpty(encodedHtml))
                                {
                                    return LoadHtmlDocument(encodedHtml);
                                }
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError("Failed to parse JSON response: {Error}", ex.Message);
                }
            }

            return LoadHtmlDocument(raw);
        }

        private HtmlDocument LoadHtmlDocument(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            return doc;
        }
    }
}
