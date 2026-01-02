// Services.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using global::BOTGC.MemberPortal.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class QuizContentCache
{
    private readonly ICacheService _cache;
    private readonly JuniorQuizOptions _options;

    public QuizContentCache(ICacheService cache, JuniorQuizOptions options)
    {
        _cache = cache;
        _options = options;
    }

    public string Key => $"{_options.CacheKeyPrefix}:content";

    public Task<QuizContentSnapshot?> GetAsync(CancellationToken ct = default)
    {
        return _cache.GetAsync<QuizContentSnapshot>(Key, ct);
    }

    public Task SetAsync(QuizContentSnapshot snapshot, CancellationToken ct = default)
    {
        return _cache.SetAsync(Key, snapshot, _options.CacheTtl, ct);
    }
}

public sealed class QuizContentSynchroniser
{
    private readonly IQuizContentSource _source;
    private readonly QuizContentCache _cache;

    public QuizContentSynchroniser(IQuizContentSource source, QuizContentCache cache)
    {
        _source = source;
        _cache = cache;
    }

    public async Task<QuizContentSnapshot> RefreshAsync(CancellationToken ct = default)
    {
        var snapshot = await _source.LoadAsync(ct);
        await _cache.SetAsync(snapshot, ct);
        return snapshot;
    }

    public Task<QuizContentSnapshot?> GetCachedAsync(CancellationToken ct = default)
    {
        return _cache.GetAsync(ct);
    }
}

public sealed class CachedQuizQuestionProvider : IQuizQuestionProvider
{
    private readonly QuizContentCache _cache;

    public CachedQuizQuestionProvider(QuizContentCache cache)
    {
        _cache = cache;
    }

    public Task<QuizContentSnapshot?> GetSnapshotAsync(CancellationToken ct = default)
    {
        return _cache.GetAsync(ct);
    }
}

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

public sealed class QuizSelectionEngine
{
    private readonly IQuizQuestionProvider _questions;
    private readonly IQuizAttemptRepository _attempts;

    public QuizSelectionEngine(IQuizQuestionProvider questions, IQuizAttemptRepository attempts)
    {
        _questions = questions;
        _attempts = attempts;
    }

    public async Task<(string ContentVersion, IReadOnlyList<QuizQuestion> Selected)> SelectAsync(
        string userId,
        int count,
        IReadOnlyCollection<QuizDifficulty>? allowedDifficulties,
        CancellationToken ct = default)
    {
        if (count <= 0)
        {
            return ("", Array.Empty<QuizQuestion>());
        }

        var snapshot = await _questions.GetSnapshotAsync(ct);
        if (snapshot is null || snapshot.Questions.Count == 0)
        {
            return ("", Array.Empty<QuizQuestion>());
        }

        var contentVersion = ComputeContentVersion(snapshot);

        var all = snapshot.Questions;

        if (allowedDifficulties is not null && allowedDifficulties.Count > 0)
        {
            var allowed = new HashSet<QuizDifficulty>(allowedDifficulties);
            all = all.Where(q => allowed.Contains(q.Difficulty)).ToArray();
        }

        if (all.Count == 0)
        {
            return (contentVersion, Array.Empty<QuizQuestion>());
        }

        var previouslyAsked = await _attempts.GetPreviouslyAskedQuestionIdsAsync(userId, ct);
        var previouslyCorrect = await _attempts.GetPreviouslyCorrectQuestionIdsAsync(userId, ct);

        var selected = new List<QuizQuestion>(count);

        var strictPool = all.Where(q => !previouslyAsked.Contains(q.Id)).ToList();
        FillRandomly(selected, strictPool, count);

        if (selected.Count < count)
        {
            var mediumPool = all
                .Where(q => !previouslyCorrect.Contains(q.Id))
                .Where(q => selected.All(s => !string.Equals(s.Id, q.Id, StringComparison.Ordinal)))
                .ToList();

            FillRandomly(selected, mediumPool, count);
        }

        if (selected.Count < count)
        {
            var fallbackPool = all
                .Where(q => selected.All(s => !string.Equals(s.Id, q.Id, StringComparison.Ordinal)))
                .ToList();

            FillRandomly(selected, fallbackPool, count);
        }

        return (contentVersion, selected);
    }

    private static void FillRandomly(List<QuizQuestion> target, List<QuizQuestion> pool, int targetCount)
    {
        if (target.Count >= targetCount || pool.Count == 0)
        {
            return;
        }

        for (var i = pool.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var needed = targetCount - target.Count;

        for (var i = 0; i < pool.Count && needed > 0; i++)
        {
            target.Add(pool[i]);
            needed--;
        }
    }

    private static string ComputeContentVersion(QuizContentSnapshot snapshot)
    {
        var ids = snapshot.Questions.Select(q => q.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var payload = $"{snapshot.LoadedAtUtc:O}|{string.Join(",", ids)}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class QuizService
{
    private readonly QuizSelectionEngine _selector;
    private readonly IQuizAttemptRepository _repo;
    private readonly IQuizQuestionProvider _questions;

    public QuizService(QuizSelectionEngine selector, IQuizAttemptRepository repo, IQuizQuestionProvider questions)
    {
        _selector = selector;
        _repo = repo;
        _questions = questions;
    }

    public async Task<StartQuizResult> StartQuizAsync(StartQuizRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetInProgressAttemptAsync(request.UserId, ct);
        if (existing is not null)
        {
            return new StartQuizResult(false, existing);
        }

        var (contentVersion, selected) = await _selector.SelectAsync(request.UserId, request.QuestionCount, request.AllowedDifficulties, ct);

        if (selected.Count == 0)
        {
            throw new InvalidOperationException("No quiz questions available.");
        }

        var attemptId = CreateAttemptId();
        var now = DateTimeOffset.UtcNow;

        var attempt = new QuizAttempt(
            AttemptId: attemptId,
            UserId: request.UserId,
            Status: "InProgress",
            StartedAtUtc: now,
            FinishedAtUtc: null,
            TotalQuestions: selected.Count,
            CorrectCount: 0,
            PassMark: request.PassMark,
            ContentVersion: contentVersion,
            QuestionIdsInOrder: selected.Select(q => q.Id).ToArray()
        );

        var created = await _repo.CreateAttemptAsync(attempt, ct);

        return new StartQuizResult(true, created);
    }

    public Task<QuizAttempt?> GetInProgressAttemptAsync(string userId, CancellationToken ct = default)
    {
        return _repo.GetInProgressAttemptAsync(userId, ct);
    }

    public Task<IReadOnlyList<QuizAttemptSummary>> ListCompletedAttemptsAsync(string userId, int take, CancellationToken ct = default)
    {
        return _repo.ListCompletedAttemptsAsync(userId, take, ct);
    }

    public Task<QuizAttemptDetails?> GetAttemptDetailsAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        return _repo.GetAttemptDetailsAsync(userId, attemptId, ct);
    }

    public async Task<QuizQuestion?> GetNextQuestionAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var details = await _repo.GetAttemptDetailsAsync(userId, attemptId, ct);
        if (details is null)
        {
            return null;
        }

        var attempt = details.Attempt;
        if (!string.Equals(attempt.Status, "InProgress", StringComparison.Ordinal))
        {
            return null;
        }

        var answered = new HashSet<string>(details.Answers.Select(a => a.QuestionId), StringComparer.Ordinal);
        var nextId = attempt.QuestionIdsInOrder.FirstOrDefault(qid => !answered.Contains(qid));
        if (nextId is null)
        {
            return null;
        }

        var snapshot = await _questions.GetSnapshotAsync(ct);
        if (snapshot is null)
        {
            return null;
        }

        return snapshot.Questions.FirstOrDefault(q => string.Equals(q.Id, nextId, StringComparison.Ordinal));
    }

    public async Task<AnswerQuestionResult?> AnswerAsync(string userId, string attemptId, string questionId, string selectedAnswerId, CancellationToken ct = default)
    {
        var details = await _repo.GetAttemptDetailsAsync(userId, attemptId, ct);
        if (details is null)
        {
            return null;
        }

        var attempt = details.Attempt;
        if (!string.Equals(attempt.Status, "InProgress", StringComparison.Ordinal))
        {
            return null;
        }

        var snapshot = await _questions.GetSnapshotAsync(ct);
        if (snapshot is null)
        {
            return null;
        }

        var question = snapshot.Questions.FirstOrDefault(q => string.Equals(q.Id, questionId, StringComparison.Ordinal));
        if (question is null)
        {
            return null;
        }

        var isCorrect = string.Equals(selectedAnswerId, question.CorrectAnswer, StringComparison.Ordinal);

        var questionIndex = attempt.QuestionIdsInOrder
            .Select((id, idx) => (id, idx))
            .Where(x => string.Equals(x.id, questionId, StringComparison.Ordinal))
            .Select(x => x.idx)
            .DefaultIfEmpty(-1)
            .First();

        if (questionIndex < 0)
        {
            return null;
        }

        var existingAnswer = await _repo.GetAnswerAsync(attemptId, questionId, ct);

        var newAnswer = new QuestionAttempt(
            QuestionId: questionId,
            SelectedAnswerId: selectedAnswerId,
            IsCorrect: isCorrect,
            AnsweredAtUtc: DateTimeOffset.UtcNow
        );

        await _repo.UpsertAnswerAsync(attemptId, questionIndex, newAnswer, ct);

        var correctCount = attempt.CorrectCount;

        if (existingAnswer is null)
        {
            if (isCorrect)
            {
                correctCount += 1;
            }
        }
        else
        {
            if (existingAnswer.IsCorrect && !isCorrect)
            {
                correctCount -= 1;
            }

            if (!existingAnswer.IsCorrect && isCorrect)
            {
                correctCount += 1;
            }
        }

        var answeredIds = new HashSet<string>(details.Answers.Select(a => a.QuestionId), StringComparer.Ordinal);
        answeredIds.Add(questionId);

        var isFinished = answeredIds.Count >= attempt.TotalQuestions;

        var updatedAttempt = attempt with
        {
            CorrectCount = correctCount,
            Status = isFinished ? "Finished" : "InProgress",
            FinishedAtUtc = isFinished ? DateTimeOffset.UtcNow : null,
        };

        await _repo.UpdateAttemptAsync(updatedAttempt, ct);

        return new AnswerQuestionResult(
            AttemptId: attemptId,
            QuestionId: questionId,
            SelectedAnswerId: selectedAnswerId,
            IsCorrect: isCorrect,
            CorrectAnswerId: question.CorrectAnswer,
            Explanation: question.Explanation,
            CorrectCount: updatedAttempt.CorrectCount,
            TotalQuestions: updatedAttempt.TotalQuestions,
            PassMark: updatedAttempt.PassMark,
            IsFinished: isFinished
        );
    }

    private static string CreateAttemptId()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed class TableStorageQuizAttemptRepository : IQuizAttemptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly TableClient _attempts;
    private readonly TableClient _answers;

    public TableStorageQuizAttemptRepository(TableStorageQuizOptions options)
    {
        var service = new TableServiceClient(options.ConnectionString);

        _attempts = service.GetTableClient(options.AttemptsTableName);
        _answers = service.GetTableClient(options.AnswersTableName);

        _attempts.CreateIfNotExists();
        _answers.CreateIfNotExists();
    }

    public async Task<QuizAttempt?> GetInProgressAttemptAsync(string userId, CancellationToken ct = default)
    {
        var filter = $"PartitionKey eq '{Escape(userId)}' and Status eq 'InProgress'";
        await foreach (var entity in _attempts.QueryAsync<AttemptEntity>(filter: filter, maxPerPage: 10, cancellationToken: ct))
        {
            return entity.ToModel();
        }

        return null;
    }

    public async Task<IReadOnlyList<QuizAttemptSummary>> ListCompletedAttemptsAsync(string userId, int take, CancellationToken ct = default)
    {
        var list = new List<QuizAttemptSummary>(take);

        var filter = $"PartitionKey eq '{Escape(userId)}' and Status eq 'Finished'";
        await foreach (var entity in _attempts.QueryAsync<AttemptEntity>(filter: filter, maxPerPage: 200, cancellationToken: ct))
        {
            if (entity.FinishedAtUtc is null)
            {
                continue;
            }

            list.Add(new QuizAttemptSummary(
                AttemptId: entity.AttemptId,
                StartedAtUtc: entity.StartedAtUtc,
                FinishedAtUtc: entity.FinishedAtUtc.Value,
                TotalQuestions: entity.TotalQuestions,
                CorrectCount: entity.CorrectCount,
                PassMark: entity.PassMark
            ));
        }

        return list
            .OrderByDescending(x => x.FinishedAtUtc)
            .Take(take)
            .ToArray();
    }

    public async Task<QuizAttemptDetails?> GetAttemptDetailsAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var rowKey = AttemptRowKey(attemptId);

        AttemptEntity attemptEntity;
        try
        {
            attemptEntity = (await _attempts.GetEntityAsync<AttemptEntity>(userId, rowKey, cancellationToken: ct)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }

        var attempt = attemptEntity.ToModel();

        var answers = new List<QuestionAttempt>(attempt.TotalQuestions);
        var answersFilter = $"PartitionKey eq '{Escape(attemptId)}'";

        await foreach (var ans in _answers.QueryAsync<AnswerEntity>(filter: answersFilter, maxPerPage: 500, cancellationToken: ct))
        {
            answers.Add(ans.ToModel());
        }

        var ordered = answers
            .OrderBy(a => a.AnsweredAtUtc)
            .ToArray();

        return new QuizAttemptDetails(attempt, ordered);
    }

    public async Task<QuizAttempt> CreateAttemptAsync(QuizAttempt attempt, CancellationToken ct = default)
    {
        var entity = AttemptEntity.FromModel(attempt);
        await _attempts.AddEntityAsync(entity, ct);
        return attempt;
    }

    public async Task<QuestionAttempt?> GetAnswerAsync(string attemptId, string questionId, CancellationToken ct = default)
    {
        var filter = $"PartitionKey eq '{Escape(attemptId)}' and QuestionId eq '{Escape(questionId)}'";
        await foreach (var entity in _answers.QueryAsync<AnswerEntity>(filter: filter, maxPerPage: 10, cancellationToken: ct))
        {
            return entity.ToModel();
        }

        return null;
    }

    public Task UpsertAnswerAsync(string attemptId, int questionIndex, QuestionAttempt answer, CancellationToken ct = default)
    {
        var entity = AnswerEntity.FromModel(attemptId, questionIndex, answer);
        return _answers.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task UpdateAttemptAsync(QuizAttempt attempt, CancellationToken ct = default)
    {
        var rowKey = AttemptRowKey(attempt.AttemptId);

        AttemptEntity existing;
        try
        {
            existing = (await _attempts.GetEntityAsync<AttemptEntity>(attempt.UserId, rowKey, cancellationToken: ct)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new InvalidOperationException("Attempt not found.");
        }

        var updated = AttemptEntity.FromModel(attempt);
        updated.ETag = existing.ETag;

        await _attempts.UpdateEntityAsync(updated, updated.ETag, TableUpdateMode.Replace, ct);
    }

    public async Task<IReadOnlySet<string>> GetPreviouslyAskedQuestionIdsAsync(string userId, CancellationToken ct = default)
    {
        var asked = new HashSet<string>(StringComparer.Ordinal);

        var filter = $"PartitionKey eq '{Escape(userId)}'";
        await foreach (var entity in _attempts.QueryAsync<AttemptEntity>(filter: filter, maxPerPage: 500, cancellationToken: ct))
        {
            foreach (var qid in entity.GetQuestionIds())
            {
                asked.Add(qid);
            }
        }

        return asked;
    }

    public async Task<IReadOnlySet<string>> GetPreviouslyCorrectQuestionIdsAsync(string userId, CancellationToken ct = default)
    {
        var correct = new HashSet<string>(StringComparer.Ordinal);

        var filter = $"PartitionKey eq '{Escape(userId)}' and Status eq 'Finished'";
        await foreach (var attempt in _attempts.QueryAsync<AttemptEntity>(filter: filter, maxPerPage: 500, cancellationToken: ct))
        {
            var answersFilter = $"PartitionKey eq '{Escape(attempt.AttemptId)}' and IsCorrect eq true";
            await foreach (var ans in _answers.QueryAsync<AnswerEntity>(filter: answersFilter, maxPerPage: 1000, cancellationToken: ct))
            {
                correct.Add(ans.QuestionId);
            }
        }

        return correct;
    }

    private static string AttemptRowKey(string attemptId)
    {
        return $"ATTEMPT_{attemptId}";
    }

    private static string Escape(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private sealed class AttemptEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string AttemptId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset? FinishedAtUtc { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int PassMark { get; set; }
        public string ContentVersion { get; set; } = string.Empty;
        public string QuestionIdsJson { get; set; } = "[]";

        public QuizAttempt ToModel()
        {
            return new QuizAttempt(
                AttemptId: AttemptId,
                UserId: PartitionKey,
                Status: Status,
                StartedAtUtc: StartedAtUtc,
                FinishedAtUtc: FinishedAtUtc,
                TotalQuestions: TotalQuestions,
                CorrectCount: CorrectCount,
                PassMark: PassMark,
                ContentVersion: ContentVersion,
                QuestionIdsInOrder: GetQuestionIds()
            );
        }

        public IReadOnlyList<string> GetQuestionIds()
        {
            try
            {
                var ids = JsonSerializer.Deserialize<string[]>(QuestionIdsJson, JsonOptions) ?? Array.Empty<string>();
                return ids;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static AttemptEntity FromModel(QuizAttempt attempt)
        {
            return new AttemptEntity
            {
                PartitionKey = attempt.UserId,
                RowKey = AttemptRowKey(attempt.AttemptId),
                AttemptId = attempt.AttemptId,
                Status = attempt.Status,
                StartedAtUtc = attempt.StartedAtUtc,
                FinishedAtUtc = attempt.FinishedAtUtc,
                TotalQuestions = attempt.TotalQuestions,
                CorrectCount = attempt.CorrectCount,
                PassMark = attempt.PassMark,
                ContentVersion = attempt.ContentVersion,
                QuestionIdsJson = JsonSerializer.Serialize(attempt.QuestionIdsInOrder.ToArray(), JsonOptions),
            };
        }
    }

    private sealed class AnswerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string AttemptId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedAnswerId { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public DateTimeOffset AnsweredAtUtc { get; set; }
        public int QuestionIndex { get; set; }

        public QuestionAttempt ToModel()
        {
            return new QuestionAttempt(
                QuestionId: QuestionId,
                SelectedAnswerId: SelectedAnswerId,
                IsCorrect: IsCorrect,
                AnsweredAtUtc: AnsweredAtUtc
            );
        }

        public static AnswerEntity FromModel(string attemptId, int questionIndex, QuestionAttempt attempt)
        {
            return new AnswerEntity
            {
                PartitionKey = attemptId,
                RowKey = $"{questionIndex:000}_{attempt.QuestionId}",
                AttemptId = attemptId,
                QuestionId = attempt.QuestionId,
                SelectedAnswerId = attempt.SelectedAnswerId,
                IsCorrect = attempt.IsCorrect,
                AnsweredAtUtc = attempt.AnsweredAtUtc,
                QuestionIndex = questionIndex,
            };
        }
    }
}

public static class JuniorQuizServiceCollectionExtensions
{
    public static IServiceCollection AddJuniorQuizCommon(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var opts = configuration.GetRequiredSection("JuniorQuiz").Get<JuniorQuizOptions>();
            return opts ?? new JuniorQuizOptions();
        });

        services.AddSingleton<QuizContentCache>();
        services.AddSingleton<IQuizQuestionProvider, CachedQuizQuestionProvider>();

        services.AddSingleton<IQuizAttemptRepository>(sp =>
        {
            var opts = configuration.GetRequiredSection("JuniorQuiz:TableStorage").Get<TableStorageQuizOptions>();
            if (opts is null)
            {
                throw new InvalidOperationException("Missing configuration: JuniorQuiz:TableStorage.");
            }

            return new TableStorageQuizAttemptRepository(opts);
        });

        services.AddSingleton<QuizSelectionEngine>();
        services.AddSingleton<QuizService>();

        return services;
    }

    public static IServiceCollection AddJuniorQuizContentFromGitHub(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJuniorQuizCommon(configuration);

        services.AddSingleton(sp =>
        {
            var opts = configuration.GetRequiredSection("JuniorQuiz:GitHub").Get<GitHubQuizSourceOptions>();
            if (opts is null)
            {
                throw new InvalidOperationException("Missing configuration: JuniorQuiz:GitHub.");
            }

            return opts;
        });

        services.AddHttpClient<GitHubZipQuizContentSource>();

        services.AddSingleton<IQuizContentSource>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(typeof(GitHubZipQuizContentSource).FullName!);
            var opts = sp.GetRequiredService<GitHubQuizSourceOptions>();
            return new GitHubZipQuizContentSource(http, opts);
        });

        services.AddSingleton<QuizContentSynchroniser>();

        return services;
    }

    public static IServiceCollection AddJuniorQuizContentFromFileSystem(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJuniorQuizCommon(configuration);

        services.AddSingleton(sp =>
        {
            var opts = configuration.GetRequiredSection("JuniorQuiz:FileSystem").Get<FileSystemQuizSourceOptions>();
            if (opts is null)
            {
                throw new InvalidOperationException("Missing configuration: JuniorQuiz:FileSystem.");
            }

            return opts;
        });

        services.AddSingleton<IQuizContentSource>(sp =>
        {
            var opts = sp.GetRequiredService<FileSystemQuizSourceOptions>();
            return new FileSystemQuizContentSource(opts);
        });

        services.AddSingleton<QuizContentSynchroniser>();

        return services;
    }
}
