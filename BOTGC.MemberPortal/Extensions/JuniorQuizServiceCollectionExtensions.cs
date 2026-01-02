using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using BOTGC.MemberPortal.Services;

namespace BOTGC.MemberPortal;

public static class JuniorQuizServiceCollectionExtensions
{
    public static IServiceCollection AddJuniorQuizFromGitHub(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(sp =>
        {
            var section = config.GetRequiredSection("JuniorQuiz");
            return section.Get<JuniorQuizOptions>() ?? new JuniorQuizOptions();
        });

        services.AddSingleton(sp =>
        {
            var section = config.GetRequiredSection("JuniorQuiz:GitHub");
            var opts = section.Get<GitHubQuizSourceOptions>();
            if (opts is null)
            {
                throw new InvalidOperationException("Missing configuration: JuniorQuiz:GitHub.");
            }

            return opts;
        });

        services.AddHttpClient<GitHubZipQuizContentSource>();

        services.AddSingleton<IQuizContentSource>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>()
                        .CreateClient(typeof(GitHubZipQuizContentSource).FullName!);

            var opts = sp.GetRequiredService<GitHubQuizSourceOptions>();
            return new GitHubZipQuizContentSource(http, opts);
        });

        services.AddSingleton<QuizContentCache>(sp =>
        {
            var cache = sp.GetRequiredService<ICacheService>();
            var opts = sp.GetRequiredService<JuniorQuizOptions>();
            return new QuizContentCache(cache, opts);
        });

        services.AddSingleton<QuizContentSynchroniser>();

        services.AddSingleton<IQuizQuestionProvider>(sp =>
        {
            var cache = sp.GetRequiredService<QuizContentCache>();
            return new CachedQuizQuestionProvider(cache);
        });

        return services;
    }

    public static IServiceCollection AddJuniorQuizFromFileSystem(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(sp =>
        {
            var section = config.GetRequiredSection("JuniorQuiz");
            return section.Get<JuniorQuizOptions>() ?? new JuniorQuizOptions();
        });

        services.AddSingleton(sp =>
        {
            var section = config.GetRequiredSection("JuniorQuiz:FileSystem");
            var opts = section.Get<FileSystemQuizSourceOptions>();
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

        services.AddSingleton<QuizContentCache>(sp =>
        {
            var cache = sp.GetRequiredService<ICacheService>();
            var opts = sp.GetRequiredService<JuniorQuizOptions>();
            return new QuizContentCache(cache, opts);
        });

        services.AddSingleton<QuizContentSynchroniser>();

        services.AddSingleton<IQuizQuestionProvider>(sp =>
        {
            var cache = sp.GetRequiredService<QuizContentCache>();
            return new CachedQuizQuestionProvider(cache);
        });

        return services;
    }
}
