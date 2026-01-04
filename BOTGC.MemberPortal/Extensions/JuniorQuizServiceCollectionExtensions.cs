using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BOTGC.MemberPortal.Extensions;

public static class JuniorQuizServiceCollectionExtensions
{
    public static IServiceCollection AddJuniorQuizCommon(this IServiceCollection services)
    {
        services.AddScoped<QuizContentCache>();
        services.AddScoped<IQuizQuestionProvider, CachedQuizQuestionProvider>();

        services.AddScoped<IQuizAttemptRepository, TableStorageQuizAttemptRepository>();

        services.AddScoped<QuizSelectionEngine>();
        services.AddScoped<QuizService>();

        services.AddHostedService<QuizContentWarmupHostedService>();

        return services;
    }

    public static IServiceCollection AddJuniorQuizContentFromGitHub(this IServiceCollection services)
    {
        services.AddJuniorQuizCommon();

        services.AddHttpClient<GitHubZipQuizContentSource>();
        services.AddScoped<IQuizContentSource, GitHubZipQuizContentSource>();

        services.AddScoped<QuizContentSynchroniser>();

        return services;
    }

    public static IServiceCollection AddJuniorQuizContentFromFileSystem(this IServiceCollection services)
    {
        services.AddJuniorQuizCommon();

        services.AddScoped<IQuizContentSource, FileSystemQuizContentSource>();

        services.AddScoped<QuizContentSynchroniser>();

        return services;
    }
}
