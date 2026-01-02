using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace BOTGC.MemberPortal.Services;

public sealed class GitHubZipQuizContentSource : IQuizContentSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly HttpClient _http;
    private readonly GitHubQuizSourceOptions _options;

    public GitHubZipQuizContentSource(HttpClient http, GitHubQuizSourceOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<QuizContentSnapshot> LoadAsync(CancellationToken ct = default)
    {
        var zipUrl = $"https://codeload.github.com/{_options.Owner}/{_options.Repo}/zip/refs/heads/{_options.Ref}";
        using var req = new HttpRequestMessage(HttpMethod.Get, zipUrl);

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        }

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var zipStream = await resp.Content.ReadAsStreamAsync(ct);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        var rootPrefix = $"{_options.Repo}-{_options.Ref}/{_options.RootPath.Trim('/')}/";
        var questions = new List<QuizQuestion>(capacity: 256);

        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            if (!entry.FullName.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!entry.FullName.EndsWith("/meta.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = entry.FullName.Substring(rootPrefix.Length);
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                continue;
            }

            var difficultyFolder = parts[0];
            var questionFolder = parts[1];

            var difficulty = ParseDifficulty(difficultyFolder);
            if (difficulty is null)
            {
                continue;
            }

            QuizMeta meta;
            await using (var metaStream = entry.Open())
            {
                meta = await JsonSerializer.DeserializeAsync<QuizMeta>(metaStream, JsonOptions, ct)
                       ?? throw new InvalidOperationException($"Unable to parse meta.json for {entry.FullName}.");
            }

            var imageUrl = BuildRawUrl(difficultyFolder, questionFolder, "image.png");

            questions.Add(new QuizQuestion(
                Id: questionFolder,
                Difficulty: difficulty.Value,
                Question: meta.Question,
                Type: meta.Type,
                Choices: meta.Choices.Select(c => new QuizChoice(c.Id, c.Text)).ToArray(),
                CorrectAnswer: meta.CorrectAnswer,
                Explanation: meta.Explanation,
                ImageUrl: imageUrl,
                ImageAlt: meta.ImageAlt
            ));
        }

        return new QuizContentSnapshot(DateTimeOffset.UtcNow, questions);
    }

    private string BuildRawUrl(string difficultyFolder, string questionFolder, string fileName)
    {
        var path = $"{_options.RootPath.Trim('/')}/{difficultyFolder}/{questionFolder}/{fileName}";
        return $"https://raw.githubusercontent.com/{_options.Owner}/{_options.Repo}/{_options.Ref}/{path}";
    }

    private static QuizDifficulty? ParseDifficulty(string folder)
    {
        return folder.ToLowerInvariant() switch
        {
            "easy" => QuizDifficulty.Easy,
            "medium" => QuizDifficulty.Medium,
            "hard" => QuizDifficulty.Hard,
            _ => null,
        };
    }

    private sealed class QuizMeta
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("question")]
        public required string Question { get; set; }

        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("choices")]
        public required List<Choice> Choices { get; set; }

        [JsonPropertyName("correctAnswer")]
        public required string CorrectAnswer { get; set; }

        [JsonPropertyName("explanation")]
        public required string Explanation { get; set; }

        [JsonPropertyName("imageAlt")]
        public required string ImageAlt { get; set; }

        public sealed class Choice
        {
            [JsonPropertyName("id")]
            public required string Id { get; set; }

            [JsonPropertyName("text")]
            public required string Text { get; set; }
        }
    }
}
