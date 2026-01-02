using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace BOTGC.MemberPortal.Services;

public sealed class FileSystemQuizContentSource : IQuizContentSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly FileSystemQuizSourceOptions _options;

    public FileSystemQuizContentSource(FileSystemQuizSourceOptions options)
    {
        _options = options;
    }

    public async Task<QuizContentSnapshot> LoadAsync(CancellationToken ct = default)
    {
        var questionsRoot = Path.Combine(_options.RootPath, _options.QuestionsFolderName);
        if (!Directory.Exists(questionsRoot))
        {
            return new QuizContentSnapshot(DateTimeOffset.UtcNow, Array.Empty<QuizQuestion>());
        }

        var questions = new List<QuizQuestion>(capacity: 256);

        foreach (var difficultyDir in Directory.EnumerateDirectories(questionsRoot))
        {
            var difficultyName = Path.GetFileName(difficultyDir);
            var difficulty = ParseDifficulty(difficultyName);
            if (difficulty is null)
            {
                continue;
            }

            foreach (var questionDir in Directory.EnumerateDirectories(difficultyDir))
            {
                ct.ThrowIfCancellationRequested();

                var questionId = Path.GetFileName(questionDir);
                var metaPath = Path.Combine(questionDir, "meta.json");
                if (!File.Exists(metaPath))
                {
                    continue;
                }

                QuizMeta meta;
                await using (var fs = File.OpenRead(metaPath))
                {
                    meta = await JsonSerializer.DeserializeAsync<QuizMeta>(fs, JsonOptions, ct)
                           ?? throw new InvalidOperationException($"Unable to parse {metaPath}.");
                }

                var imagePath = Path.Combine(questionDir, _options.ImageFileName);
                var imageUrl = new Uri(imagePath).AbsoluteUri;

                questions.Add(new QuizQuestion(
                    Id: questionId,
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
        }

        return new QuizContentSnapshot(DateTimeOffset.UtcNow, questions);
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