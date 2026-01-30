using BOTGC.MemberPortal.Services;

namespace BOTGC.MemberPortal.Extensions;

public static class LearningPackServiceCollectionExtensions
{
    public static IServiceCollection AddLearningPacksCommon(this IServiceCollection services)
    {
        services.AddScoped<LearningPackContentCache>();
        services.AddScoped<ILearningPackProvider, CachedLearningPackProvider>();

        services.AddScoped<ILearningPackProgressRepository, TableStorageLearningPackProgressRepository>();

        services.AddScoped<LearningPackService>();
        services.AddSingleton<LearningMarkdownRenderer>();

        services.AddHostedService<LearningPackContentWarmupHostedService>();

        return services;
    }

    public static IServiceCollection AddLearningPacksFromGitHub(this IServiceCollection services)
    {
        services.AddLearningPacksCommon();

        services.AddHttpClient<GitHubZipLearningPackContentSource>();
        services.AddScoped<ILearningPackContentSource, GitHubZipLearningPackContentSource>();

        services.AddScoped<LearningPackContentSynchroniser>();

        return services;
    }

    public static IServiceCollection AddLearningPacksFromFileSystem(this IServiceCollection services)
    {
        services.AddLearningPacksCommon();

        services.AddScoped<ILearningPackContentSource, FileSystemLearningPackContentSource>();

        services.AddScoped<LearningPackContentSynchroniser>();

        return services;
    }
}
