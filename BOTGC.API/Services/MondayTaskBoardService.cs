using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
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
                _logger.LogDebug("Creating Monday task for membership application {BoardId} for board name {BoardName}.", boardId, boardName);
                return boardId;
            }

            _logger.LogWarning("No board found in Monday for board name: {BoardName}.", boardName);
            return null;
        }

        private string MapMembershipCategoryToListItem(string membershipCategory, DateTime dateOfBirth)
        {
            var age = CalculateAge(dateOfBirth);

            string categoryName = membershipCategory switch
            {
                "7Day" when age >= 22 && age <= 29 => "Intermediate",
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

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        public async Task<string> CreateMemberApplicationAsync(NewMemberApplicationResultDto applicationResult)
        {
            var memberId = applicationResult.MemberId;
            var dto = applicationResult.Application;

            var forename = NameCasingHelper.CapitaliseForename(dto.Forename);
            var surname = NameCasingHelper.CapitaliseSurname(dto.Surname);
            var name = $"{forename} {surname}";

            _logger.LogInformation("Creating Monday task for membership application: {Name} (ApplicationId: {ApplicationId})",
                name, dto.ApplicationId);
                        
            var boardId = await GetBoardIdByNameAsync("Membership Applications")
                         ?? throw new InvalidOperationException("Board not found.");

            var itemName = $"Online Application - {name}";

            var userId = await GetUserIdByEmailAsync("clubmanager@botgc.co.uk");

            var linkValue = new
            {
                url = $"https://www.botgc.co.uk/member.php?memberid={memberId}",
                text = $"{memberId}"
            };

            var columnValuesObject = new Dictionary<string, object?>
            {
                ["status"] = new { label = "Working on it" },
                ["color_mkq7h26c"] = new { label = MapMembershipCategoryToListItem(dto.MembershipCategory, dto.DateOfBirth) },
                ["date4"] = new { date = dto.ApplicationDate.ToString("yyyy-MM-dd") },
                ["text_mkq639pw"] = name,
                ["text_mkq6xbq4"] = dto.Telephone,
                ["text_mkq6vbtw"] = dto.Email,
                ["link_mkq67bhc"] = memberId == null ? null : new
                {
                    url = $"https://www.botgc.co.uk/member.php?memberid={memberId}",
                    text = memberId.ToString()
                },
                ["date_mkq7q88n"] = new { date = dto.ApplicationDate.AddDays(3).ToString("yyyy-MM-dd") },
                ["date_mkq7j3ma"] = new { date = dto.ApplicationDate.AddDays(5).ToString("yyyy-MM-dd") },
                ["text_mkq7w63d"] = dto.ApplicationId,
                ["boolean_mkqnq3va"] = new { @checked = dto.ArrangeFinance == true },  
                ["text_mkqn9qqm"] = dto.ReferrerId
            };

            if (!string.IsNullOrWhiteSpace(userId))
            {
                columnValuesObject["person"] = new
                {
                    personsAndTeams = new[]
                    {
                        new { id = int.Parse(userId), kind = "person" }
                    }
                };
            }

            // Fix incorrect property name "checkedValue" => "checked"
            var json = JsonSerializer.Serialize(columnValuesObject, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Construct GraphQL variables
            var variables = new
            {
                boardId,
                itemName,
                columnValues = json // already stringified
            };

            var payload = new
            {
                query = @"
                    mutation ($boardId: ID!, $itemName: String!, $columnValues: JSON!) {
                        create_item(board_id: $boardId, item_name: $itemName, column_values: $columnValues) {
                            id
                        }
                    }",
                variables
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(string.Empty, content);

            response.EnsureSuccessStatusCode();

            var responseJson = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var itemId = responseJson?["data"]?["create_item"]?["id"]?.ToString()!;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                _logger.LogError("Failed to create Monday item. Response content: {Response}", responseJson);
                throw new InvalidOperationException("Failed to retrieve item ID from Monday response.");
            }

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

        public async Task<List<MembershipCategoryGroupDto>> GetMembershipCategories()
        {
            _logger.LogInformation("Fetching membership categories from Monday.com");

            var boardId = await GetBoardIdByNameAsync("Membership Categories")
                         ?? throw new InvalidOperationException("Board not found.");

            var query = @"
                query GetBoardItems($boardId: [ID!]) {
                    boards(ids: $boardId) {
                      columns {
                        id
                        title
                        settings_str
                      }
                      items_page(limit: 100) {
                        items {
                          name
                          column_values {
                            id
                            text
                            value
                          }
                        }
                      }
                    }
                }";

            var payload = new
            {
                query,
                variables = new { boardId = new[] { boardId } }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var board = json?["data"]?["boards"]?[0];
            var items = board?["items_page"]?["items"]?.AsArray();
            var columns = board?["columns"]?.AsArray();

            if (items == null || columns == null)
            {
                _logger.LogWarning("No items or column metadata found on board ID {BoardId}", boardId);
                return new List<MembershipCategoryGroupDto>();
            }

            var labelOrder = new Dictionary<string, int>();

            var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var col in columns.Where(c => c?["title"] is not null && c?["id"] is not null))
            {
                var title = col["title"]!.ToString();
                var id = col["id"]!.ToString();

                if (!columnMap.ContainsKey(title))
                {
                    columnMap[title] = id;
                }
                else
                {
                    _logger.LogWarning("Duplicate column title detected: {Title}, keeping first instance with ID {Id}", title, columnMap[title]);
                }
            }

            var statusColumn = columns
                .FirstOrDefault(c => c?["id"]?.ToString() == columnMap["Type"]);

            if (statusColumn is not null)
            {
                var settingsStr = statusColumn["settings_str"]?.ToString();
                if (!string.IsNullOrEmpty(settingsStr))
                {
                    using var doc = JsonDocument.Parse(settingsStr);
                    if (doc.RootElement.TryGetProperty("labels", out var labelsElement))
                    {
                        int order = 0;
                        foreach (var label in labelsElement.EnumerateObject())
                        {
                            var labelName = label.Value.GetString();
                            if (!string.IsNullOrEmpty(labelName))
                            {
                                labelOrder[labelName] = order++;
                            }
                        }
                    }
                }
            }

            var groupings = new Dictionary<int, (string name, List<MembershipCategoryDto> categories)>();

            foreach (var item in items)
            {
                var name = item?["name"]?.ToString() ?? "";
                var columnValues = item?["column_values"]?.AsArray();

                string GetColumnValue(string id)
                {
                    return columnValues?
                        .FirstOrDefault(c => c?["id"]?.ToString() == id)?["value"]?.ToString() ?? "";
                }

                string GetColumnText(string id)
                {
                    return columnValues?
                        .FirstOrDefault(c => c?["id"]?.ToString() == id)?["text"]?.ToString() ?? "";
                }

                bool IsChecked(string jsonValue)
                {
                    return !string.IsNullOrEmpty(jsonValue) && jsonValue.Contains("\"checked\":true");
                }

                string GetText(string jsonValue)
                {
                    if (string.IsNullOrEmpty(jsonValue))
                        return "";

                    var text = JsonDocument.Parse(jsonValue).RootElement.GetProperty("text").GetString() ?? "";

                    // Fix common mojibake from misencoded apostrophes
                    return text
                        .Replace("â", "’")
                        .Replace("â", "–")
                        .Replace("â", "—")
                        .Replace("â¦", "…")
                        .Replace("â", "“")
                        .Replace("â", "”")
                        .Replace("â˜", "‘");
                }

                // Extract group name and order from "type" column (color_mkqynm1f)
                string groupName = columnValues?
                    .FirstOrDefault(c => c?["id"]?.ToString() == columnMap["Type"])?
                    ["text"]?.ToString() ?? "Uncategorised";

                int groupIndex = labelOrder.TryGetValue(groupName, out var idx) ? idx : 999;

                var dto = new MembershipCategoryDto
                {
                    Name = name,
                    Title = GetColumnText(columnMap["Description"]),
                    Description = GetText(GetColumnValue(columnMap["Information"])),
                    Price = columnMap.TryGetValue("Price", out var priceColId) ? GetColumnText(priceColId) : "",
                    FinanceAvailable = IsChecked(GetColumnValue(columnMap["Finance Eligible"])),
                    ReferrerEligable = IsChecked(GetColumnValue(columnMap["Referrer Eligible"])),
                    IsOnWaitingList = IsChecked(GetColumnValue(columnMap["Waiting List"])),
                    Display = IsChecked(GetColumnValue(columnMap["Advertise"]))
                };

                if (!groupings.ContainsKey(groupIndex))
                {
                    groupings[groupIndex] = (groupName, new List<MembershipCategoryDto>());
                }

                groupings[groupIndex].categories.Add(dto);
            }

            var retVal = groupings
                .OrderBy(g => g.Key)
                .Select(g => new MembershipCategoryGroupDto
                {
                    Name = g.Value.name,
                    Order = g.Key,
                    Categories = g.Value.categories.ToList()
                })
                .ToList();

            return retVal;
        }
    }
}
