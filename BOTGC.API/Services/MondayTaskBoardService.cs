using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BOTGC.API.Services
{
    public class MondayTaskBoardService : ITaskBoardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MondayTaskBoardService> _logger;
        private readonly string _apiKey;

        public MondayTaskBoardService(
            HttpClient httpClient,
            IOptions<AppSettings> options,
            ILogger<MondayTaskBoardService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = options?.Value?.Monday?.APIKey ?? throw new ArgumentNullException(nameof(options), "Monday API key is missing in app settings.");

            _httpClient.BaseAddress = new Uri("https://api.monday.com/v2/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<string?> GetUserIdByEmailAsync(string email)
        {
            _logger.LogDebug("Fetching Monday user ID for email: {Email}", email);

            var query = @"{ users { id name email } }";
            var payload = new { query };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var users = json?["data"]?["users"]?.AsArray();

            var user = users?
                .FirstOrDefault(u => u?["email"]?.ToString()?.ToLowerInvariant() == email.ToLowerInvariant());

            if (user != null)
            {
                var userId = user?["id"]?.ToString();
                _logger.LogDebug("Found user ID {UserId} for email {Email}.", userId, email);
                return userId;
            }

            _logger.LogWarning("No user found in Monday for email: {Email}.", email);
            return null;
        }

        private async Task<string?> GetBoardIdByNameAsync(string boardName)
        {
            _logger.LogDebug("Fetching Monday board ID for board name: {BoardName}", boardName);

            var query = @"{ boards(limit: 40) { id name } }";
            var payload = new { query };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var board = json?["data"]?["boards"]?.AsArray()?
                .FirstOrDefault(b => b?["name"]?.ToString() == boardName);

            if (board != null)
            {
                var boardId = board?["id"]?.ToString();
                _logger.LogDebug("Found board ID {BoardId} for board name {BoardName}.", boardId, boardName);
                return boardId;
            }

            _logger.LogWarning("No board found in Monday for board name: {BoardName}.", boardName);
            return null;
        }

        private string MapMembershipCategoryToListItem(string membershipCategory)
        {
            string categoryName = membershipCategory switch
            {
                "7Day" => "7 Day",
                "6Day" => "6 Day", 
                "5Day" => "5 Day", 
                "Intermediate" => "Intermediate",
                "Student" => "Student",
                "Junior" => "Junior",
                "Flexi" => "Flexi",
                "Clubhouse" => "Clubhouse",
                "Family" => "Family",
                "Social" => "Social",
                _ => throw new ArgumentException($"Unsupported membership category or invalid age: {membershipCategory}")
            };

            return categoryName;
        }

        public async Task<string> CreateMemberApplicationAsync(NewMemberApplicationDto dto)
        {
            _logger.LogInformation("Creating Monday task for membership application: {Forename} {Surname} (ApplicationId: {ApplicationId})",
                dto.Forename, dto.Surname, dto.ApplicationId);

            var boardId = await GetBoardIdByNameAsync("Membership Applications")
                         ?? throw new InvalidOperationException("Board not found.");

            var itemName = $"Online Application - {dto.Forename} {dto.Surname}";

            var userId = await GetUserIdByEmailAsync("clubmanager@botgc.co.uk");

            var columnValues = new Dictionary<string, object?>
            {
                ["status"] = new { label = "Working on it" },
                ["color_mkq7h26c"] = new { label = MapMembershipCategoryToListItem(dto.MembershipCategory) },
                ["date4"] = new { date = dto.ApplicationDate.ToString("yyyy-MM-dd") },
                ["text_mkq639pw"] = $"{dto.Forename} {dto.Surname}",
                ["text_mkq6xbq4"] = dto.Telephone,
                ["text_mkq6vbtw"] = dto.Email,
                ["link_mkq67bhc"] = string.IsNullOrWhiteSpace(dto.MemberId) ? null : new
                {
                    url = $"https://www.botgc.co.uk/member.php?memberid={dto.MemberId}",
                    text = dto.MemberId
                },
                ["date_mkq7q88n"] = new { date = dto.ApplicationDate.AddDays(3).ToString("yyyy-MM-dd") },
                ["date_mkq7j3ma"] = new { date = dto.ApplicationDate.AddDays(5).ToString("yyyy-MM-dd") },
                ["text_mkq7w63d"] = dto.ApplicationId
            };

            if (!string.IsNullOrWhiteSpace(userId))
            {
                columnValues["person"] = new
                {
                    personsAndTeams = new[]
                    {
                        new { id = int.Parse(userId), kind = "person" }
                    }
                };
            }

            var mutation = @"
            mutation ($boardId: ID!, $itemName: String!, $columnValues: JSON!) {
                create_item(board_id: $boardId, item_name: $itemName, column_values: $columnValues) {
                    id
                }
            }";

            var variables = new
            {
                boardId,
                itemName,
                columnValues = JsonSerializer.Serialize(columnValues)
            };

            var payload = new { query = mutation, variables };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var responseJson = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var itemId = responseJson?["data"]?["create_item"]?["id"]?.ToString()!;

            _logger.LogInformation("Successfully created Monday task with ID {ItemId} for application {ApplicationId}.", itemId, dto.ApplicationId);

            return itemId;
        }

        public async Task<string?> FindExistingApplicationItemIdAsync(string applicationId)
        {
            _logger.LogInformation("Checking for existing Monday item with ApplicationId: {ApplicationId}", applicationId);

            var boardId = await GetBoardIdByNameAsync("Membership Applications")
                         ?? throw new InvalidOperationException("Board not found.");

            var query = @"
                query GetBoardItems($boardId: [ID!]) {
                  boards(ids: $boardId) {
                    items_page(limit: 500) {
                      items {
                        id
                        column_values(ids: [""text_mkq7w63d""]) {
                          id
                          value
                        }
                      }
                    }
                  }
                }";

            var variables = new { boardId = new[] { boardId } };  

            var payload = new { query, variables };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var responseJson = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var items = responseJson?["data"]?["boards"]?[0]?["items_page"]?["items"]?.AsArray();

            foreach (var item in items!)
            {
                var columnValues = item?["column_values"]?.AsArray();
                var appIdColumn = columnValues?
                    .FirstOrDefault(c => c?["id"]?.ToString() == "text_mkq7w63d");

                if (appIdColumn != null)
                {
                    var rawValue = appIdColumn["value"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(rawValue))
                    {
                        var unwrapped = JsonSerializer.Deserialize<string>(rawValue);
                        if (unwrapped == applicationId)
                        {
                            var foundId = item?["id"]?.ToString();
                            _logger.LogInformation("Found existing Monday item {ItemId} for ApplicationId {ApplicationId}.", foundId, applicationId);
                            return foundId;
                        }
                    }
                }
            }

            _logger.LogInformation("No existing Monday item found for ApplicationId {ApplicationId}.", applicationId);
            return null;
        }


        public async Task<string> AttachFile(string itemId, byte[] fileBytes, string fileName)
        {
            _logger.LogInformation("Attaching file {FileName} to Monday item {ItemId}.", fileName, itemId);

            string query = "mutation ($file: File!, $itemId: ID!) " +
                           "{ add_file_to_column(item_id: $itemId, column_id: \"file_mkq7bhzz\", file: $file) { id } }";
            string variablesJson = $"{{ \"itemId\": \"{itemId}\" }}";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(query), "query");
            form.Add(new StringContent(variablesJson), "variables");
            form.Add(new StringContent("{\"file\": [\"variables.file\"]}"), "map");

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            form.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync("file", form);
            var responseBody = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully attached file to Monday item {ItemId}. Response: {ResponseBody}", itemId, responseBody);

            return responseBody;
        }
    }
}
