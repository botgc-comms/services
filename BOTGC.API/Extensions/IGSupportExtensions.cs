using Services.Common;
using Services.Dto;
using Services.Interfaces;
using Services.Services;
using Services.Services.CompetitionProcessors;
using System.Net;

namespace Services.Extensions
{
    public static class IGSupportExtensions
    {
        public static IServiceCollection AddIGSupport(this IServiceCollection services)
        {
            services.AddSingleton<CookieContainer>(); // Shared Cookie Container
            services.AddHttpClient<IGLoginService>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var cookieContainer = sp.GetRequiredService<CookieContainer>();
                    return new HttpClientHandler
                    {
                        CookieContainer = cookieContainer,
                        UseCookies = true,
                        AllowAutoRedirect = true
                    };
                });

            services.AddHttpClient<IReportService>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var cookieContainer = sp.GetRequiredService<CookieContainer>();
                    return new HttpClientHandler
                    {
                        CookieContainer = cookieContainer,
                        UseCookies = true,
                        AllowAutoRedirect = true
                    };
                });

            services.AddSingleton<IGLoginService>();
            services.AddSingleton<IReportParser<MemberDto>, IGMemberReportParser>();
            services.AddSingleton<IReportParser<RoundDto>, IGRoundReportParser>();
            services.AddSingleton<IReportParser<PlayerIdLookupDto>, IGPlayerIdLookupReportParser>();
            services.AddSingleton<IReportParser<ScorecardDto>, IGScorecardReportParser>();

            services.AddTransient<ICompetitionProcessor, JuniorEclecticCompetitionProcessor>();

            services.AddSingleton<ICompetitionTaskQueue, CompetitionTaskQueue>();
            services.AddHostedService<CompeitionStatus>

            services.AddSingleton<IReportService, IGReportsService>();

            return services;
        }
    }
}
