using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BOTGC.API.Services
{
    public class MondayClient : ITaskBoardService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public MondayClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.monday.com/v2/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<string?> GetUserIdByEmailAsync(string email)
        {
            var query = @"{ users { id name email } }";

            var payload = new { query };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var users = json?["data"]?["users"]?.AsArray();

            var user = users?
                .FirstOrDefault(u => u?["email"]?.ToString()?.ToLowerInvariant() == email.ToLowerInvariant());

            return user?["id"]?.ToString();
        }


        private async Task<string?> GetBoardIdByNameAsync(string boardName)
        {
            var query = @"{ boards(limit: 40) { id name } }";

            var payload = new { query };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            return json?["data"]?["boards"]?.AsArray()?
                .FirstOrDefault(b => b?["name"]?.ToString() == boardName)?["id"]?.ToString();
        }

        public async Task<string> CreateMemberApplicationAsync(NewMemberApplicationDto dto)
        {
            var boardId = await GetBoardIdByNameAsync("Membership Applications")
                         ?? throw new InvalidOperationException("Board not found.");

            var itemName = $"Online Application - {dto.Forename} {dto.Surname}";

            var userId = await GetUserIdByEmailAsync("clubmanager@botgc.co.uk");

            var columnValues = new Dictionary<string, object?>
            {
                ["status"] = new { label = "Working on it" },
                ["color_mkq7h26c"] = new { label = dto.MembershipCategory },
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
            return responseJson?["data"]?["create_item"]?["id"]?.ToString()!;
        }

        public async Task<string> AttachFile(string itemId, byte[] fileBytes, string fileName)
        {
            string query = "mutation ($file: File!, $itemId: ID!) " +
                           "{ add_file_to_column(item_id: $itemId, column_id: \"file_mkq7bhzz\", file: $file) { id } }";
            string variablesJson = $"{{ \"itemId\": \"{itemId}\" }}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", _apiKey);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(query), "query");
            form.Add(new StringContent(variablesJson), "variables");
            form.Add(new StringContent("{\"file\": [\"variables.file\"]}"), "map");

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            form.Add(fileContent, "file", fileName);

            HttpResponseMessage response = await client.PostAsync("https://api.monday.com/v2/file", form);
            string responseBody = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            return responseBody;
        }
    }

}
