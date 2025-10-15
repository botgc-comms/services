using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using Humanizer;
using Microsoft.Extensions.Options;
using Polly;
using System.Globalization;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private const string __BOARD_ID_CACHE_KEY = "Board_Id_{boardName}";

        public MondayTaskBoardService(
            HttpClient httpClient,
            IOptions<AppSettings> options,
            ILogger<MondayTaskBoardService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = options?.Value?.Monday?.APIKey ?? throw new ArgumentNullException(nameof(options), "Monday API key is missing in app settings.");
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            _httpClient.BaseAddress = new Uri("https://api.monday.com/v2/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static readonly Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> _mondayApiPolicy =
            Polly.Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode == 429 || (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

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
            ICacheService? cacheService = null;

            var boardId = string.Empty;
            var cacheKey = __BOARD_ID_CACHE_KEY.Replace("{boardName}", boardName);

            if (!string.IsNullOrEmpty(cacheKey))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var cachedResults = await cacheService!.GetAsync<string>(cacheKey).ConfigureAwait(false);
                if (cachedResults != null && cachedResults.Any())
                {
                    _logger.LogInformation("Retrieving board id from cache for board name {BoardName}...", boardName);
                    boardId = cachedResults;
                }
            }

            if (String.IsNullOrEmpty(boardId))
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
                    boardId = board?["id"]?.ToString();
                    _logger.LogDebug("Creating Monday task for membership application {BoardId} for board name {BoardName}.", boardId, boardName);
                }
                else
                {
                    _logger.LogWarning("No board found in Monday for board name: {BoardName}.", boardName);
                }
            }

            if (!string.IsNullOrEmpty(boardId) && cacheService != null)
            {
                _logger.LogInformation("Caching board ID {BoardId} for board name {BoardName}.", boardId, boardName);
                await cacheService.SetAsync(cacheKey, boardId, TimeSpan.FromDays(365));
            }

            return boardId;
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

            var allowedChannels = new[]
            {
                "Direct",
                "Social",
                "Referral",
                "Screens",
                "Card",
                "Web"
            };

            var channel = dto.Channel;
            channel = allowedChannels
                .FirstOrDefault(c => string.Equals(c, channel, StringComparison.OrdinalIgnoreCase))
                ?? "Not Set";

            var columnValuesObject = new Dictionary<string, object?>
            {
                ["status"] = new { label = "Working on it" },
                ["color_mksadsjf"] = new { label = channel },
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

        public async Task<StockBoardSyncResult> SyncStockLevelsAsync(List<StockItemDto> stockItems)
        {
            var alertDate = DateTime.UtcNow;

            var boardId = await GetBoardIdByNameAsync("Stock Levels")
                         ?? throw new InvalidOperationException("Stock Levels board not found.");

            var query = @"
                query GetBoardItems($boardId: [ID!], $cursor: String) {
                  boards(ids: $boardId) {
                    items_page(limit: 500, cursor: $cursor) {
                      cursor
                      items {
                        id
                        name
                        column_values(ids: [
                          ""text_mkt4f493"",
                          ""status"",
                          ""date4"",
                          ""numeric_mkt46h37"",
                          ""numeric_mkt4q9cz"",
                          ""numeric_mkt46r6t"",
                          ""text_mkt4tafy"",
                          ""text_mkt46vft""
                        ]) {
                          id
                          text
                          value
                        }
                      }
                    }
                  }
                }";

            var mondayItems = new List<JsonNode>();
            string? cursor = null;

            while (true)
            {
                var payload = new { query, variables = new { boardId = new[] { boardId }, cursor } };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, content));
                response.EnsureSuccessStatusCode();

                var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                var page = json?["data"]?["boards"]?[0]?["items_page"];
                var items = page?["items"]?.AsArray();
                if (items != null) mondayItems.AddRange(items);

                cursor = page?["cursor"]?.ToString();
                if (string.IsNullOrEmpty(cursor)) break;
            }

            static double? ParseNumber(JsonNode? cv)
            {
                if (cv is null) return null;

                var valueStr = cv["value"]?.ToString();

                if (!string.IsNullOrWhiteSpace(valueStr))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(valueStr);
                        var root = doc.RootElement;

                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            if (root.TryGetProperty("number", out var n))
                            {
                                if (n.ValueKind == JsonValueKind.Number) return n.GetDouble();
                                if (n.ValueKind == JsonValueKind.String && double.TryParse(n.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dn)) return dn;
                            }

                            if (root.TryGetProperty("amount", out var a))
                            {
                                if (a.ValueKind == JsonValueKind.Number) return a.GetDouble();
                                if (a.ValueKind == JsonValueKind.String && double.TryParse(a.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var da)) return da;
                            }
                        }
                        else if (root.ValueKind == JsonValueKind.Number)
                        {
                            return root.GetDouble();
                        }
                        else if (root.ValueKind == JsonValueKind.String)
                        {
                            var s = root.GetString();
                            if (!string.IsNullOrWhiteSpace(s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var ds)) return ds;
                        }
                    }
                    catch
                    {
                        // ignore and fall back to 'text'
                    }
                }

                var textStr = cv["text"]?.ToString();
                if (!string.IsNullOrWhiteSpace(textStr) && double.TryParse(textStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var dt)) return dt;

                return null;
            }

            var mondayMap = new Dictionary<string, (string ItemId, string? Status, double? Qty, double? MinAlert, double? MaxAlert)>(StringComparer.Ordinal);

            foreach (var i in mondayItems)
            {
                var id = i?["id"]?.ToString();
                var cvs = i?["column_values"]?.AsArray();
                if (string.IsNullOrWhiteSpace(id) || cvs == null) continue;

                string? stockId = null;
                string? status = null;
                double? qty = null;
                double? minAlert = null;
                double? maxAlert = null;

                foreach (var cv in cvs)
                {
                    var colId = cv?["id"]?.ToString();
                    switch (colId)
                    {
                        case "text_mkt4f493":
                            stockId = cv?["text"]?.ToString()?.Trim();
                            break;
                        case "status":
                            status = cv?["text"]?.ToString();
                            break;
                        case "numeric_mkt46h37":
                            qty = ParseNumber(cv);
                            break;
                        case "numeric_mkt4q9cz":
                            minAlert = ParseNumber(cv);
                            break;
                        case "numeric_mkt46r6t":
                            maxAlert = ParseNumber(cv);
                            break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(stockId) && !mondayMap.ContainsKey(stockId))
                {
                    mondayMap.Add(stockId!, (id!, status, qty, minAlert, maxAlert));
                }
            }

            static string ComputeStatus(StockItemDto dto)
            {
                if (dto.TotalQuantity.HasValue && dto.TotalQuantity <= 0) return "Out of Stock";
                if (dto.TotalQuantity.HasValue && dto.MinAlert.HasValue && dto.MinAlert.Value > 0 && dto.TotalQuantity <= dto.MinAlert) return "Very Low";
                if (dto.TotalQuantity.HasValue && dto.MaxAlert.HasValue && dto.MaxAlert.Value > 0 && dto.TotalQuantity <= dto.MaxAlert) return "Running Low";
                return "OK";
            }

            static bool ShouldUpdate(double? prevQty, string? prevStatus, double? newQty, string desiredStatus, double? minAlert, double? maxAlert)
            {
                if (!newQty.HasValue) return false;

                if (newQty.Value <= 0)
                {
                    return !string.Equals(prevStatus, desiredStatus, StringComparison.Ordinal);
                }

                if (!string.Equals(prevStatus, desiredStatus, StringComparison.Ordinal))
                {
                    return true;
                }

                if (!prevQty.HasValue) return true;

                var decrease = prevQty.Value - newQty.Value;
                if (decrease <= 0) return false;

                double DefaultThreshold(double baseline) => Math.Max(10d, Math.Ceiling(baseline * 0.10d));

                double threshold;
                if (string.Equals(prevStatus, "Very Low", StringComparison.Ordinal))
                {
                    var basis = minAlert ?? 0d;
                    threshold = Math.Max(1d, Math.Ceiling(basis * 0.10d));
                }
                else if (string.Equals(prevStatus, "Running Low", StringComparison.Ordinal))
                {
                    var basis = maxAlert ?? 0d;
                    threshold = Math.Max(1d, Math.Ceiling(basis * 0.10d));
                }
                else
                {
                    threshold = DefaultThreshold(prevQty.Value);
                }

                return decrease >= threshold;
            }

            var createdIds = new List<string>();
            var updatedIds = new List<string>();

            var batchedUpdates = new List<(string ItemId, string ColumnValuesJson)>();
            var batchedCreates = new List<(string ItemName, string ColumnValuesJson)>();

            var activeStock = stockItems.Where(s => s.IsActive.GetValueOrDefault(false)).ToList();

            foreach (var dto in activeStock)
            {
                var stockId = dto.Id.ToString();
                var desiredStatus = ComputeStatus(dto);

                if (mondayMap.TryGetValue(stockId, out var current))
                {
                    var prevQty = current.Qty;
                    var prevStatus = current.Status;
                    var minA = dto.MinAlert.HasValue ? (double?)dto.MinAlert.Value : current.MinAlert;
                    var maxA = dto.MaxAlert.HasValue ? (double?)dto.MaxAlert.Value : current.MaxAlert;
                    var newQty = dto.TotalQuantity.HasValue ? (double?)dto.TotalQuantity.Value : null;

                    if (ShouldUpdate(prevQty, prevStatus, newQty, desiredStatus, minA, maxA))
                    {
                        var columnValuesObject = new Dictionary<string, object?>
                        {
                            ["text_mkt4f493"] = stockId,
                            ["status"] = new { label = desiredStatus },
                            ["date4"] = new { date = alertDate.ToString("yyyy-MM-dd") },
                            ["numeric_mkt46h37"] = dto.TotalQuantity,
                            ["numeric_mkt4q9cz"] = dto.MinAlert,
                            ["numeric_mkt46r6t"] = dto.MaxAlert,
                            ["text_mkt4tafy"] = dto.Division,
                            ["text_mkt46vft"] = (dto.Unit?.ToLowerInvariant() ?? string.Empty).Pluralize()
                        };

                        var columnValuesJson = JsonSerializer.Serialize(columnValuesObject, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = null,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });

                        batchedUpdates.Add((current.ItemId, columnValuesJson));
                        updatedIds.Add(stockId);
                    }
                }
                else
                {
                    if (dto.TotalQuantity.HasValue)
                    {
                        var columnValuesObject = new Dictionary<string, object?>
                        {
                            ["text_mkt4f493"] = stockId,
                            ["status"] = new { label = desiredStatus },
                            ["date4"] = new { date = alertDate.ToString("yyyy-MM-dd") },
                            ["numeric_mkt46h37"] = dto.TotalQuantity,
                            ["numeric_mkt4q9cz"] = dto.MinAlert,
                            ["numeric_mkt46r6t"] = dto.MaxAlert,
                            ["text_mkt4tafy"] = dto.Division,
                            ["text_mkt46vft"] = (dto.Unit?.ToLowerInvariant() ?? string.Empty).Pluralize()
                        };

                        var columnValuesJson = JsonSerializer.Serialize(columnValuesObject, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = null,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });

                        batchedCreates.Add((dto.Name, columnValuesJson));
                        createdIds.Add(stockId);
                    }
                }
            }

            if (batchedUpdates.Count > 0)
            {
                await ExecuteBatchedUpdatesAsync(boardId, batchedUpdates, 25);
            }

            if (batchedCreates.Count > 0)
            {
                await ExecuteBatchedCreatesAsync(boardId, batchedCreates, 20);
            }

            var idsPassedIn = new HashSet<string>(activeStock.Select(x => x.Id.ToString()), StringComparer.Ordinal);
            var mondayToDelete = mondayMap.Keys.Except(idsPassedIn).ToList();

            if (mondayToDelete.Count > 0)
            {
                var deleteIds = mondayToDelete
                    .Select(stockId => mondayMap[stockId].ItemId)
                    .Distinct()
                    .ToList();

                await ExecuteBatchedDeletesAsync(deleteIds, 50);
            }

            return new StockBoardSyncResult
            {
                Created = createdIds,
                Updated = updatedIds,
                ExistingMondayIds = mondayMap.Keys.ToList()
            };
        }

        private async Task ExecuteBatchedDeletesAsync(
            IEnumerable<string> itemIds,
            int batchSize = 50)
        {
            var batch = new List<string>(batchSize);

            foreach (var id in itemIds)
            {
                batch.Add(id);
                if (batch.Count == batchSize)
                {
                    await PostDeleteBatchAsync(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await PostDeleteBatchAsync(batch);
            }
        }

        private async Task PostDeleteBatchAsync(List<string> batch)
        {
            var sb = new StringBuilder();
            sb.Append("mutation(");
            for (int i = 0; i < batch.Count; i++)
            {
                sb.Append($"$itemId{i}: ID!,");
            }
            sb.Length--;
            sb.Append("){");

            for (int i = 0; i < batch.Count; i++)
            {
                sb.Append($@"
                    d{i}: delete_item(item_id: $itemId{i}) {{ id }}");
            }

            sb.Append("}");

            var variables = new Dictionary<string, object?>();
            for (int i = 0; i < batch.Count; i++)
            {
                variables[$"itemId{i}"] = batch[i];
            }

            var payload = new { query = sb.ToString(), variables };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, content));
            response.EnsureSuccessStatusCode();
        }


        private async Task ExecuteBatchedUpdatesAsync(
            string boardId,
            IEnumerable<(string ItemId, string ColumnValuesJson)> updates,
            int batchSize = 25)
        {
            var batch = new List<(string ItemId, string ColumnValuesJson)>(batchSize);

            foreach (var u in updates)
            {
                batch.Add(u);
                if (batch.Count == batchSize)
                {
                    await PostUpdateBatchAsync(boardId, batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await PostUpdateBatchAsync(boardId, batch);
            }
        }

        private async Task PostUpdateBatchAsync(
            string boardId,
            List<(string ItemId, string ColumnValuesJson)> batch)
        {
            var sb = new StringBuilder();
            sb.Append("mutation(");
            sb.Append("$boardId: ID!,");
            for (int i = 0; i < batch.Count; i++)
            {
                sb.Append($"$itemId{i}: ID!,");
                sb.Append($"$values{i}: JSON!,");
            }
            sb.Length--;
            sb.Append("){");

            for (int i = 0; i < batch.Count; i++)
            {
                sb.Append($@"
                    u{i}: change_multiple_column_values(
                      board_id: $boardId,
                      item_id: $itemId{i},
                      column_values: $values{i}
                    ){{ id }}");
            }

            sb.Append("}");

            var variables = new Dictionary<string, object?>
            {
                ["boardId"] = boardId
            };

            for (int i = 0; i < batch.Count; i++)
            {
                variables[$"itemId{i}"] = batch[i].ItemId;
                variables[$"values{i}"] = batch[i].ColumnValuesJson;
            }

            var payload = new { query = sb.ToString(), variables };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, content));
            response.EnsureSuccessStatusCode();
        }

        private async Task ExecuteBatchedCreatesAsync(
            string boardId,
            IEnumerable<(string ItemName, string ColumnValuesJson)> creates,
            int batchSize = 20)
        {
            var batch = new List<(string ItemName, string ColumnValuesJson)>(batchSize);

            foreach (var c in creates)
            {
                batch.Add(c);
                if (batch.Count == batchSize)
                {
                    await PostCreateBatchAsync(boardId, batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await PostCreateBatchAsync(boardId, batch);
            }
        }

        private async Task PostCreateBatchAsync(
            string boardId,
            List<(string ItemName, string ColumnValuesJson)> batch)
        {
            var sb = new StringBuilder();
            sb.Append("mutation(");
            sb.Append("$boardId: ID!,");
            for (int i = 0; i < batch.Count; i++)
            {
                sb.Append($"$name{i}: String!,");
                sb.Append($"$values{i}: JSON!,");
            }
            sb.Length--;
            sb.Append("){");

            for (int i = 0; i < batch.Count; i++)
            {
                sb.Append($@"
                    c{i}: create_item(
                      board_id: $boardId,
                      item_name: $name{i},
                      column_values: $values{i}
                    ){{ id }}");
            }

            sb.Append("}");

            var variables = new Dictionary<string, object?>
            {
                ["boardId"] = boardId
            };

            for (int i = 0; i < batch.Count; i++)
            {
                variables[$"name{i}"] = batch[i].ItemName;
                variables[$"values{i}"] = batch[i].ColumnValuesJson;
            }

            var payload = new { query = sb.ToString(), variables };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, content));
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> CreateStockTakeAndInvestigationsAsync(StockTakeCompletedCommand msg, string? igLink = null)
        {
            var parentBoardId = "5034730760";
            var parentStatusLabel = msg.InvestigateItems.Count > 0 ? "Investigate" : "Done";
            var parentName = $"Stock-take {msg.Date:yyyy-MM-dd} • {msg.Division} (Accepted {msg.AcceptedItems.Count}, Investigate {msg.InvestigateItems.Count})";

            var operators = msg.AcceptedItems.Select(a => a.StockTakeEntry.OperatorName)
                .Concat(msg.InvestigateItems.Select(i => i.StockTakeEntry.OperatorName))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            var times = msg.AcceptedItems.Select(a => a.StockTakeEntry.At)
                .Concat(msg.InvestigateItems.Select(i => i.StockTakeEntry.At))
                .Where(t => t != default)
                .OrderBy(t => t)
                .ToList();

            static decimal SumCounts(IEnumerable<StockTakeObservationDto> obs) =>
                obs.Where(o => o.Code.StartsWith("CountIn", StringComparison.OrdinalIgnoreCase))
                   .Sum(o => o.Value);

            static string SplitByLocation(IEnumerable<StockTakeObservationDto> obs) =>
                string.Join(" • ",
                    obs.Where(o => o.Code.StartsWith("CountIn", StringComparison.OrdinalIgnoreCase))
                       .Select(o => $"{o.Location} {o.Value:0}"));

            string CaptureWindowHuman()
            {
                if (times.Count == 0) return string.Empty;
                if (times.Count == 1) return times[0].ToString("HH:mm");
                return $"{times.First():HH:mm}–{times.Last():HH:mm}";
            }

            string BuildParentUpdateHtml()
            {
                var statusHuman = msg.InvestigateItems.Count > 0 ? "To be investigated" : "Completed";
                var dateHuman = msg.Date.ToString("dd MMM yyyy");
                var window = CaptureWindowHuman();
                var ops = operators.Count > 0 ? string.Join(", ", operators) : "Unspecified";

                var sb = new System.Text.StringBuilder();
                sb.Append("<p><strong>Status:</strong> ").Append(statusHuman).Append("</p>");
                sb.Append("<p><strong>Mini stock take — ").Append(msg.Division).Append(" — ").Append(dateHuman).Append("</strong><br>");
                if (!string.IsNullOrEmpty(window)) sb.Append("Counts captured ").Append(window).Append(" by ").Append(ops).Append(".</p>");
                else sb.Append("Counts captured by ").Append(ops).Append(".</p>");

                // Accepted
                sb.Append("<p><strong>Accepted (").Append(msg.AcceptedItems.Count).Append(")</strong></p><ul>");
                foreach (var a in msg.AcceptedItems)
                {
                    var obs = a.StockTakeEntry.Observations ?? new List<StockTakeObservationDto>();
                    var onHand = a.Observed ?? SumCounts(obs);
                    var est = a.Estimate ?? a.StockTakeEntry.EstimatedQuantityAtCapture;
                    var open = obs.Where(o => string.Equals(o.Code, "OpenBottleWeightGrams", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Value);

                    sb.Append("<li>")
                      .Append(a.StockTakeEntry.Name)
                      .Append(" — ")
                      .Append(onHand.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                      .Append(" on hand (estimate ")
                      .Append(est.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                      .Append("). Open bottles recorded: ")
                      .Append(open.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
                      .Append(" g.</li>");
                }
                sb.Append("</ul>");

                // Investigate
                sb.Append("<p><strong>Needs investigation (").Append(msg.InvestigateItems.Count).Append(")</strong></p><ul>");
                foreach (var i in msg.InvestigateItems)
                {
                    var obs = i.StockTakeEntry.Observations ?? new List<StockTakeObservationDto>();
                    var onHand = i.Observed ?? SumCounts(obs);
                    var est = i.Estimate ?? i.StockTakeEntry.EstimatedQuantityAtCapture;
                    var variance = i.Difference ?? (onHand - est);
                    var pct = i.Percent ?? (est == 0 ? 0 : (variance / est) * 100m);
                    var reason = string.IsNullOrWhiteSpace(i.Reason) ? "Manual review" : i.Reason!;
                    var split = SplitByLocation(obs);

                    sb.Append("<li>")
                      .Append(i.StockTakeEntry.Name)
                      .Append(" — ")
                      .Append(reason.ToLowerInvariant())
                      .Append(": ")
                      .Append(onHand.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                      .Append(" counted vs ")
                      .Append(est.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                      .Append(" expected (")
                      .Append(variance.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                      .Append(", ")
                      .Append(pct.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))
                      .Append("%)");

                    if (i.Allowed.HasValue)
                    {
                        sb.Append(" — allowed ")
                          .Append(i.Allowed.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                        if (!string.IsNullOrWhiteSpace(i.DominantRule))
                            sb.Append(" (rule: ").Append(i.DominantRule).Append(")");
                    }
                    sb.Append(".");

                    if (!string.IsNullOrWhiteSpace(split)) sb.Append(" Split: ").Append(split).Append(".");
                    sb.Append("</li>");
                }
                sb.Append("</ul>");

                sb.Append("<p><strong>What to do</strong></p>")
                  .Append("<ol>")
                  .Append("<li>Recount at Store, Colt and Lounge.</li>")
                  .Append("<li>Check recent deliveries, transfers and POS sales since the estimate time.</li>")
                  .Append("<li>Review input logs if numbers look duplicated.</li>")
                  .Append("</ol>");

                return sb.ToString();
            }

            string BuildChildUpdateHtml(StockTakeItemInvestigationDto inv, decimal tolerancePercent = 10m)
            {
                var obs = inv.StockTakeEntry.Observations ?? new List<StockTakeObservationDto>();
                var countObs = obs.Where(o => o.Code.StartsWith("CountIn", StringComparison.OrdinalIgnoreCase)).ToList();
                var weightObs = obs.Where(o => o.Code.EndsWith("WeightGrams", StringComparison.OrdinalIgnoreCase)).ToList();

                var onHand = inv.Observed ?? countObs.Sum(o => o.Value);
                var est = inv.Estimate ?? inv.StockTakeEntry.EstimatedQuantityAtCapture;
                var variance = inv.Difference ?? (onHand - est);
                var pct = inv.Percent ?? (est == 0 ? 0 : (variance / est) * 100m);
                var split = SplitByLocation(obs);

                // prefer explicit reason if supplied
                var reason = string.IsNullOrWhiteSpace(inv.Reason) ? "Manual review" : inv.Reason!;
                var outsideTolerance = string.Equals(reason, "Outside tolerance", StringComparison.OrdinalIgnoreCase)
                                       || (!string.Equals(reason, "No observations", StringComparison.OrdinalIgnoreCase) && Math.Abs(pct) > tolerancePercent);
                var noObservations = string.Equals(reason, "No observations", StringComparison.OrdinalIgnoreCase)
                                     || (countObs.Count == 0 && weightObs.Count == 0);
                var partialObservations = string.Equals(reason, "Partial / incomplete observations", StringComparison.OrdinalIgnoreCase)
                                          || (!noObservations && (countObs.Count == 0 ^ weightObs.Count == 0));

                var sb = new System.Text.StringBuilder();
                sb.Append("<p><strong>Why this needs checking:</strong> ").Append(reason).Append("</p>");
                sb.Append("<p>")
                  .Append(onHand.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(" counted vs ")
                  .Append(est.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(" expected (")
                  .Append(variance.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(", ")
                  .Append(pct.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))
                  .Append("%)");

                if (inv.Allowed.HasValue)
                {
                    sb.Append(" — allowed ")
                      .Append(inv.Allowed.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(inv.DominantRule))
                        sb.Append(" (rule: ").Append(inv.DominantRule).Append(")");
                }
                sb.Append("</p>");

                if (!string.IsNullOrWhiteSpace(split)) sb.Append("<p>Split: ").Append(split).Append("</p>");

                sb.Append("<p>Captured by ").Append(inv.StockTakeEntry.OperatorName)
                  .Append(" at ").Append(inv.StockTakeEntry.At.ToString("dd MMM yyyy HH:mm"))
                  .Append("</p>");

                if (outsideTolerance)
                {
                    sb.Append("<p><strong>Actions</strong></p><ol>")
                      .Append("<li>Confirm the relevant purchase orders have been logged.</li>")
                      .Append("<li>Confirm wastage for this item has been accurately recorded.</li>")
                      .Append("<li>Ensure the staff member knows how to count/weight this item and use the app correctly.</li>")
                      .Append("</ol>")
                      .Append("<p><em>Tip:</em> Perform another stock take for this product yourself.</p>");
                }
                else if (noObservations)
                {
                    sb.Append("<p><strong>Actions</strong></p><ol>")
                      .Append("<li>Verify the item is still sold and should be active in IG.</li>")
                      .Append("<li>If it is still stocked, ensure staff record <strong>0</strong> where none are found rather than leaving fields blank in the app.</li>")
                      .Append("</ol>");
                }
                else if (partialObservations)
                {
                    sb.Append("<p><strong>Actions</strong></p><ol>")
                      .Append("<li>Ensure the staff member understands the difference between recording <strong>zero</strong> and leaving a field blank.</li>")
                      .Append("<li>For opened bottles, ensure they know how to <strong>weigh</strong> them correctly.</li>")
                      .Append("</ol>");
                }
                else
                {
                    sb.Append("<p><strong>Actions</strong></p><ul>")
                      .Append("<li>Recount all locations.</li>")
                      .Append("<li>Check deliveries, transfers and POS since the estimate.</li>")
                      .Append("</ul>");
                }

                sb.Append("<p>Outcome/notes:</p>");
                return sb.ToString();
            }

            // parent row
            var parentValues = new Dictionary<string, object?>
            {
                ["status"] = new { label = parentStatusLabel },
                ["date4"] = new { date = msg.Date.ToString("yyyy-MM-dd") },
                ["text_mkwqn3vg"] = msg.Division,
                ["text_mkwqkapb"] = string.Join(", ", operators)
            };
            if (!string.IsNullOrWhiteSpace(igLink))
            {
                parentValues["link_mkwqs968"] = new { url = igLink, text = igLink };
            }

            var parentValuesJson = System.Text.Json.JsonSerializer.Serialize(parentValues, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var createParentPayload = new
            {
                query = @"mutation ($boardId: ID!, $name: String!, $values: JSON!) {
                     create_item(board_id: $boardId, item_name: $name, column_values: $values) { id }
                 }",
                variables = new { boardId = parentBoardId, name = parentName, values = parentValuesJson }
            };

            using var parentContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(createParentPayload), Encoding.UTF8, "application/json");
            var parentResp = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, parentContent));
            parentResp.EnsureSuccessStatusCode();
            var parentJson = System.Text.Json.Nodes.JsonNode.Parse(await parentResp.Content.ReadAsStringAsync());
            var parentItemId = parentJson?["data"]?["create_item"]?["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(parentItemId)) throw new InvalidOperationException("Failed to create parent item.");

            var parentUpdatePayload = new
            {
                query = @"mutation ($itemId: ID!, $body: String!) {
                    create_update(item_id: $itemId, body: $body) { id }
                 }",
                variables = new { itemId = parentItemId, body = BuildParentUpdateHtml() }
            };

            using var updContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(parentUpdatePayload), Encoding.UTF8, "application/json");
            var updResp = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, updContent));
            updResp.EnsureSuccessStatusCode();

            // subitems
            foreach (var inv in msg.InvestigateItems)
            {
                var obs = inv.StockTakeEntry.Observations ?? new List<StockTakeObservationDto>();
                var observed = inv.Observed ?? SumCounts(obs);
                var est = inv.Estimate ?? inv.StockTakeEntry.EstimatedQuantityAtCapture;
                var variance = inv.Difference ?? (observed - est);
                var pct = inv.Percent ?? (est == 0 ? 0 : (variance / est) * 100m);
                var reason = string.IsNullOrWhiteSpace(inv.Reason) ? "Manual review" : inv.Reason!;
                var varianceText = $"{variance.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} ({pct.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}%)";

                var subValues = new Dictionary<string, object?>
                {
                    ["status"] = new { label = "Todo" },
                    ["text_mkwq56zm"] = reason,
                    ["text_mkwq6qpk"] = observed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["text_mkwqwnx3"] = est.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["text_mkwqrhav"] = varianceText
                };
                if (!string.IsNullOrWhiteSpace(igLink))
                {
                    subValues["link_mkwqwxvb"] = new { url = igLink, text = igLink };
                }

                var subValuesJson = System.Text.Json.JsonSerializer.Serialize(subValues, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var subPayload = new
                {
                    query = @"mutation ($parentId: ID!, $name: String!, $values: JSON!) {
                        create_subitem(parent_item_id: $parentId, item_name: $name, column_values: $values) { id }
                     }",
                    variables = new
                    {
                        parentId = parentItemId,
                        name = inv.StockTakeEntry.Name, // child title = product name
                        values = subValuesJson
                    }
                };

                using var subContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(subPayload), Encoding.UTF8, "application/json");
                var subResp = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, subContent));
                subResp.EnsureSuccessStatusCode();

                var subJson = System.Text.Json.Nodes.JsonNode.Parse(await subResp.Content.ReadAsStringAsync());
                var subItemId = subJson?["data"]?["create_subitem"]?["id"]?.ToString();
                if (string.IsNullOrWhiteSpace(subItemId)) continue;

                var subBody = BuildChildUpdateHtml(inv, 10m);
                var subUpdatePayload = new
                {
                    query = @"mutation ($itemId: ID!, $body: String!) {
                        create_update(item_id: $itemId, body: $body) { id }
                     }",
                    variables = new { itemId = subItemId, body = subBody }
                };

                using var subUpdContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(subUpdatePayload), Encoding.UTF8, "application/json");
                var subUpdResp = await _mondayApiPolicy.ExecuteAsync(() => _httpClient.PostAsync(string.Empty, subUpdContent));
                subUpdResp.EnsureSuccessStatusCode();
            }

            return parentItemId!;
        }
    }
}