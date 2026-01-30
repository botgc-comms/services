using BOTGC.MemberPortal.Models;
using System.Reflection.Metadata;

namespace BOTGC.MemberPortal;

public class AppSettings
{
    public string AllowedCorsOrigins { get; set; } = string.Empty;
    public ApiSettings API { get; set; } = new();
    public Access? Access { get; set; }
    public List<TileDefinition> Tiles { get; set; } = new List<TileDefinition>();
    public Cache Cache { get; set; } = new();
    public WhatsNextSettings? WhatsNext { get; init; }

    public QuizSettings Quiz { get; set; } = new();
    public AdminSettings Admin { get; set; } = new();
    public LearningPackSettings LearningPacks { get; set; } = new();
}

public sealed class LearningPackSettings
{
    public string CacheKeyPrefix { get; set; } = "learningpacks";
    public TimeSpan PrimaryCacheTtl { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan StandbyCacheTtl { get; set; } = TimeSpan.FromDays(7);

    public LearningPackGitHubSettings? GitHub { get; set; }
    public LearningPackFileSystemSettings? FileSystem { get; set; }
    public LearningPackTableStorageSettings? TableStorage { get; set; }
}

public sealed class LearningPackGitHubSettings
{
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string Ref { get; set; } = "main";
    public string RootPath { get; set; } = "learning-packs";
    public string? Token { get; set; }
}

public sealed class LearningPackFileSystemSettings
{
    public string RootPath { get; set; } = string.Empty;
}

public sealed class LearningPackTableStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ProgressTableName { get; set; } = "LearningPackProgress";
    public string PageViewsTableName { get; set; } = "LearningPackPageViews";
}

public sealed class QuizSettings
{
    public JuniorQuizSettings JuniorQuiz { get; set; } = new();
    public GitHubQuizSourceSettings? GitHub { get; set; }
    public FileSystemQuizSourceSettings? FileSystem { get; set; }
    public TableStorageQuizSettings? TableStorage { get; set; }
}

public sealed class JuniorQuizSettings
{
    public string CacheKeyPrefix { get; set; } = "junior-quiz";
    public TimeSpan PrimaryCacheTtl { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan StandbyCacheTtl { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(30);
}

public sealed class GitHubQuizSourceSettings
{
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string Ref { get; set; } = "main";
    public string RootPath { get; set; } = "questions";
    public string? Token { get; set; }
}

public sealed class FileSystemQuizSourceSettings
{
    public string RootPath { get; set; } = string.Empty;
    public string QuestionsFolderName { get; set; } = "questions";
    public string ImageFileName { get; set; } = "image.png";
}

public sealed class TableStorageQuizSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string AttemptsTableName { get; set; } = "QuizAttempts";
    public string AnswersTableName { get; set; } = "QuizAnswers";
}

public sealed class WhatsNextSettings
{
    public int MaxItems { get; init; } = 2;
    public IReadOnlyList<WhatsNextGenericItemSettings> GenericItems { get; init; } = Array.Empty<WhatsNextGenericItemSettings>();
}

public sealed class WhatsNextGenericItemSettings
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Href { get; init; }
    public required string IconUrl { get; init; }

    public DateTimeOffset? StartUtc { get; init; }
    public int Priority { get; init; }
}

public class ApiSettings
{
    public string Url { get; set; } = string.Empty;
    public string XApiKey { get; set; } = string.Empty;
}

public class Access
{
    public string? SharedSecret { get; set; }
    public string? CookieName { get; set; }
    public int CookieTtlDays { get; set; } = 365;
}

public class Cache
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "BOTGC.API";
}

public sealed class AdminSettings
{
    public AdminTableStorageSettings? TableStorage { get; set; }
}

public sealed class AdminTableStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;

    public string CheckRideReportsTableName { get; set; } = "CheckRideReports";
}